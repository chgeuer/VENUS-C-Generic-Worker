//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EMIC.Cloud.Administrator.Host;
using Microsoft.EMIC.Cloud.DataManagement;
using Microsoft.EMIC.Cloud.Demo;
using Microsoft.EMIC.Cloud.GenericWorker;
using Microsoft.EMIC.Cloud.GenericWorker.AzureProvider;
using Microsoft.EMIC.Cloud.GenericWorker.SampleNotifications;
using Microsoft.EMIC.Cloud.Storage.Azure;
using Microsoft.EMIC.Cloud.UserAdministration;
using Microsoft.EMIC.Cloud.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.EMIC.Cloud.ApplicationRepository;
using System.Xml;
using System.Web;
using OGF.BES;

namespace Tests
{
    /// <summary>
    /// All tests in this class are long running tests, at the moment they are inactive -> TODO: move these tests to an own assembly and run them on the buildserver
    /// </summary>
    [TestClass]
    public class StressTests
    {
        protected const bool IsSecurityEnabled = false;

        protected static ServiceHost genericWorkerHost;
        protected static ServiceHost scalingServiceHost;
        protected static ServiceHost notificationServiceHost;

        protected static CloudStorageAccount account;
        protected static CloudBlobClient blobClient;
        protected static CloudBlobContainer userDataContainer;
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

        //[TestMethod]
        public void MassiveAmountsOfJobsTest()
        {
            Task.Factory.StartNew(() => driver.Run(cts.Token));
            var jobManagementPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);

            var jobcount = 350;
            Console.WriteLine(string.Format("submitting {0} jobs", jobcount));

            string inputFileContent = "1,2,3,4,5";
            string inputFileName = "input2.csv";
            Func<string, Reference> AzureRefBuilder = (fileName) =>
            {
                return new Reference(new AzureBlobReference(TestHelper.computeNameAndDeleteIfExist(fileName,userDataContainer), CloudSettings.UserDataStoreConnectionString));
            };

            var jobs = new List<VENUSJobDescription>();

            for (int i = 0; i < jobcount; i++ )
            {
                var job = TestHelper.GetMathJob(AzureRefBuilder, inputFileName, string.Format("result{0}.csv", i), applicationIdentificationURI, appReference, descReference);
                jobs.Add(job);
                jobManagementPortal.SubmitVENUSJob(job);
            }

            TestHelper.upload(inputFileName, inputFileContent, userDataContainer);
            TestHelper.PollForOutputFiles(appDesc, jobs, TimeSpan.FromSeconds(jobcount * mathJobExecutionTimeInSeconds));
            Enumerable.Range(0, jobcount).ToList().ForEach(i => TestHelper.computeNameAndDeleteIfExist(string.Format("result{0}.csv", i),userDataContainer));
        }


        //[TestMethod] //Todo: make sure assembler is uploaded to the blobstore
        public void JobSubmissionUsingDownloadsRefs2kRegularFileDownloads()
        {
            Task.Factory.StartNew(() => driver.Run(cts.Token));

            string inputFile = "input_file.fasta";
            string resultFileName = "assembledResult";

            int numFragments = 2*10;

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
            Func<string, string, string> uploadFile = ((blobname, filename) =>
            {
                var blob = userDataContainer.GetBlobReference(blobname);
                blob.UploadFile(filename);
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
                uploadFile(resultfileName, inputFile);
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
                    r.FetchAttributes();
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

                    Thread.Sleep(5000);
                }
            }
            #endregion
            Assert.IsTrue(foundResult);
        }

        //[TestMethod] //Todo: make sure assembler is uploaded to the blobstore
        public void JobSubmissionUsingDownloadsRefs20kSmallFileDownloads()
        {
            Task.Factory.StartNew(() => driver.Run(cts.Token));

            string resultFileName = "assembledResult";

            int numFragments = 2 * 10;

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
            Func<string, string> uploadBlob = (blobname) =>
            {
                var blob = userDataContainer.GetBlobReference(blobname);
                blob.UploadText("test content");
                var blobAddress = blob.Uri.AbsoluteUri;

                return blobAddress;
            };

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
                uploadBlob(resultfileName);
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
                    r.FetchAttributes();
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

                    Thread.Sleep(5000);
                }
            }
            #endregion
            Assert.IsTrue(foundResult);
        }

        [TestMethod]
        public void HugeJobList_GetJobsTest()
        {
            var rt = new TwoRuntimes(genericWorkerContainer);
            var worker = rt.RT0;
            var submission = rt.RT1;
            var pagesize = CloudSettings.JobEntriesPerPage; //same value as used in BESFactoryPortTypeImplService //we can actually pass 333 job endpointrefs in a WCF message
            var jobcount = pagesize+10;
            var user = "anonymous";
            Enumerable.Range(0, jobcount).ToList().ForEach(i => { var job = submission.SubmitJob(user, Guid.NewGuid().ToString(), TestHelper.CreateJSDL);});

            var jobManagementPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);
            Func<bool> cond = () => { return (jobManagementPortal.GetNumberOfJobs(user, new List<JobStatus>() { JobStatus.Submitted }) == jobcount); };
            TestHelper.PollForCondition(cond,TimeSpan.FromMilliseconds(100),TimeSpan.FromSeconds(10));
            Assert.IsTrue(cond());

            var firstpage = jobManagementPortal.GetJobs(user, 0);
            Assert.AreEqual(pagesize, firstpage.Count);
            firstpage = jobManagementPortal.GetAllJobs(0);
            Assert.AreEqual(pagesize, firstpage.Count);
            
            var secondpage = jobManagementPortal.GetJobs(user, 1);
            Assert.AreEqual(10, secondpage.Count);
            secondpage = jobManagementPortal.GetAllJobs(1);
            Assert.AreEqual(10, secondpage.Count);

            var nonavailablePage = jobManagementPortal.GetJobs(user, 20);
            Assert.AreEqual(0, nonavailablePage.Count);

            var aliceJobs = jobManagementPortal.GetJobs(user);
            Assert.AreEqual(jobcount, aliceJobs.Count);
            aliceJobs = jobManagementPortal.GetAllJobs();
            Assert.AreEqual(jobcount, aliceJobs.Count);                   
        }

        [TestMethod]
        public void HugeJobGroup_GetJobsByGroupTest()
        {
            var rt = new TwoRuntimes(genericWorkerContainer);
            var worker = rt.RT0;
            var submission = rt.RT1;
            var pagesize = CloudSettings.JobEntriesPerPage; //same value as used in BESFactoryPortTypeImplService //we can actually pass 333 job endpointrefs in a WCF message

            var group = "blastJobs";
            var jobPrefix = "Job";
            var user = "anonymous";
            var numGroupMembers = pagesize + 10;

            for (var i = 0; i < numGroupMembers; i++)
            {
                var jobName = string.Format("{0}{1}", jobPrefix, i);
                var job = submission.SubmitJob(user, jobName, TestHelper.CreateGroupJSDL(group, jobName));
                Thread.Sleep(10);
            }

            var jobManagementPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);
            Func<bool> cond = () => { return (jobManagementPortal.GetNumberOfJobs(user, new List<JobStatus>() { JobStatus.Submitted }) == numGroupMembers); };
            TestHelper.PollForCondition(cond,TimeSpan.FromMilliseconds(100),TimeSpan.FromSeconds(10));
            Assert.IsTrue(cond());

            var firstpage = jobManagementPortal.GetJobsByGroup(user,group,0); 
            Assert.AreEqual(pagesize, firstpage.Count);

            var secondpage = jobManagementPortal.GetJobsByGroup(user, group, 1);
            Assert.AreEqual(10, secondpage.Count);

            var nonavailablePage = jobManagementPortal.GetJobsByGroup(user, group, 20);
            Assert.AreEqual(0, nonavailablePage.Count);

            var groupJobs = jobManagementPortal.GetJobsByGroup(user, group);
            Assert.AreEqual(numGroupMembers, groupJobs.Count);
        }


        [TestMethod]
        [Timeout(TestTimeout.Infinite)]
        public void HugeJobHierarchy_GetHierarchyTest2()
        {
            var rt = new TwoRuntimes(genericWorkerContainer);
            var worker1 = rt.RT0;
            var submission = rt.RT1;
            var owner = "John Doe";

            var pagesize = CloudSettings.JobEntriesPerPage; //same value as used in BESFactoryPortTypeImplService //we can actually pass 333 job endpointrefs in a WCF message
            var additonalJobs = 10;
            var jobcount = pagesize + additonalJobs;
            string rootJobId = "id-internalJobID-" + Guid.NewGuid().ToString();
            IJob rootJob = null;


            rootJob = submission.SubmitJob(owner, rootJobId, TestHelper.CreateHierarchicalJSDL("jobid://Root"));
            var rootJobEpr = TestHelper.GetEpr(rootJob);
            Assert.IsNotNull(rootJob);

            rootJob = TestHelper.GetRoot(worker1, rootJobId, rootJob);

            for (int i = 1; i < (pagesize + additonalJobs); i++)
            {
                var customerJobID = string.Format("jobid://Root/{0}", i);
                worker1.SubmitJob(owner, Guid.NewGuid().ToString(), TestHelper.CreateHierarchicalJSDL(customerJobID), true);
                Thread.Sleep(1);
            }

            var jobManagementPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);
            Func<bool> cond = () => { return (jobManagementPortal.GetNumberOfJobs(owner, new List<JobStatus>() { JobStatus.Submitted, JobStatus.Running }) == jobcount); };
            TestHelper.PollForCondition(cond,TimeSpan.FromMilliseconds(100),TimeSpan.FromSeconds(10));
            var currentNumOfJobs = jobManagementPortal.GetNumberOfJobs(owner, new List<JobStatus>() { JobStatus.Submitted, JobStatus.Running });
            Assert.IsTrue(cond(), string.Format("current number of jobs in the system: {0}, expected was: {1}",currentNumOfJobs,jobcount) );

            var firstpage = jobManagementPortal.GetHierarchy(rootJobEpr, 0);

            Assert.AreEqual(pagesize, firstpage.Count);

            var secondpage = jobManagementPortal.GetHierarchy(rootJobEpr, 1);
            Assert.AreEqual(additonalJobs, secondpage.Count);

            var nonavailablePage = jobManagementPortal.GetHierarchy(rootJobEpr, 20);
            Assert.AreEqual(0, nonavailablePage.Count);

            var allJobs = jobManagementPortal.GetHierarchy(rootJobEpr);
            Assert.AreEqual(jobcount, allJobs.Count);
        }

        //[TestMethod]
        //[ExpectedException(typeof(System.ServiceModel.CommunicationException))]
        public void HugeJobList_ExceptionIfPagingNotUsedTest()
        {
            var rt = new TwoRuntimes(genericWorkerContainer);
            var worker = rt.RT0;
            var submission = rt.RT1;
            var pagesize = CloudSettings.JobEntriesPerPage; //same value as used in BESFactoryPortTypeImplService //we can actually pass 333 job endpointrefs in a WCF message
            var jobcount = 334;
            var user = "anonymous";
            Enumerable.Range(0, jobcount).ToList().ForEach(i => { var job = submission.SubmitJob(user, Guid.NewGuid().ToString(), TestHelper.CreateJSDL); });
            var jobManagementPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);
            var firstpage = jobManagementPortal.GetJobs(user, 0);
            Assert.AreEqual(pagesize, firstpage.Count);
            firstpage = jobManagementPortal.GetAllJobs(0);
            Assert.AreEqual(pagesize, firstpage.Count);

            var secondpage = jobManagementPortal.GetJobs(user, 1);
            Assert.AreEqual(jobcount - pagesize, secondpage.Count);
            secondpage = jobManagementPortal.GetAllJobs(1);
            Assert.AreEqual(jobcount - pagesize, secondpage.Count);

            var nonavailablePage = jobManagementPortal.GetJobs(user, 20);
            Assert.AreEqual(0, nonavailablePage.Count);

            var aliceJobs = jobManagementPortal.GetJobs(user);
            Assert.AreEqual(jobcount, aliceJobs.Count);
            aliceJobs = jobManagementPortal.GetAllJobs();
            Assert.AreEqual(jobcount, aliceJobs.Count);
        }



        /// <summary>
        /// Test title: FiftyWorkersOn10JobsTest
        /// Description:
        ///     in this test 50 workers are started, then 10 jobs are submitted to the system. 
        /// Author:
        ///     EMIC / emic_cce@microsoft.com
        /// Category:
        ///     Job Management, positive, local, end2end
        /// Running services/endpoints: 
        ///     running Job Management endpoint    
        /// Expected Results: 
        ///     All jobs produce output files
        /// </summary>  
        //[TestMethod]
        //[Timeout(50 * 60 * 1000)]
        public void FiftyWorkersOn10JobsTest()
        {
            var jobManagementClient = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);

            int numGWs = 50;
            var numJobs = 10;
            DateTime startTime = DateTime.UtcNow;

            CancellationTokenSource cts = new CancellationTokenSource();

            //create and start GWs
            var containerGW = new CompositionContainer(new AggregateCatalog(
                new TypeCatalog(typeof(CloudSettings)),
                new AssemblyCatalog(typeof(GenericWorkerDriver).Assembly),
                new AssemblyCatalog(typeof(AzureGWRuntimeEnvironment).Assembly),
                new AssemblyCatalog(typeof(AzureArgumentCatalogueReference).Assembly),
                new AssemblyCatalog(typeof(KTH.GenericWorker.CDMI.CDMIBlobReference).Assembly)
            ));

            var workerTasks = new List<Task>();
            for (int i = 0; i < numGWs; i++)
            {
                var driver = containerGW.GetExportedValue<GenericWorkerDriver>();
                Console.WriteLine("startup GW {0}", i);
                workerTasks.Add(Task.Factory.StartNew(() => driver.Run(cts.Token), cts.Token));
            }

            Console.WriteLine(string.Format("Launching the workers took {0}",
                DateTime.UtcNow.Subtract(startTime).ToString()));
            Console.WriteLine(string.Format("{0}/{1} are not running",
                workerTasks.Where(t => t.Status != TaskStatus.Running).Count(),
                workerTasks.Count()));

            Console.WriteLine(string.Format("submitting {0} jobs", numJobs));

            string inputFileContent = "1,2,3,4,5";
            string inputFileName = "input2.csv";

            Func<string, Reference> AzureRefBuilder = (fileName) =>
            {
                return new Reference(new AzureBlobReference(TestHelper.computeNameAndDeleteIfExist(fileName, userDataContainer), CloudSettings.UserDataStoreConnectionString));
            };

            var jobs = Enumerable.Range(0, numJobs).Select(i => TestHelper.GetMathJob(AzureRefBuilder, inputFileName, string.Format("result{0}.csv", i),applicationIdentificationURI,appReference, descReference)).ToList();
            jobs.ForEach(j => { jobManagementClient.SubmitVENUSJob(j); });

            TestHelper.upload(inputFileName, inputFileContent, userDataContainer);
            TestHelper.PollForOutputFiles(appDesc, jobs, TimeSpan.FromSeconds(numJobs * mathJobExecutionTimeInSeconds));
            Enumerable.Range(0, numJobs).ToList().ForEach(i => TestHelper.computeNameAndDeleteIfExist(string.Format("result{0}.csv", i), userDataContainer));
            cts.Cancel();
        }
    } 
}
