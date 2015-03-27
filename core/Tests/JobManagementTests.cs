//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EMIC.Cloud.GenericWorker;
using Microsoft.EMIC.Cloud.Demo;
using Microsoft.EMIC.Cloud.ApplicationRepository;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.StorageClient;
using System.Runtime.Serialization;
using System.IO;
using System.Web;
using Ionic.Zip;
using Microsoft.EMIC.Cloud.DataManagement;
using Microsoft.EMIC.Cloud.Storage.Azure;
using System.ComponentModel.Composition.Hosting;
using OGF.BES;
using KTH.GenericWorker.CDMI;
using System.Threading;
using Microsoft.WindowsAzure;
using System.ServiceModel;
using Microsoft.EMIC.Cloud.GenericWorker.AzureProvider;
using Microsoft.EMIC.Cloud.GenericWorker.SampleNotifications;
using Microsoft.EMIC.Cloud.Utilities;
using System.ServiceModel.Description;
using Microsoft.EMIC.Cloud.UserAdministration;
using Microsoft.EMIC.Cloud.Administrator.Host;
using System.Xml;

namespace Tests
{
    [TestClass]
    public class JobManagementTests
    {
        private const int testTimeOutInMilliSeconds = 3 * 90 * 1000;

        protected const bool IsSecurityEnabled = false;

        protected static ServiceHost genericWorkerHost;
        protected static ServiceHost scalingServiceHost;
        protected static ServiceHost notificationServiceHost;

        protected static CloudStorageAccount account;
        protected static CloudBlobClient blobClient;
        protected static CloudBlobContainer userDataContainer;
        protected static CloudBlobContainer gwJobContainer;
        protected static CancellationTokenSource cts;
        protected static IGWRuntimeEnvironment azureRuntime;
        protected static CompositionContainer genericWorkerContainer, containerGW;
        protected static GenericWorkerDriver driver;

        public static string applicationIdentificationURI;
        public static Reference appReference;
        public static Reference descReference;
        protected static VENUSApplicationDescription appDesc;
        protected static Func<string, Reference> AzureRefBuilder, CDMIRefBuilder;
        protected const int mathJobExecutionTimeInSeconds = 20;
        protected JobTableDataContext GetDetailsTable()
        {
            return new JobTableDataContext(
               account.TableEndpoint.AbsoluteUri,
               account.Credentials, CloudSettings.GenericWorkerDetailsTableName);
        }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            account = CloudStorageAccount.Parse(CloudSettings.UserDataStoreConnectionString);
            blobClient = account.CreateCloudBlobClient();
            userDataContainer = blobClient.GetContainerReference(CloudSettings.UserDataStoreBlobName);
            userDataContainer.CreateIfNotExist();
            BlobContainerPermissions resultPermissions = userDataContainer.GetPermissions();
            resultPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
            userDataContainer.SetPermissions(resultPermissions);
            gwJobContainer = blobClient.GetContainerReference(CloudSettings.GenericWorkerJobBlobStore);

            TestHelper.UploadMathApplication(out applicationIdentificationURI, out appReference, out descReference, out appDesc, userDataContainer);
        }

        [TestInitialize]
        public void TestInitialize()
        {
            cts = new CancellationTokenSource();

            #region service setup

            genericWorkerHost = new ServiceHost(typeof(BESFactoryPortTypeImplService));
            genericWorkerContainer = new CompositionContainer(
                 new AggregateCatalog(
                     new TypeCatalog(typeof(CloudSettings)),
                     new AssemblyCatalog(typeof(LiteralArgument).Assembly),
                     new AssemblyCatalog(typeof(AzureGWRuntimeEnvironment).Assembly),
                     new AssemblyCatalog(typeof(AzureArgumentCatalogueReference).Assembly),
                     new AssemblyCatalog(typeof(KTH.GenericWorker.CDMI.CDMIBlobReference).Assembly),
                     new AssemblyCatalog(typeof(AzureQueueJobStatusNotificationPlugin).Assembly)
                ));

            genericWorkerHost.Description.Behaviors.Add(new MyServiceBehavior<BESFactoryPortTypeImplService>(genericWorkerContainer));
            genericWorkerHost.Description.Behaviors.Find<ServiceDebugBehavior>().IncludeExceptionDetailInFaults = true;
            genericWorkerHost.AddServiceEndpoint(typeof(BESFactoryPortType), WCFUtils.CreateUnprotectedBinding(), CloudSettings.GenericWorkerUrl);
            genericWorkerHost.Open();

            var scalingServiceContainer = new CompositionContainer(
                new AggregateCatalog(
                     new TypeCatalog(typeof(CloudSettings)),
                     new AssemblyCatalog(typeof(LiteralArgument).Assembly)
                    ));

            scalingServiceHost = new ServiceHost(typeof(ScalingServiceImpl));
            scalingServiceHost.Description.Behaviors.Add(new MyServiceBehavior<ScalingServiceImpl>(scalingServiceContainer));
            scalingServiceHost.Description.Behaviors.Find<ServiceDebugBehavior>().IncludeExceptionDetailInFaults = true;
            scalingServiceHost.AddServiceEndpoint(typeof(IScalingService), WCFUtils.CreateUnprotectedBinding(), CloudSettings.ScalingServiceUrl);
            scalingServiceHost.Open();

            notificationServiceHost = new ServiceHost(typeof(NotificationService));
            notificationServiceHost.Description.Behaviors.Add(new MyServiceBehavior<NotificationService>(genericWorkerContainer));
            notificationServiceHost.Description.Behaviors.Find<ServiceDebugBehavior>().IncludeExceptionDetailInFaults = true;
            notificationServiceHost.AddServiceEndpoint(typeof(INotificationService), WCFUtils.CreateUnprotectedBinding(), CloudSettings.NotificationServiceURL);
            notificationServiceHost.Open();

            CreateProfileClient.CreateClientIdentityFileForService(CloudSettings.ProcessIdentityFilename);
            HostState state = new HostState();
            var t = CreateProfileHost.CreateTask(CloudSettings.ProcessIdentityFilename, cts, state);
            t.Start();

            // bring up one worker task
            containerGW = new CompositionContainer(new AggregateCatalog(
                new TypeCatalog(typeof(CloudSettings)),
                new AssemblyCatalog(typeof(GenericWorkerDriver).Assembly),
                new AssemblyCatalog(typeof(AzureGWRuntimeEnvironment).Assembly),
                new AssemblyCatalog(typeof(AzureArgumentCatalogueReference).Assembly),
                new AssemblyCatalog(typeof(KTH.GenericWorker.CDMI.CDMIBlobReference).Assembly),
                     new AssemblyCatalog(typeof(AzureQueueJobStatusNotificationPlugin).Assembly)
                ));

            azureRuntime = containerGW.GetExportedValue<IGWRuntimeEnvironment>();
            driver = containerGW.GetExportedValue<GenericWorkerDriver>();
            TestHelper.FlushTable(genericWorkerContainer);
            #endregion
            TestHelper.FlushBlobContainer(genericWorkerContainer);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (!cts.IsCancellationRequested)
            {
                cts.Cancel();
            }
            cts = null;

            genericWorkerHost.Close();

            while (true)
            {
                if (genericWorkerHost.State == CommunicationState.Closed || genericWorkerHost.State == CommunicationState.Faulted)
                {
                    break;
                }
                Thread.Sleep(500);
            }

            genericWorkerHost = null;

            scalingServiceHost.Close();

            while (true)
            {
                if (scalingServiceHost.State == CommunicationState.Closed || scalingServiceHost.State == CommunicationState.Faulted)
                {
                    break;
                }
                Thread.Sleep(500);
            }

            scalingServiceHost = null;

            notificationServiceHost.Close();

            while (true)
            {
                if (notificationServiceHost.State == CommunicationState.Closed || notificationServiceHost.State == CommunicationState.Faulted)
                {
                    break;
                }
                Thread.Sleep(500);
            }

            notificationServiceHost = null;

            TestHelper.FlushTable(genericWorkerContainer);
            TestHelper.FlushBlobContainer(genericWorkerContainer);
            Thread.Sleep(6500);//To be on the save side
        }

        /// <summary>
        /// Test title: JobListingTest
        /// Description:
        ///     This test case covers the use of the GetJobs and GetAllJobs methods
        /// Author:
        ///     EMIC / emic_cce@microsoft.com
        /// Category:
        ///     Job Management, positive, local, end2end
        /// Running services/endpoints: 
        ///     running Job Management endpoint    
        /// Expected Results: 
        ///     2 jobs submitted by alice [GetJobs("alice")], 1 job by bob [GetJobs("bob")], in total 3 jobs [GetAllJobs()]
        /// </summary>      
        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void JobListingTest()
        {
            var rt = new TwoRuntimes(genericWorkerContainer);
            var worker = rt.RT0;
            var submission = rt.RT1;
            string alice = "Alice";
            string bob = "Bob";

            var alice1 = submission.SubmitJob(alice, Guid.NewGuid().ToString(), TestHelper.CreateJSDL);
            var alice2 = submission.SubmitJob(alice, Guid.NewGuid().ToString(), TestHelper.CreateJSDL);
            var bob1 = submission.SubmitJob(bob, Guid.NewGuid().ToString(), TestHelper.CreateJSDL);

            var jobManagementPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);
            var aliceJobs = jobManagementPortal.GetJobs("Alice");
            Assert.IsTrue(aliceJobs.Count == 2);
            var bobJobs = jobManagementPortal.GetJobs("Bob");
            Assert.IsTrue(bobJobs.Count == 1);
            var allJobs = jobManagementPortal.GetAllJobs();
            Assert.IsTrue(allJobs.Count == 3);

            worker.MarkJobAsChekingInputData(alice1, "worker 1");
            worker.MarkJobAsRunning(alice1, "", "", "");

            var aliceJobStatuses = jobManagementPortal.GetActivityStatuses(aliceJobs);
            var alicePendingJobs = aliceJobStatuses.GetActivityStatusesResponse1.Response.Where(r => r.ActivityStatus.state == ActivityStateEnumeration.Pending).Select(e => e.ActivityIdentifier).ToList();

            var aliceJobStatusMapping = aliceJobStatuses.GetActivityStatusesResponse1.Response.Select(r => new { r.ActivityStatus.state, r.ActivityIdentifier }).ToList();

            Assert.IsTrue(alicePendingJobs.Count == 1);
        }

        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void GetNumberOfJobsTest()
        {
            var rt = new TwoRuntimes(genericWorkerContainer);
            var worker = rt.RT0;
            var submission = rt.RT1;
            string alice = "Alice";
            string bob = "Bob";

            var alice1 = submission.SubmitJob(alice, Guid.NewGuid().ToString(), TestHelper.CreateJSDL);
            var alice2 = submission.SubmitJob(alice, Guid.NewGuid().ToString(), TestHelper.CreateJSDL);
            var bob1 = submission.SubmitJob(bob, Guid.NewGuid().ToString(), TestHelper.CreateJSDL);

            var jobManagementPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);
            Assert.AreEqual(2, jobManagementPortal.GetNumberOfJobs(alice, new List<JobStatus> { JobStatus.Submitted }));
            Assert.AreEqual(1, jobManagementPortal.GetNumberOfJobs(bob, new List<JobStatus> { JobStatus.Submitted }));
        }

        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void GetNumberOfJobs_JobGroupsTest()
        {
            var group = "blastJobs";
            var job1 = "blast1";
            var job2 = "blast2";

            var jsdlJob1 = TestHelper.CreateGroupJSDL(group, job1);
            var jsdlJob2 = TestHelper.CreateGroupJSDL(group, job2);
            var jobManagementPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);

            var endp1 = jobManagementPortal.SubmitVENUSJob(jsdlJob1);
            var endp2 = jobManagementPortal.SubmitVENUSJob(jsdlJob2);

            Assert.AreEqual(2, jobManagementPortal.GetNumberOfJobs("anonymous", new List<JobStatus> { JobStatus.Submitted }));
        }

        /// <summary>
        /// Test title: JobGroupingTest
        /// Description:
        ///     This test case covers the submission and retrieval of jobs belonging to a group [GetJobsByGroup]
        /// Author:
        ///     EMIC / emic_cce@microsoft.com
        /// Category:
        ///     Job Management, positive, cloud, end2end
        /// Running services/endpoints: 
        ///     running Job Management endpoint    
        /// Expected Results: 
        ///     a group of 2 jobs that are submitted by user anonymous under the groupname blastJobs [ GetJobsByGroup("anonymous", "blastJobs")]
        /// </summary>      
        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void JobGroupingTest()
        {
            var group = "blastJobs";
            var job1 = "blast1";
            var job2 = "blast2";

            var jsdlJob1 = TestHelper.CreateGroupJSDL(group, job1);
            var jsdlJob2 = TestHelper.CreateGroupJSDL(group, job2);
            var jobManagementPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);

            var endp1 = jobManagementPortal.SubmitVENUSJob(jsdlJob1);
            var endp2 = jobManagementPortal.SubmitVENUSJob(jsdlJob2);
            var otherendp = jobManagementPortal.SubmitVENUSJob(TestHelper.CreateJSDL);
            var jobgroup = jobManagementPortal.GetJobsByGroup("anonymous", group);
            Assert.AreEqual(jobgroup.Count, 2);
        }

        [TestMethod]
        [Timeout(3 * testTimeOutInMilliSeconds)]
        public void RemoveTerminatedJobsTestAllStatuses()
        {
            var rt = new TwoRuntimes(genericWorkerContainer);
            var worker = rt.RT0;
            var submission = rt.RT1;

            var group = "blastJobs";
            var jobPrefix = "Job";
            var owner = "anonymous";
            var numStates = 7;

            for (var i = 0; i < numStates; i++)
            {
                var status = (JobStatus)i;
                var jobName = string.Format("{0}{1}", jobPrefix, i);
                var job = submission.SubmitJob(owner, jobName, TestHelper.CreateGroupJSDL(group, jobName));
                Assert.IsTrue(TestHelper.AdvanceNewJobToStatus(worker, job, status));
            }
            var jobManagementPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);
            Assert.AreEqual(3, jobManagementPortal.GetNumberOfJobs(owner, new List<JobStatus> { JobStatus.Submitted, JobStatus.CheckingInputData, JobStatus.CancelRequested }));
            jobManagementPortal.RemoveTerminatedJobs(owner);
            Func<bool> cond = () => (jobManagementPortal.GetJobs(owner).Count == numStates - 3) ; //-3 because of the 3 terminated states (finished, failed, cancelled)
            TestHelper.PollForCondition(cond, TimeSpan.FromMilliseconds(300), TimeSpan.FromMinutes(3));
            Assert.AreEqual(numStates - 3, jobManagementPortal.GetJobs(owner).Count);
            //TODO: make sure that the group head is also deleted if all parts of the group are deleted, what happens when a new job of that group is submitted, will that create a new head? that would be good! 
        }


        /// <summary>
        /// Test title: JobGroupCancelationTest
        /// Description:
        ///     This test case covers the submission and canceling of jobs belonging to a group [CancelGroup]
        /// Author:
        ///     EMIC / emic_cce@microsoft.com
        /// Category:
        ///     Job Management, positive, local, end2end
        /// Running services/endpoints: 
        ///     running Job Management endpoint    
        /// Expected Results: 
        ///     We have 2 jobs in the system (1 running, 1 pending(submitted/checkingInputFiles)) both belonging to the group "blastJobs" submitted by user "anonymous"
        ///     We call CancelGroup("anonymous", "blastJobs")
        ///     We expect to have one job that has the status "Cancelled" and one with the status "Cancel requested"
        /// </summary>   
        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds*2)]
        public void JobGroupCancelationTest()
        {
            var rt = new TwoRuntimes(genericWorkerContainer);
            var worker = rt.RT0;
            var submission = rt.RT1;

            var group = "blastJobs";
            var job1 = "blast1";
            var job2 = "blast2";
            var runningJob = "runningJob";
            var owner = "anonymous";

            var jsdlJob1 = TestHelper.CreateGroupJSDL(group, job1);
            var jsdlJob2 = TestHelper.CreateGroupJSDL(group, job2);
            var jobManagementPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);

            var endp1 = jobManagementPortal.SubmitVENUSJob(jsdlJob1);
            var endp2 = jobManagementPortal.SubmitVENUSJob(jsdlJob2);
            var job = worker.SubmitJob(owner, runningJob, TestHelper.CreateGroupJSDL(group, runningJob));

            var otherendp = jobManagementPortal.SubmitVENUSJob(TestHelper.CreateJSDL);

            var jobgroup = jobManagementPortal.GetJobsByGroup(owner, group);
            Assert.AreEqual(jobgroup.Count, 3);

            var ownerJobs = jobManagementPortal.GetJobs(owner);
            Assert.AreEqual(ownerJobs.Count, 4);

            worker.MarkJobAsChekingInputData(job, "instance 1");
            worker.MarkJobAsRunning(job, "", "", "");
            var ownerJobStatuses = jobManagementPortal.GetActivityStatuses(ownerJobs);
            var ownerPendingJobs = ownerJobStatuses.GetActivityStatusesResponse1.Response.Where(r => r.ActivityStatus.state == ActivityStateEnumeration.Pending).Select(e => e.ActivityIdentifier).ToList();
            Assert.AreEqual(ownerPendingJobs.Count, 3);
            var ownerRunningJobs = ownerJobStatuses.GetActivityStatusesResponse1.Response.Where(r => r.ActivityStatus.state == ActivityStateEnumeration.Running).Select(e => e.ActivityIdentifier).ToList();
            Assert.AreEqual(ownerRunningJobs.Count, 1);
            jobManagementPortal.CancelGroup(owner, group);

            Func<bool> cond = () => jobgroup.All(j => submission.GetJobStatus(j.ReferenceParameters.Any[0].InnerText) == JobStatus.Cancelled || submission.GetJobStatus(j.ReferenceParameters.Any[0].InnerText) == JobStatus.CancelRequested);
            TestHelper.PollForCondition(cond, TimeSpan.FromMilliseconds(300), TimeSpan.FromSeconds(240));
            Assert.IsTrue(cond());
        }


        /// <summary>
        /// Test title: JobGroupCancelationTestAllStatuses
        /// Description:
        ///     This test case covers the submission of one job per status (7 jobs) and canceling all the jobs belonging to the group [CancelGroup]
        /// Author:
        ///     EMIC / emic_cce@microsoft.com
        /// Category:
        ///     Job Management, positive, local, end2end
        /// Running services/endpoints: 
        ///     running Job Management endpoint    
        /// Expected Results: 
        ///     We have 7 jobs in the system (1 running, 1 pending(submitted/checkingInputFiles)) all belonging to the group "blastJobs" submitted by user "anonymous"
        ///     We call CancelGroup("anonymous", "blastJobs")
        ///     We expect to have six jobs that have the status "Cancelled" and one with the status "Cancel requested"
        /// </summary>   
        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds*4)]
        public void JobGroupCancelationServiceTestAllStatuses()
        {
            var rt = new TwoRuntimes(genericWorkerContainer);
            var worker = rt.RT0;
            var submission = rt.RT1;

            var group = "blastJobs";
            var jobPrefix = "Job";
            var owner = "anonymous";
            var numStates = 7;

            for (var i = 0; i < numStates; i++)
            {
                var status = (JobStatus)i;
                var jobName = string.Format("{0}{1}", jobPrefix, i);
                var job = submission.SubmitJob(owner, jobName, TestHelper.CreateGroupJSDL(group, jobName));
                Assert.IsTrue(TestHelper.AdvanceNewJobToStatus(worker, job, status));
            }
            var jobManagementPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);
            jobManagementPortal.CancelGroup(owner, group);

            var jobgroup = jobManagementPortal.GetJobsByGroup(owner, group);
            Assert.AreEqual(jobgroup.Count, numStates);
            Func<bool> cond = () => (jobgroup.Where(j => submission.GetJobStatus(j.ReferenceParameters.Any[0].InnerText) == JobStatus.Cancelled).Count() == 6);
            TestHelper.PollForCondition(cond, TimeSpan.FromMilliseconds(300), TimeSpan.FromSeconds(240));
            Assert.IsTrue(cond());
            Assert.AreEqual(jobgroup.Where(j => submission.GetJobStatus(j.ReferenceParameters.Any[0].InnerText) == JobStatus.Cancelled).Count(), 6);
            Assert.AreEqual(jobgroup.Where(j => submission.GetJobStatus(j.ReferenceParameters.Any[0].InnerText) == JobStatus.CancelRequested).Count(), 1);
        }

        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void StatusPollingTest()
        {
            var rt = new TwoRuntimes(genericWorkerContainer);
            var worker = rt.RT0;
            var submission = rt.RT1;
            string alice = "Alice";

            var internalJobIdAlice = Guid.NewGuid().ToString();
            var alice1 = submission.SubmitJob(alice, internalJobIdAlice, TestHelper.CreateJSDL);
            var jobManagementClient = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);
            var jobs = jobManagementClient.GetJobs(alice);
            var statuses = jobManagementClient.GetActivityStatuses(jobs);
            TestHelper.AdvanceNewJobToStatus(worker, alice1, JobStatus.CheckingInputData);
            statuses = jobManagementClient.GetActivityStatuses(jobs);
            TestHelper.AdvanceNewJobToStatus(worker, alice1, JobStatus.Running);
            statuses = jobManagementClient.GetActivityStatuses(jobs);
            Assert.AreEqual(OGF.BES.ActivityStateEnumeration.Running, statuses.GetActivityStatusesResponse1.Response[0].ActivityStatus.state);

            TestHelper.AdvanceNewJobToStatus(worker, alice1, JobStatus.Finished);
            statuses = jobManagementClient.GetActivityStatuses(jobs);
            Assert.AreEqual(OGF.BES.ActivityStateEnumeration.Finished, statuses.GetActivityStatusesResponse1.Response[0].ActivityStatus.state);
        }

        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void CustomerIDIsURIButJobIsNotHierarchicalOrGroupJobTest()
        {
            var rt = new TwoRuntimes(genericWorkerContainer);
            var worker = rt.RT0;
            var submission = rt.RT1;
            string alice = "Alice";

            var internalJobIdAlice = Guid.NewGuid().ToString();
            var jobDesc = TestHelper.CreateJSDL;
            jobDesc.CustomerJobID = "Vina:E:\vina_input_hans\apdbqt\\20009.pdbqt 20/06/2012 12:30:14";
            var alice1 = submission.SubmitJob(alice, internalJobIdAlice, jobDesc);
            var jobManagementClient = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);
            var jobs = jobManagementClient.GetJobs(alice);
            var statuses = jobManagementClient.GetActivityStatuses(jobs);
            TestHelper.AdvanceNewJobToStatus(worker, alice1, JobStatus.CheckingInputData);
            statuses = jobManagementClient.GetActivityStatuses(jobs);
            TestHelper.AdvanceNewJobToStatus(worker, alice1, JobStatus.Running);
            statuses = jobManagementClient.GetActivityStatuses(jobs);
            Assert.AreEqual(OGF.BES.ActivityStateEnumeration.Running, statuses.GetActivityStatusesResponse1.Response[0].ActivityStatus.state);
            jobManagementClient.TerminateActivities(jobs);
            Func<bool> cond = () => { return jobManagementClient.GetActivityStatuses(jobs).GetActivityStatusesResponse1.Response[0].ActivityStatus.state == OGF.BES.ActivityStateEnumeration.Pending; };
            TestHelper.PollForCondition(cond ,TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(30));
            Assert.AreEqual(OGF.BES.ActivityStateEnumeration.Pending, jobManagementClient.GetActivityStatuses(jobs).GetActivityStatusesResponse1.Response[0].ActivityStatus.state);
        }

        [TestMethod]
        public void JobSubmissionUsingUploadsRefs()
        {
            Task.Factory.StartNew(() => driver.Run(cts.Token));

            string inputFile = "input_file.fasta";
            int numFragments = 5;

            string upvBioURIPrefix = "http://www.upvbio.eu/cloud/demo/gw/UPVBIOApp/";
            string splitterAppIdentificationURI = upvBioURIPrefix + "Splitter";

            var account = CloudStorageAccount.Parse(CloudSettings.UserDataStoreConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer appDataContainer = blobClient.GetContainerReference("applicationcontainer");
            appDataContainer.CreateIfNotExist();

            Func<ReferenceCollection, bool, CloudBlob> uploadReferenceCollection = ((referenceCollection, isUploadsCollection) =>
            {
                var postfix = (isUploadsCollection) ? "UploadsCollection" : "DownloadsCollection";
                var blobName = Guid.NewGuid().ToString() + postfix;

                var ra = new ReferenceArray() { Name = postfix, References = referenceCollection };

                var xmlDoc = new XmlDocument();
                var serializedRefArr = ra.Serialize(xmlDoc);
                xmlDoc.AppendChild(serializedRefArr);

                CloudBlob xmlBlob;

                xmlBlob = userDataContainer.GetBlobReference(blobName);
                xmlBlob.Properties.ContentType = "text/xml";
                xmlBlob.UploadText(xmlDoc.InnerXml);

                return xmlBlob;
            });

            // method for file uploading
            Func<string, string> uploadFileFasta = ((blobname) =>
            {
                var blob = userDataContainer.GetBlobReference(blobname);
                var content = ">AACY01000001;AACY01000000;|NUC|environmental_sequence|unclassifiedsequences;environmentalsamples.\ntatttaatggtctatatatcgctgcaacttttactccgtagttttctatttgcattgccattaattcaaagttattgaaatgacctgaaataaatacaacaggttttttttctttaataatcttatctaaattatttaaaccattaattttaatatggtcatttaattcaccatttttaaatttattaagaaaaatatattctgacaatattcttccatagttaccccacatattactaatcattttatttctttcatgattagaacttccaacgtttgacttttcaagattatttaaaattttattatttgatcgaaaaataggtccaaaagattttcctaaaaattctccaatagtagaagatattttgtagccaaaaattttaaaaataaaaaagaaaaattttataattataaattcgaaaaaatatttaattgtcttcataaactttcctttaaaagttttataaagttatctttattttctatttctaatttaacttctaatttttctatactatttttaaaactattattgattctaaaataatctttttcagttgttaataatattgcgtcattattttttgcttttaatattagatcgtttaaatctttttctgagaatttataatggtcaggaaaacttctataatctataagatttaagttgttatctttaataagttcaaaaaaatttattggattacctattcctgcaaaagctataatttttttattagaaaaatttgaaatattttttggaacatatttagtataaaaaatattattttcttttattttatttatttgtttttctatatttgtattttttttaccattaataaatatacaatttgctctacttagtgagttcaaattttctctaagtggtcctgcaggtattaataatccattacctatccattgtttttcattaaaacataaaattgaaaagtttttttgaattgaaaaatcttgaaaaccatcatccattattgcagtatcatatttgttttttattaattcttttattgctaatgatcggtttttttctgaaaagactatacttttacttttgagtaatgaaatttcatcttcaaggtaattgtaatattttttaataattgctggattttttccaatagaatttaagatattataaatctcataaactaaaggagttttacctgttccacctaaataaatgttcccaacacacacaataggaatttcaaagttttgattttttttaaacttttttataatagttaataaaaaaaaataaataattgaaataggatataaaataattgaataaatagaaattttttctgagtcccagaatcttggttttttaattaacattatattaaattttttatttctaaatcgatattatttaatattcttttccctagtaaattaattttatttatattttttttcagtcttattttggtttgaagatttttatttataattaattttgcgtccctaacattttttatttttgaagaaactccaattttatttaataatacataaatttctttgaaattctctacatttgaaccatgcataactttacaaccacttcttgcagcttccaaggggttttggcctcctcttttataattgaaccacccaagtatacaatcttacacatgtt\n";
                for (int i=0;i<2;i++) content+=content;
                blob.UploadText(content);
                var blobAddress = blob.Uri.AbsoluteUri;

                return blobAddress;
            });

            Func<string, string> computeName = ((name) =>
            {
                var result = userDataContainer.GetBlobReference(name).Uri.AbsoluteUri;

                return result;
            });

            var addressInputFile = uploadFileFasta(inputFile);


            // define Splitter job description
            var splitterJob = new VENUSJobDescription()
            {
                ApplicationIdentificationURI = splitterAppIdentificationURI,
                CustomerJobID = "UPVBIO Splitter Job " + DateTime.Now.ToLocalTime().ToString(),
                JobName = "some job name",
                AppPkgReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(splitterAppIdentificationURI) + "_App"), CloudSettings.UserDataStoreConnectionString)),
                AppDescReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(splitterAppIdentificationURI) + "_Desc"), CloudSettings.UserDataStoreConnectionString)),
                JobArgs = new ArgumentCollection()
                {
                        new SingleReference
                        {
                            Name="inputfile",
                            Ref= new Reference(
                                    inputFile,
                                    new AzureBlobReference
                                    {
                                        DataAddress = addressInputFile, 
                                        ConnectionString = CloudSettings.UserDataStoreConnectionString,                            
                                    })
                        },
                        new LiteralArgument
                        {
                            Name = "numfragments",
                            LiteralValue = String.Format("{0}", numFragments)
                        },
                        new LiteralArgument
                        {
                            Name = "startfragment",
                            LiteralValue = "0"
                        }                        
                }
            };


            //add uploads to job
            //create your own ReferenceCollection:
            var refCol = new ReferenceCollection();

            //add References to it as you did with the Uploads and Downloads property:
            for (int i = 0; i < numFragments; i++)
            {
                var seqfileName = string.Format("seqfile{0}.sqf", i);
                refCol.Add(new Reference(seqfileName, new AzureBlobReference(computeName(seqfileName), CloudSettings.UserDataStoreConnectionString)));
            }

            //new:
            var uploadsRefBlobSplitter = uploadReferenceCollection(refCol, true);
            splitterJob.UploadsReference = new Reference(new AzureBlobReference(uploadsRefBlobSplitter, CloudSettings.UserDataStoreConnectionString));

            var submissionPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);

            CreateActivityResponse resp = submissionPortal.SubmitVENUSJob(splitterJob);

            bool foundResult = false;

            #region Poll for result

            int attempts = 0;
            while (!foundResult)
            {
                var r = userDataContainer.GetBlobReference("seqfile2.sqf");
                try
                {
                    var t = r.DownloadText();
                    foundResult = true;
                }
                catch (StorageClientException ex)
                {
                    if (ex.ErrorCode != StorageErrorCode.BlobNotFound)
                    {
                        throw;
                    }

                    if (attempts++ == 200)
                    {
                        throw new TimeoutException("No result seen");
                    }

                    Thread.Sleep(2000);
                }
            }
            #endregion
            Assert.IsTrue(foundResult);

        }

        [TestMethod]
        public void JobSubmissionUsingDownloadsRefs()
        {
            Task.Factory.StartNew(() => driver.Run(cts.Token));

            string resultFileName = "assembledResult";
            
            int numFragments = 3;

            string upvBioURIPrefix = "http://www.upvbio.eu/cloud/demo/gw/UPVBIOApp/";
            string assemblerAppIdentificationURI = upvBioURIPrefix + "Assembler";

            var account = CloudStorageAccount.Parse(CloudSettings.UserDataStoreConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer appDataContainer = blobClient.GetContainerReference("applicationcontainer");
            appDataContainer.CreateIfNotExist();

            Func<ReferenceCollection, bool, CloudBlob> uploadReferenceCollection = ((referenceCollection, isUploadsCollection) =>
            {
                var postfix = (isUploadsCollection) ? "UploadsCollection" : "DownloadsCollection";
                var blobName = Guid.NewGuid().ToString() + postfix;

                var ra = new ReferenceArray() { Name = postfix, References = referenceCollection };

                var xmlDoc = new XmlDocument();
                var serializedRefArr = ra.Serialize(xmlDoc);
                xmlDoc.AppendChild(serializedRefArr);

                CloudBlob xmlBlob;

                xmlBlob = userDataContainer.GetBlobReference(blobName);
                xmlBlob.Properties.ContentType = "text/xml";
                xmlBlob.UploadText(xmlDoc.InnerXml);

                return xmlBlob;
            });

            // method for file uploading
            Func<string, string> uploadText = ((blobname) =>
            {
                var blob = userDataContainer.GetBlobReference(blobname);
                blob.UploadText("sdfsfsff");
                var blobAddress = blob.Uri.AbsoluteUri;

                return blobAddress;
            });

            Func<string, string> computeName = ((name) =>
            {
                var result = userDataContainer.GetBlobReference(name).Uri.AbsoluteUri;

                return result;
            });

            var assemblerJob = new VENUSJobDescription()
            {
                ApplicationIdentificationURI = assemblerAppIdentificationURI,
                CustomerJobID = "UPVBIO Assembler Job " + DateTime.Now.ToLocalTime().ToString(),
                JobName = "some job name",
                AppPkgReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(assemblerAppIdentificationURI) + "_App"), CloudSettings.UserDataStoreConnectionString)),
                AppDescReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(assemblerAppIdentificationURI) + "_Desc"), CloudSettings.UserDataStoreConnectionString)),
                JobArgs = new ArgumentCollection()
                {
                        new SingleReference
                        {
                            Name="resultFileName",
                            Ref = new Reference(resultFileName, new AzureBlobReference(computeName(resultFileName), CloudSettings.UserDataStoreConnectionString)),
                        }
                }
            };
            //add downloads to job
            var downloadsRefCol = new ReferenceCollection();
            for (int i = 0; i < numFragments; i++)
            {
                var resultfileName = string.Format("result{0}.txt", i);
                uploadText(resultfileName);
                downloadsRefCol.Add(new Reference(resultfileName, new AzureBlobReference(computeName(resultfileName), CloudSettings.UserDataStoreConnectionString)));
            }

            var downloadsRefBlobAssembler = uploadReferenceCollection(downloadsRefCol, false);
            assemblerJob.DownloadsReference = new Reference(new AzureBlobReference(downloadsRefBlobAssembler, CloudSettings.UserDataStoreConnectionString));

            var submissionPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);

            CreateActivityResponse resp = submissionPortal.SubmitVENUSJob(assemblerJob);

            bool foundResult = false;

            #region Poll for result

            int attempts = 0;
            while (!foundResult)
            {
                var r = userDataContainer.GetBlobReference(resultFileName);
                try
                {
                    var t = r.DownloadText();
                    foundResult = true;
                }
                catch (StorageClientException ex)
                {
                    if (ex.ErrorCode != StorageErrorCode.BlobNotFound)
                    {
                        throw;
                    }

                    if (attempts++ == 200)
                    {
                        throw new TimeoutException("No result seen");
                    }

                    Thread.Sleep(2000);
                }
            }
            #endregion
            Assert.IsTrue(foundResult);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<OGF.BES.Faults.InvalidRequestMessageFaultType>))]
        public void JobSubmissionUsingNonExistingDownloadsRefs()
        {
            Task.Factory.StartNew(() => driver.Run(cts.Token));

            string resultFileName = "assembledResult";

            int numFragments = 3;

            string upvBioURIPrefix = "http://www.upvbio.eu/cloud/demo/gw/UPVBIOApp/";
            string assemblerAppIdentificationURI = upvBioURIPrefix + "Assembler";

            var account = CloudStorageAccount.Parse(CloudSettings.UserDataStoreConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer appDataContainer = blobClient.GetContainerReference("applicationcontainer");
            appDataContainer.CreateIfNotExist();

            Func<ReferenceCollection, bool, CloudBlob> uploadReferenceCollection = ((referenceCollection, isUploadsCollection) =>
            {
                var postfix = (isUploadsCollection) ? "UploadsCollection" : "DownloadsCollection";
                var blobName = Guid.NewGuid().ToString() + postfix;

                var ra = new ReferenceArray() { Name = postfix, References = referenceCollection };

                var xmlDoc = new XmlDocument();
                var serializedRefArr = ra.Serialize(xmlDoc);
                xmlDoc.AppendChild(serializedRefArr);

                CloudBlob xmlBlob;

                xmlBlob = userDataContainer.GetBlobReference(blobName);
                xmlBlob.Properties.ContentType = "text/xml";
                xmlBlob.UploadText(xmlDoc.InnerXml);

                return xmlBlob;
            });

            // method for file uploading
            Func<string, string> uploadText = ((blobname) =>
            {
                var blob = userDataContainer.GetBlobReference(blobname);
                blob.UploadText("sdfsdfsffdsssfsf");
                var blobAddress = blob.Uri.AbsoluteUri;

                return blobAddress;
            });

            Func<string, string> computeName = ((name) =>
            {
                var result = userDataContainer.GetBlobReference(name).Uri.AbsoluteUri;

                return result;
            });

            var assemblerJob = new VENUSJobDescription()
            {
                ApplicationIdentificationURI = assemblerAppIdentificationURI,
                CustomerJobID = "UPVBIO Assembler Job " + DateTime.Now.ToLocalTime().ToString(),
                JobName = "some job name",
                AppPkgReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(assemblerAppIdentificationURI) + "_App"), CloudSettings.UserDataStoreConnectionString)),
                AppDescReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(assemblerAppIdentificationURI) + "_Desc"), CloudSettings.UserDataStoreConnectionString)),
                JobArgs = new ArgumentCollection()
                {
                        new SingleReference
                        {
                            Name="resultFileName",
                            Ref = new Reference(resultFileName, new AzureBlobReference(computeName(resultFileName), CloudSettings.UserDataStoreConnectionString)),
                        }
                }
            };
            //add downloads to job
            var downloadsRefCol = new ReferenceCollection();
            for (int i = 0; i < numFragments; i++)
            {
                var resultfileName = string.Format("result{0}.txt", i);
                uploadText(resultfileName);
                downloadsRefCol.Add(new Reference(resultfileName, new AzureBlobReference(computeName(resultfileName), CloudSettings.UserDataStoreConnectionString)));
            }

            var downloadsRefBlobAssembler = uploadReferenceCollection(downloadsRefCol, false);
            assemblerJob.DownloadsReference = new Reference(new AzureBlobReference(downloadsRefBlobAssembler, CloudSettings.UserDataStoreConnectionString));
            downloadsRefBlobAssembler.DeleteIfExists();
            var submissionPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);

            CreateActivityResponse resp = submissionPortal.SubmitVENUSJob(assemblerJob);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<OGF.BES.Faults.InvalidRequestMessageFaultType>))]
        public void JobSubmissionUsingDownloadsRefsBlobWithInvalidFormat()
        {
            Task.Factory.StartNew(() => driver.Run(cts.Token));

            string resultFileName = "assembledResult";

            string upvBioURIPrefix = "http://www.upvbio.eu/cloud/demo/gw/UPVBIOApp/";
            string assemblerAppIdentificationURI = upvBioURIPrefix + "Assembler";

            var account = CloudStorageAccount.Parse(CloudSettings.UserDataStoreConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer appDataContainer = blobClient.GetContainerReference("applicationcontainer");
            appDataContainer.CreateIfNotExist();


            // method for file uploading
            Func<string, string> uploadText = ((blobname) =>
            {
                var blob = userDataContainer.GetBlobReference(blobname);
                blob.UploadText("dsfsfsfsdfsdfsfsf");
                var blobAddress = blob.Uri.AbsoluteUri;

                return blobAddress;
            });

            Func<string, string> computeName = ((name) =>
            {
                var result = userDataContainer.GetBlobReference(name).Uri.AbsoluteUri;

                return result;
            });

            var assemblerJob = new VENUSJobDescription()
            {
                ApplicationIdentificationURI = assemblerAppIdentificationURI,
                CustomerJobID = "UPVBIO Assembler Job " + DateTime.Now.ToLocalTime().ToString(),
                JobName = "some job name",
                AppPkgReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(assemblerAppIdentificationURI) + "_App"), CloudSettings.UserDataStoreConnectionString)),
                AppDescReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(assemblerAppIdentificationURI) + "_Desc"), CloudSettings.UserDataStoreConnectionString)),
                JobArgs = new ArgumentCollection()
                {
                        new SingleReference
                        {
                            Name="resultFileName",
                            Ref = new Reference(resultFileName, new AzureBlobReference(computeName(resultFileName), CloudSettings.UserDataStoreConnectionString)),
                        }
                }
            };

            assemblerJob.DownloadsReference = new Reference(new AzureBlobReference(uploadText(resultFileName), CloudSettings.UserDataStoreConnectionString));
            var submissionPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);

            CreateActivityResponse resp = submissionPortal.SubmitVENUSJob(assemblerJob);
        }

        [TestMethod]
        public void EndToEndScenarioWithoutAppRepo()
        {
            Task.Factory.StartNew(() => driver.Run(cts.Token));

            string applicationIdentificationURI = "http://www.microsoft.com/emic/cloud/demo/SimpleMathApp";

            VENUSApplicationDescription appDesc = new VENUSApplicationDescription()
            {
                ApplicationIdentificationURI = applicationIdentificationURI,
                CommandTemplate = new VENUSCommandTemplate()
                {
                    Path = string.Empty,
                    Executable = "SimpleMathConsoleApp.exe",
                    Args = new List<CommandLineArgument>() 
                    {
                        new CommandLineArgument() { 
                            Name = "InputFile", 
                            FormatString = "-infile {0}", 
                            Required = Required.Mandatory,
                            CommandLineArgType = CommandLineArgType.SingleReferenceInputArgument
                        },
                        new CommandLineArgument() { 
                            Name = "OutputFile", 
                            FormatString = "-outfile {0}", 
                            Required = Required.Mandatory,
                            CommandLineArgType = CommandLineArgType.SingleReferenceOutputArgument
                        },
                        new CommandLineArgument() {
                            Name = "Operation",
                            FormatString = "-sum",
                            Required = Required.Optional,
                            CommandLineArgType = CommandLineArgType.Switch
                        }
                    }
                }
            };

            #region Upload application

            Console.WriteLine("Uploading application");
            Func<VENUSApplicationDescription, CloudBlob> uploadAppDesc = ((appDescription) =>
            {
                var blobName = HttpUtility.UrlEncode(appDescription.ApplicationIdentificationURI) + "_Desc";
                DataContractSerializer dcs = new DataContractSerializer(typeof(VENUSApplicationDescription));
                MemoryStream msxml = new MemoryStream();
                dcs.WriteObject(msxml, appDescription);
                CloudBlob xmlBlob = userDataContainer.GetBlobReference(blobName);
                xmlBlob.Properties.ContentType = "text/xml";
                xmlBlob.UploadByteArray(msxml.ToArray());
                return xmlBlob;
            });

            Func<string, MemoryStream, CloudBlob> uploadApp = (appURI, zipBytes) =>
            {
                var blobName = HttpUtility.UrlEncode(appURI) + "_App";
                CloudBlob applicationBlob = userDataContainer.GetBlobReference(blobName);
                applicationBlob.UploadByteArray(zipBytes.ToArray());

                return applicationBlob;
            };

            MemoryStream ms = new MemoryStream();
            using (var zip = new ZipFile())
            {
                zip.AddFile(@"..\..\..\core\Test.SimpleMathConsoleApp\bin\Debug\SimpleMathConsoleApp.exe", "");
                zip.Save(ms);
            }

            CloudBlob appBlob = uploadApp(appDesc.ApplicationIdentificationURI, ms);
            CloudBlob appDescBlob = uploadAppDesc(appDesc);

            #endregion

            var appReference = new Reference(new AzureBlobReference(appBlob, CloudSettings.UserDataStoreConnectionString));
            var descReference = new Reference(new AzureBlobReference(appDescBlob, CloudSettings.UserDataStoreConnectionString));

            #region Submit job

            Func<string, string, string> upload = ((name, content) =>
            {
                var blob = userDataContainer.GetBlobReference(name);
                blob.UploadText(content);
                var blobAddress = blob.Uri.AbsoluteUri;
                return blobAddress;
            });

            Func<string, string> computeNameAndDeleteIfExist = ((name) =>
            {
                var r = userDataContainer.GetBlobReference(name);
                r.DeleteIfExists();
                return r.Uri.AbsoluteUri;
            });

            string inputFileContent = "1,2,3,4,5";
            string remoteFileAddress = upload("input2.csv", inputFileContent);
            string resultAddress = computeNameAndDeleteIfExist("result.csv");
            string resultZipName = "myresults.zip";
            string resultZipAddress = computeNameAndDeleteIfExist(resultZipName);
            #endregion

            var job = new VENUSJobDescription()
            {
                ApplicationIdentificationURI = applicationIdentificationURI,
                CustomerJobID = "EndToEndTest job ID 124",
                ResultZipFilename = resultZipName,
                AppPkgReference = appReference,
                AppDescReference = descReference,
                JobName = "Invoke to show at Aachen meeting",
                JobArgs = new ArgumentCollection()
                {
                     new AzureArgumentSingleReference {  //TODO: use AzureBlobReference
                         Name="InputFile", 
                         DataAddress = remoteFileAddress, 
                         ConnectionString = CloudSettings.UserDataStoreConnectionString
                     },
                     new AzureArgumentSingleReference
                     { 
                         Name = "OutputFile", 
                         DataAddress = resultAddress, 
                         ConnectionString = CloudSettings.UserDataStoreConnectionString
                     },
                     new SwitchArgument 
                     { 
                         Name = "Operation", 
                         Value = true 
                     }
                },
                Downloads = new ReferenceCollection()
                {
                    new Reference("default.aspx", new HttpGetReference("http://www.gutenberg.org/index.html"))                    
                },
                Uploads = new ReferenceCollection()
                {
                     new Reference(new AzureBlobReference(resultAddress,CloudSettings.UserDataStoreConnectionString)),
                     new Reference(new AzureBlobReference(resultZipAddress,CloudSettings.UserDataStoreConnectionString)),
                     new Reference(new AzureBlobReference(computeNameAndDeleteIfExist("index.html"),CloudSettings.UserDataStoreConnectionString))
                }
            };

            var container = new CompositionContainer(new AggregateCatalog(
                new AssemblyCatalog(typeof(HttpGetReference).Assembly),
                new AssemblyCatalog(typeof(AzureArgumentCatalogueReference).Assembly),
                new AssemblyCatalog(typeof(CDMIBlobReference).Assembly)
                ));
            var argumentRepository = container.GetExportedValue<ArgumentRepository>();

            var submissionPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);

            CreateActivityResponse resp = submissionPortal.SubmitVENUSJob(job);

            bool foundResult = false;

            #region Poll for result

            int attempts = 0;
            while (!foundResult)
            {
                var r = userDataContainer.GetBlobReference("result.csv");
                try
                {
                    var t = r.DownloadText();
                    if (t.Equals("15"))
                    {
                        foundResult = true;
                        break;
                    }
                }
                catch (StorageClientException ex)
                {
                    if (ex.ErrorCode != StorageErrorCode.BlobNotFound)
                    {
                        throw;
                    }

                    if (attempts++ == 200)
                    {
                        throw new TimeoutException("No result seen");
                    }

                    Thread.Sleep(2000);
                }
            }
            try
            {
                var r = userDataContainer.GetBlobReference("default.aspx");
                var t = r.DownloadText();
            }
            catch (StorageClientException ex)
            {
                if (ex.ErrorCode != StorageErrorCode.BlobNotFound)
                {
                    throw new Exception("Download or Upload task did not work");
                }
            }
            #endregion
            var internalJobId = resp.CreateActivityResponse1.ActivityIdentifier.ReferenceParameters.Any[0].InnerText;
            Func<bool> cond;
            var myJob = new List<EndpointReferenceType>();
            myJob.Add(resp.CreateActivityResponse1.ActivityIdentifier);

            cond = () => { return submissionPortal.GetActivityStatuses(myJob).GetActivityStatusesResponse1.Response[0].ActivityStatus.state == ActivityStateEnumeration.Finished; };
            TestHelper.PollForCondition(cond, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(40));
            Assert.IsTrue(cond());
            Assert.IsTrue(foundResult);
        }

    }
}
