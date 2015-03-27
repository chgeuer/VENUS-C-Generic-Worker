//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.ComponentModel.Composition.Hosting;
using Microsoft.EMIC.Cloud.DataManagement;
using Microsoft.EMIC.Cloud.Storage.Azure;
using Microsoft.EMIC.Cloud.GenericWorker.SampleNotifications;
using Microsoft.EMIC.Cloud.GenericWorker.AzureProvider;
using Microsoft.EMIC.Cloud.LiveDemo;
using Microsoft.EMIC.Cloud.GenericWorker;
using System.ServiceModel;
using Microsoft.EMIC.Cloud.Security;
using System.Security.Cryptography.X509Certificates;
using System.Configuration;
using System.ServiceModel.Security;
using PluginAttr = Microsoft.EMIC.Cloud.Notification.SerializableKeyValuePair<string, string>;
using Microsoft.EMIC.Cloud.ApplicationRepository;
using System.Web;
using System.Runtime.Serialization;
using System.IO;
using Ionic.Zip;
using Microsoft.EMIC.Cloud.Notification;
using System.Threading;
using KTH.GenericWorker.CDMI;
using System.Net;
using Microsoft.EMIC.Cloud.AzureSettings;
using Microsoft.EMIC.Cloud;

namespace Tests.Live
{
    [TestClass]
    public class LiveTests
    {

        private static CloudStorageAccount account;
        private static CloudBlobClient blobClient;
        private static CloudBlobContainer userDataContainer;
        private static CompositionContainer genericWorkerContainer;

        private static string issuerAddress;
        private static string authThumprint;
        private static string dnsEnpointId;
        private static string jobListingServiceUrl;
        private static string secScalingSvcUrl;
        private static string secNotificationServiceUrl;

        private static string applicationIdentificationURI;
        private static Reference appReference;
        private static Reference descReference;
        private static VENUSApplicationDescription appDesc;
        private static Func<string, Reference> AzureRefBuilder, CDMIRefBuilder;
        private const int mathJobExecutionTimeInSeconds = 20;
        private static string upload(string blobName, string blobContent)
        {
            var blob = userDataContainer.GetBlobReference(blobName);
            blob.UploadText(blobContent);
            var blobAddress = blob.Uri.AbsoluteUri;
            return blobAddress;
        }


        private static string computeNameAndDeleteIfExist(string blobname)
        {
            var r = userDataContainer.GetBlobReference(blobname);
            r.DeleteIfExists();
            return r.Uri.AbsoluteUri;
        }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            #region retrieving service URLs from app.config

            issuerAddress = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.STS.URL"];
            authThumprint = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.AuthCert.Thumbprint"];
            dnsEnpointId = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.AuthCert.DnsEndpointId"];
            jobListingServiceUrl = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.JobListingService.URL"];
            secScalingSvcUrl = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.SecureScalingService.URL"];
            secNotificationServiceUrl = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.SecureNotificationService.URL"];

            #endregion

            #region setting up the storage account

            account = CloudStorageAccount.Parse(LiveDemoCloudSettings.LiveTestsCloudConnectionString);
            blobClient = account.CreateCloudBlobClient();
            userDataContainer = blobClient.GetContainerReference(LiveDemoCloudSettings.UserDataStoreBlobName);
            userDataContainer.CreateIfNotExist();
            BlobContainerPermissions resultPermissions = userDataContainer.GetPermissions();
            resultPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
            userDataContainer.SetPermissions(resultPermissions);

            #endregion

            genericWorkerContainer = new CompositionContainer(
                                                 new AggregateCatalog(
                                                     new TypeCatalog(typeof(LiveDemoCloudSettings)),
                                                     new AssemblyCatalog(typeof(LiteralArgument).Assembly),
                                                     new AssemblyCatalog(typeof(AzureGWRuntimeEnvironment).Assembly),
                                                     new AssemblyCatalog(typeof(AzureArgumentCatalogueReference).Assembly),
                                                     new AssemblyCatalog(typeof(KTH.GenericWorker.CDMI.CDMIBlobReference).Assembly),
                                                     new AssemblyCatalog(typeof(AzureQueueJobStatusNotificationPlugin).Assembly)
                                                ));

            UploadMathApplication(out applicationIdentificationURI, out appReference, out descReference, out appDesc);

            #region define RefBuilders for the used storage handlers
            AzureRefBuilder = (fileName) =>
            {
                return new Reference(new AzureBlobReference(computeNameAndDeleteIfExist(fileName), LiveDemoCloudSettings.LiveTestsCloudConnectionString));
            };

            CDMIRefBuilder = (fileName) =>
            {
                bool useSecureBinding = false;
                var cdmiAddress = useSecureBinding ? "https://cdmi.pdc2.pdc.kth.se:8080" : "http://cdmi.pdc2.pdc.kth.se:2365";
                var username = "christian";
                var password = "venusc";

                var inputUri = string.Format("{0}/{1}", cdmiAddress, fileName);
                var cdmiRef = new CDMIBlobReference()
                {
                    URI = inputUri,
                    Credentials = new NetworkCredential(username, password),
                    RequestFactory = url =>
                    {
                        var request = (HttpWebRequest)WebRequest.Create(url);
                        return request;
                    }
                };
                if (cdmiRef.ExistsDataItem())
                {
                    cdmiRef.Delete();
                }
                return new Reference(fileName, cdmiRef);
            };

            #endregion
        }

        [TestInitialize]
        public void TestInitialize()
        {
            TestHelper.FlushTable(genericWorkerContainer);
            TestHelper.FlushBlobContainer(genericWorkerContainer);
        }

        /// <summary>
        /// Test title: ListJobsWhenNoJobsAreSubmittedTest
        /// Description:
        ///     a user calls GetJobs to retrieve her own lis of jobs when she actually has not submitted any jobs
        /// Author:
        ///     EMIC / emic_cce@microsoft.com
        /// Category:
        ///     Job Management, positive, cloud, end2end
        /// Running services/endpoints: 
        ///     running Job Management endpoint    
        /// Expected Results: 
        ///     The retrieved job list should be empty
        /// </summary>
        [TestMethod]
        public void ListJobsWhenNoJobsAreSubmittedTest()
        {
            var user = "Alice";

            var jobManagementPortal = GenericWorkerJobManagementClient.CreateSecureClient(
                    address: new EndpointAddress(new Uri(jobListingServiceUrl), new DnsEndpointIdentity(dnsEnpointId)),
                    issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                    username: user,
                    password: "secret",
                    serviceCert: X509Helper.GetX509Certificate2(
                        StoreLocation.LocalMachine, StoreName.My,
                        authThumprint,
                        X509FindType.FindByThumbprint));
            var jobs = jobManagementPortal.GetJobs(user);
            Assert.AreEqual(0, jobs.Count);
        }

        /// <summary>
        /// Test title: CallOperationWithWrongCertificateTest
        /// Description:
        ///     In this test a service operation is called with a client initialized with a wrong certificate
        /// Author:
        ///     EMIC / emic_cce@microsoft.com
        /// Category:
        ///     Security, negative, cloud, end2end
        /// Running services/endpoints: 
        ///     running Job Management endpoint    
        /// Expected Results: 
        ///     MessageSecurityException
        /// </summary>

        [TestMethod]
        [ExpectedException(typeof(MessageSecurityException))]
        public void CallOperationWithWrongCertificateTest()
        {
            var user = "Alice";
            var authThumprint = "0E2F80D72A59089A7B4EE0E89853EE5FD1295692";
            var jobManagementPortal = GenericWorkerJobManagementClient.CreateSecureClient(
                    address: new EndpointAddress(new Uri(jobListingServiceUrl), new DnsEndpointIdentity(dnsEnpointId)),
                    issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                    username: user,
                    password: "secret",
                    serviceCert: X509Helper.GetX509Certificate2(
                        StoreLocation.LocalMachine, StoreName.My,
                        authThumprint,
                        X509FindType.FindByThumbprint));
            var jobs = jobManagementPortal.GetJobs(user);
        }

        /// <summary>
        /// Test title: CallOperationWithWrongPasswordTest
        /// Description:
        ///     In this test a service operation is called with a client initialized with a wrong password
        /// Author:
        ///     EMIC / emic_cce@microsoft.com
        /// Category:
        ///     Security, negative, cloud, end2end
        /// Running services/endpoints: 
        ///     running Job Management endpoint    
        /// Expected Results: 
        ///     MessageSecurityException
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(MessageSecurityException))]
        public void CallOperationWithWrongPasswordTest()
        {
            var user = "Alice";

            var jobManagementPortal = GenericWorkerJobManagementClient.CreateSecureClient(
                    address: new EndpointAddress(new Uri(jobListingServiceUrl), new DnsEndpointIdentity(dnsEnpointId)),
                    issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                    username: user,
                    password: "wrong password",
                    serviceCert: X509Helper.GetX509Certificate2(
                        StoreLocation.LocalMachine, StoreName.My,
                        authThumprint,
                        X509FindType.FindByThumbprint));
            var jobs = jobManagementPortal.GetJobs(user);
        }

        /// <summary>
        /// Test title: JobListingServiceUserGetAllJobsTest
        /// Description:
        ///     In this test a user tries to use the GetAllJobs operation which is only for the admin user
        /// Author:
        ///     EMIC / emic_cce@microsoft.com
        /// Category:
        ///     Security, negative, cloud, end2end
        /// Running services/endpoints: 
        ///     running Job Management endpoint    
        /// Expected Results: 
        ///     SecurityAccessDeniedException
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(SecurityAccessDeniedException))]
        public void JobListingServiceUserGetAllJobsTest()
        {
            var user = "Alice";

            var jobManagementPortal = GenericWorkerJobManagementClient.CreateSecureClient(
                    address: new EndpointAddress(new Uri(jobListingServiceUrl), new DnsEndpointIdentity(dnsEnpointId)),
                    issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                    username: user,
                    password: "secret",
                    serviceCert: X509Helper.GetX509Certificate2(
                        StoreLocation.LocalMachine, StoreName.My,
                        authThumprint,
                        X509FindType.FindByThumbprint));
            var jobs = jobManagementPortal.GetAllJobs();
        }

        /// <summary>
        /// Test title: JobListingServiceUserGetAllJobsTest
        /// Description:
        ///     In this test a user tries to use the GetAllJobs operation which is only for the admin user
        /// Author:
        ///     EMIC / emic_cce@microsoft.com
        /// Category:
        ///     Security, negative, cloud, end2end
        /// Running services/endpoints: 
        ///     running Job Management endpoint    
        /// Expected Results: 
        ///     SecurityAccessDeniedException
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(FaultException<OGF.BES.Faults.NotAuthorizedFaultType>))]
        public void JobListingServiceUserGetJobsOfOtherUserTest()
        {
            var alice = "Alice";
            var bob = "Bob";
            var aliceJobManagementClient = GenericWorkerJobManagementClient.CreateSecureClient(
                    address: new EndpointAddress(new Uri(jobListingServiceUrl), new DnsEndpointIdentity(dnsEnpointId)),
                    issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                    username: alice,
                    password: "secret",
                    serviceCert: X509Helper.GetX509Certificate2(
                        StoreLocation.LocalMachine, StoreName.My,
                        authThumprint,
                        X509FindType.FindByThumbprint));
            aliceJobManagementClient.GetJobs(bob);
        }

        /// <summary>
        /// Test title: UserUpdateDeploymentTest
        /// Description:
        ///     In this test a user tries to use the UpdateDeployment operation which is only for the admin user
        /// Author:
        ///     EMIC / emic_cce@microsoft.com
        /// Category:
        ///     Security, negative, cloud, end2end
        /// Running services/endpoints: 
        ///     running Scaling Service endpoint    
        /// Expected Results: 
        ///     SecurityAccessDeniedException
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(SecurityAccessDeniedException))]
        public void UserUpdateDeploymentTest()
        {
            var alice = "Alice";
            var scalingClient = ScalingServiceClient.CreateSecureClient(
                    address: new EndpointAddress(new Uri(secScalingSvcUrl), new DnsEndpointIdentity(dnsEnpointId)),
                    issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                    username: alice,
                    password: "secret",
                    serviceCert: X509Helper.GetX509Certificate2(
                        StoreLocation.LocalMachine, StoreName.My,
                        authThumprint,
                        X509FindType.FindByThumbprint));
            scalingClient.UpdateDeployment(new DeploymentSize());
        }

        /// <summary>
        /// Test title: AdminGetAndUpdateDeploymentTest
        /// Description:
        ///     In this test an admin retrieves the number of deployed instances and updates this number
        /// Author:
        ///     EMIC / emic_cce@microsoft.com
        /// Category:
        ///     Resource Management, positive, cloud, end2end
        /// Running services/endpoints: 
        ///     running Scaling Service endpoint    
        /// Expected Results: 
        ///     the number of deployed instances should match the updated value (oldvalue+1)%6+1
        /// </summary>
        //[TestMethod] //takes too long
        //[Timeout(40*60*1000)]
        public void AdminGetAndUpdateDeploymentTest()
        {
            var alice = "Administrator";
            var scalingClient = ScalingServiceClient.CreateSecureClient(
                    address: new EndpointAddress(new Uri(secScalingSvcUrl), new DnsEndpointIdentity(dnsEnpointId)),
                    issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                    username: alice,
                    password: "secret",
                    serviceCert: X509Helper.GetX509Certificate2(
                        StoreLocation.LocalMachine, StoreName.My,
                        authThumprint,
                        X509FindType.FindByThumbprint));
            var cloud = "Azure";
            var currInstanceCount = scalingClient.ListDeployments().Where(d => d.CloudProviderID == cloud).Select(d => d.InstanceCount).First();
            var newInstanceCount = ((currInstanceCount + 1) % 6) + 1;
            scalingClient.UpdateDeployment(new DeploymentSize() { CloudProviderID = cloud, InstanceCount = newInstanceCount });
            Func<bool> condition = () => scalingClient.ListDeployments().Where(d => d.CloudProviderID == cloud && d.InstanceCount == newInstanceCount).Count() == 1;
            PollForCondition(condition, TimeSpan.FromMilliseconds(500), TimeSpan.FromMinutes(35));
            Assert.IsTrue(condition());
        }

        /// <summary>
        /// Test title: NotificationServiceTest
        /// Description:
        ///     the user submits a job and subscribes on different statuses using 3 different notification plugin configs for 3 different queues
        ///     the allstatuses,runningstatus and finishedstatus queue
        /// Author:
        ///     EMIC / emic_cce@microsoft.com
        /// Category:
        ///     Job Management, positive, cloud, end2end
        /// Running services/endpoints: 
        ///     running Job Management endpoint    
        /// Expected Results: 
        ///     1 message in runningstatus queue, 1 in finishedstatus and more than 2 messages in allstatusesqueue
        /// </summary>            
        //[TestMethod]
        public void NotificationServiceTest()
        {

            var queueClient = account.CreateCloudQueueClient();

            var allstatuses = "allstatuses";
            var runningstatus = "runningstatus";
            var finishedstatus = "finishedstatus";

            var allqueue = queueClient.GetQueueReference(allstatuses);
            var runningqueue = queueClient.GetQueueReference(runningstatus);
            var finishedqueue = queueClient.GetQueueReference(finishedstatus);

            if (allqueue.Exists()) allqueue.Delete();
            if (runningqueue.Exists()) runningqueue.Delete();
            if (finishedqueue.Exists()) finishedqueue.Delete();

            var alice = "Alice";
            var aliceJobManagementClient = GenericWorkerJobManagementClient.CreateSecureClient(
                    address: new EndpointAddress(new Uri(jobListingServiceUrl), new DnsEndpointIdentity(dnsEnpointId)),
                    issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                    username: alice,
                    password: "secret",
                    serviceCert: X509Helper.GetX509Certificate2(
                        StoreLocation.LocalMachine, StoreName.My,
                        authThumprint,
                        X509FindType.FindByThumbprint));

            Func<bool> cond = () => aliceJobManagementClient.GetJobs(alice).Count() == 0;
            PollForCondition(
                cond,
                TimeSpan.FromMilliseconds(10), TimeSpan.FromSeconds(60));
            Assert.IsTrue(cond());   

            string applicationIdentificationURI;
            Reference appReference;
            Reference descReference;
            VENUSApplicationDescription appDesc;
            UploadMathApplication(out applicationIdentificationURI, out appReference, out descReference, out appDesc);

            #region Submit job

            string inputFileContent = "1,2,3,4,5";
            string inputFileName = "input2.csv";
            string resultFileName = "result.csv";

            var job = GetMathJob(AzureRefBuilder, inputFileName, resultFileName);

            var aliceJob = aliceJobManagementClient.SubmitVENUSJob(job).CreateActivityResponse1.ActivityIdentifier;
            #endregion

            var aliceNotificationClient = NotificationServiceClient.CreateSecureClient(
                    address: new EndpointAddress(new Uri(secNotificationServiceUrl), new DnsEndpointIdentity(dnsEnpointId)),
                    issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                    username: alice,
                    password: "secret",
                    serviceCert: X509Helper.GetX509Certificate2(
                        StoreLocation.LocalMachine, StoreName.My,
                        authThumprint,
                        X509FindType.FindByThumbprint));

            var pluginConfigAllQueue = new List<PluginAttr>();// 
            pluginConfigAllQueue.Add(new PluginAttr(PluginConfigMandatoryKeys.NAME, JobStatusNotificationPluginNames.AZURE_QUEUE));
            pluginConfigAllQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.QUEUE_NAME, allstatuses));
            pluginConfigAllQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.MESSAGE, "msg1: I like all statuses"));
            pluginConfigAllQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.CONNECTION_STRING, LiveDemoCloudSettings.LiveTestsCloudConnectionString));

            var pluginConfigRunningQueue = new List<PluginAttr>();// 
            pluginConfigRunningQueue.Add(new PluginAttr(PluginConfigMandatoryKeys.NAME, JobStatusNotificationPluginNames.AZURE_QUEUE));
            pluginConfigRunningQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.QUEUE_NAME, runningstatus));
            pluginConfigRunningQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.MESSAGE, "msg1: I like running statuses"));
            pluginConfigRunningQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.CONNECTION_STRING, LiveDemoCloudSettings.LiveTestsCloudConnectionString));

            var pluginConfigFinishedQueue = new List<PluginAttr>();// 
            pluginConfigFinishedQueue.Add(new PluginAttr(PluginConfigMandatoryKeys.NAME, JobStatusNotificationPluginNames.AZURE_QUEUE));
            pluginConfigFinishedQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.QUEUE_NAME, finishedstatus));
            pluginConfigFinishedQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.MESSAGE, "msg1: I like finished statuses"));
            pluginConfigFinishedQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.CONNECTION_STRING, LiveDemoCloudSettings.LiveTestsCloudConnectionString));

            PollForCondition(
                () => aliceJobManagementClient.GetJobs(alice).Count() == 1,
                TimeSpan.FromMilliseconds(10), TimeSpan.FromSeconds(60));

            aliceNotificationClient.CreateSubscriptionForStatuses(aliceJob, new List<JobStatus>() { JobStatus.Running }, pluginConfigRunningQueue);
            aliceNotificationClient.CreateSubscriptionForStatuses(aliceJob, new List<JobStatus>() { JobStatus.Finished }, pluginConfigFinishedQueue);
            aliceNotificationClient.CreateSubscription(aliceJob, pluginConfigAllQueue);

            //trigger execution
            upload(inputFileName, inputFileContent);

            PollForOutputFiles(appDesc, new List<VENUSJobDescription>() { job }, TimeSpan.FromSeconds(120));
            Thread.Sleep(10 * 1000);
            Assert.AreEqual(1, runningqueue.PeekMessages(10).Count());
            Assert.IsTrue(finishedqueue.PeekMessages(10).Count() > 0); 
            Assert.IsTrue(allqueue.PeekMessages(10).Count() > 2);
        }

        /// <summary>
        /// Test title: NotificationServiceSubscribeOnOtherUsersJobsTest
        /// Description:
        ///     User Bob tries to subscribe on the jobs of user Alice
        /// Author:
        ///     EMIC / emic_cce@microsoft.com
        /// Category:
        ///     Security, negative, cloud, end2end
        /// Running services/endpoints: 
        ///     running Job Management endpoint    
        /// Expected Results: 
        ///     SecurityAccessDeniedException
        /// </summary>        
        [TestMethod]
        [ExpectedException(typeof(FaultException<OGF.BES.Faults.NotAuthorizedFaultType>))]
        public void NotificationServiceSubscribeOnOtherUsersJobsTest()
        {
            var alice = "Alice";
            var bob = "Bob";

            var aliceJobManagementClient = GenericWorkerJobManagementClient.CreateSecureClient(
                    address: new EndpointAddress(new Uri(jobListingServiceUrl), new DnsEndpointIdentity(dnsEnpointId)),
                    issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                    username: alice,
                    password: "secret",
                    serviceCert: X509Helper.GetX509Certificate2(
                        StoreLocation.LocalMachine, StoreName.My,
                        authThumprint,
                        X509FindType.FindByThumbprint));

            aliceJobManagementClient.SubmitVENUSJob(TestHelper.CreateJSDL);
            var aliceJobs = aliceJobManagementClient.GetJobs(alice);
            Assert.AreEqual(1, aliceJobs.Count);
            var aliceSingleJob = aliceJobs.First();

            var bobNotificationClient = NotificationServiceClient.CreateSecureClient(
                    address: new EndpointAddress(new Uri(secNotificationServiceUrl), new DnsEndpointIdentity(dnsEnpointId)),
                    issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                    username: bob,
                    password: "secret",
                    serviceCert: X509Helper.GetX509Certificate2(
                        StoreLocation.LocalMachine, StoreName.My,
                        authThumprint,
                        X509FindType.FindByThumbprint));

            var pluginConfigAllQueue = new List<PluginAttr>();
            pluginConfigAllQueue.Add(new PluginAttr(PluginConfigMandatoryKeys.NAME, JobStatusNotificationPluginNames.AZURE_QUEUE));
            pluginConfigAllQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.QUEUE_NAME, "queuename"));
            pluginConfigAllQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.MESSAGE, "msg1: I like all statuses"));
            pluginConfigAllQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.CONNECTION_STRING, LiveDemoCloudSettings.LiveTestsCloudConnectionString));

            bobNotificationClient.CreateSubscription(aliceSingleJob, pluginConfigAllQueue);
        }

        /// <summary>
        /// Test title: NotificationSubscribeOnTerminatedJobTest
        /// Description:
        ///     User Alice tries to subscribe on a terminated job
        /// Author:
        ///     EMIC / emic_cce@microsoft.com
        /// Category:
        ///     Job Management, negative, cloud, end2end
        /// Running services/endpoints: 
        ///     running Job Management endpoint    
        /// Expected Results: 
        ///     FaultException
        /// </summary>            
        [TestMethod]
        [ExpectedException(typeof(FaultException<OGF.BES.Faults.CantApplyOperationToCurrentStateFaultType>))]
        public void NotificationSubscribeOnTerminatedJobTest()
        {
            var alice = "Alice";

            var aliceJobManagementClient = GenericWorkerJobManagementClient.CreateSecureClient(
                    address: new EndpointAddress(new Uri(jobListingServiceUrl), new DnsEndpointIdentity(dnsEnpointId)),
                    issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                    username: alice,
                    password: "secret",
                    serviceCert: X509Helper.GetX509Certificate2(
                        StoreLocation.LocalMachine, StoreName.My,
                        authThumprint,
                        X509FindType.FindByThumbprint));

            var aliceNotificationClient = NotificationServiceClient.CreateSecureClient(
                    address: new EndpointAddress(new Uri(secNotificationServiceUrl), new DnsEndpointIdentity(dnsEnpointId)),
                    issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                    username: alice,
                    password: "secret",
                    serviceCert: X509Helper.GetX509Certificate2(
                        StoreLocation.LocalMachine, StoreName.My,
                        authThumprint,
                        X509FindType.FindByThumbprint));

            aliceJobManagementClient.SubmitVENUSJob(TestHelper.CreateJSDL);
            var aliceJobs = aliceJobManagementClient.GetJobs(alice);
            Assert.AreEqual(1, aliceJobs.Count);
            var aliceSingleJob = aliceJobs.First();

            var pluginConfigAllQueue = new List<PluginAttr>();
            pluginConfigAllQueue.Add(new PluginAttr(PluginConfigMandatoryKeys.NAME, JobStatusNotificationPluginNames.AZURE_QUEUE));
            pluginConfigAllQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.QUEUE_NAME, "queuename"));
            pluginConfigAllQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.MESSAGE, "msg1: I like all statuses"));
            pluginConfigAllQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.CONNECTION_STRING, LiveDemoCloudSettings.LiveTestsCloudConnectionString));
            PollForCondition(
                () => aliceJobManagementClient.GetActivityStatuses(aliceJobs).GetActivityStatusesResponse1.Response.Where(r => r.ActivityStatus.state == OGF.BES.ActivityStateEnumeration.Failed).Count() == 1,
                TimeSpan.FromMilliseconds(10), TimeSpan.FromSeconds(60));
            Thread.Sleep(10000);
            aliceNotificationClient.CreateSubscription(aliceSingleJob, pluginConfigAllQueue);
        }

        /// <summary>
        /// Test title: UserTryingToCancelOtherUsersJobTest
        /// Description:
        ///     User Bob tries to cancel the jobs of user Alice
        /// Author:
        ///     EMIC / emic_cce@microsoft.com
        /// Category:
        ///     Security, negative, cloud, end2end
        /// Running services/endpoints: 
        ///     running Job Management endpoint    
        /// Expected Results: 
        ///     SecurityAccessDeniedException
        /// </summary>        
        [TestMethod]
        public void UserTryingToCancelOtherUsersJobTest()
        {
            var alice = "Alice";
            var bob = "Bob";

            var aliceJobManagementClient = GenericWorkerJobManagementClient.CreateSecureClient(
                    address: new EndpointAddress(new Uri(jobListingServiceUrl), new DnsEndpointIdentity(dnsEnpointId)),
                    issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                    username: alice,
                    password: "secret",
                    serviceCert: X509Helper.GetX509Certificate2(
                        StoreLocation.LocalMachine, StoreName.My,
                        authThumprint,
                        X509FindType.FindByThumbprint));

            var bobJobManagementClient = GenericWorkerJobManagementClient.CreateSecureClient(
                    address: new EndpointAddress(new Uri(jobListingServiceUrl), new DnsEndpointIdentity(dnsEnpointId)),
                    issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                    username: bob,
                    password: "secret",
                    serviceCert: X509Helper.GetX509Certificate2(
                        StoreLocation.LocalMachine, StoreName.My,
                        authThumprint,
                        X509FindType.FindByThumbprint));

            aliceJobManagementClient.SubmitVENUSJob(TestHelper.CreateJSDL);
            var aliceJobs = aliceJobManagementClient.GetJobs(alice);
            Assert.AreEqual(1, aliceJobs.Count);

            var resp = bobJobManagementClient.TerminateActivities(aliceJobs);
            var fault = resp.TerminateActivitiesResponse1.Response[0].Fault.faultstring;
            Assert.IsTrue(fault.StartsWith(string.Format(ExceptionMessages.UnauthenticatedCall,"")));
        }

        /// <summary>
        /// Test title: SimpleJobSubmissionExecutionTest
        /// Description:
        ///     One job is submitted 
        /// Author:
        ///     EMIC / emic_cce@microsoft.com
        /// Category:
        ///     Job Management, postive, cloud, end2end
        /// Running services/endpoints: 
        ///     running Job Management endpoint    
        /// Expected Results: 
        ///     SecurityAccessDeniedException
        /// </summary>  
        [TestMethod]
        public void SimpleJobSubmissionExecutionTest()
        {
            var alice = "Alice";
            var aliceJobManagementClient = GenericWorkerJobManagementClient.CreateSecureClient(
                    address: new EndpointAddress(new Uri(jobListingServiceUrl), new DnsEndpointIdentity(dnsEnpointId)),
                    issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                    username: alice,
                    password: "secret",
                    serviceCert: X509Helper.GetX509Certificate2(
                        StoreLocation.LocalMachine, StoreName.My,
                        authThumprint,
                        X509FindType.FindByThumbprint));

            string applicationIdentificationURI;
            Reference appReference;
            Reference descReference;
            VENUSApplicationDescription appDesc;
            UploadMathApplication(out applicationIdentificationURI, out appReference, out descReference, out appDesc);

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
            string inputFileName = "input2.csv";
            string resultFileName = "result.csv";
            var inputBlob = userDataContainer.GetBlobReference(inputFileName);
            inputBlob.DeleteIfExists();
            string remoteFileAddress = inputBlob.Uri.AbsoluteUri;

            string resultAddress = computeNameAndDeleteIfExist(resultFileName);
            string resultZipName = "myresults.zip";
            string resultZipAddress = computeNameAndDeleteIfExist(resultZipName);


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
                         ConnectionString = LiveDemoCloudSettings.LiveTestsCloudConnectionString
                     },
                     new AzureArgumentSingleReference
                     { 
                         Name = "OutputFile", 
                         DataAddress = resultAddress, 
                         ConnectionString = LiveDemoCloudSettings.LiveTestsCloudConnectionString
                     },
                     new SwitchArgument 
                     { 
                         Name = "Operation", 
                         Value = true 
                     }
                }
            };

            var aliceJob = aliceJobManagementClient.SubmitVENUSJob(job).CreateActivityResponse1.ActivityIdentifier;
            #endregion

            PollForCondition(
                () => aliceJobManagementClient.GetJobs(alice).Count() == 1,
                TimeSpan.FromMilliseconds(10), TimeSpan.FromSeconds(60));

            //trigger execution
            upload(inputFileName, inputFileContent);

            #region Poll for result
            var foundResult = false;
            int attempts = 0;
            while (!foundResult)
            {
                var r = userDataContainer.GetBlobReference(resultFileName);
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
            #endregion

            Assert.IsTrue(foundResult);
        }

        [TestMethod]
        public void JobSubmissionExecutionTest()
        {
            var alice = "Alice";
            var aliceJobManagementClient = GenericWorkerJobManagementClient.CreateSecureClient(
                    address: new EndpointAddress(new Uri(jobListingServiceUrl), new DnsEndpointIdentity(dnsEnpointId)),
                    issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                    username: alice,
                    password: "secret",
                    serviceCert: X509Helper.GetX509Certificate2(
                        StoreLocation.LocalMachine, StoreName.My,
                        authThumprint,
                        X509FindType.FindByThumbprint));

            #region Submit job

            string inputFileContent = "1,2,3,4,5";
            string inputFileName = "input2.csv";

            #endregion

            var refBuilders = new List<Func<string, Reference>>() { AzureRefBuilder}; //, CDMIRefBuilder };
            foreach (var refBuilder in refBuilders)
            {
                Console.WriteLine("submitting 5 jobs");
                var jobcount = 1;
                var jobs = Enumerable.Range(0, jobcount).Select(i => GetMathJob(refBuilder, inputFileName, string.Format("result{0}.csv", i))).ToList();
                jobs.ForEach(j => { aliceJobManagementClient.SubmitVENUSJob(j); Thread.Sleep(TimeSpan.FromMilliseconds(100)); });

                PollForCondition(
                    () => aliceJobManagementClient.GetJobs(alice).Count() == jobcount,
                    TimeSpan.FromMilliseconds(10), TimeSpan.FromSeconds(60));

                var inputRef = refBuilder(inputFileName);
                File.WriteAllText(inputFileName, inputFileContent);
                inputRef.Upload(".");

                PollForOutputFiles(appDesc, jobs, TimeSpan.FromSeconds(5*mathJobExecutionTimeInSeconds));
                aliceJobManagementClient.RemoveTerminatedJobs(alice);
            }
        }

        [TestMethod]
        public void JobGroupingServiceTest()
        {
            var group = "blastJobs";
            var job1 = "blast1";
            var job2 = "blast2";

            var jsdlJob1 = TestHelper.CreateGroupJSDL(group, job1);
            var jsdlJob2 = TestHelper.CreateGroupJSDL(group, job2);
            var user = "Researcher";
            var researchJobManagementClient = GenericWorkerJobManagementClient.CreateSecureClient(
                address: new EndpointAddress(new Uri(jobListingServiceUrl), new DnsEndpointIdentity(dnsEnpointId)),
                issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                username: user,
                password: "secret",
                serviceCert: X509Helper.GetX509Certificate2(
                    StoreLocation.LocalMachine, StoreName.My,
                    authThumprint,
                    X509FindType.FindByThumbprint));

            var endp1 = researchJobManagementClient.SubmitVENUSJob(jsdlJob1);
            var endp2 = researchJobManagementClient.SubmitVENUSJob(jsdlJob2);
            var otherendp = researchJobManagementClient.SubmitVENUSJob(TestHelper.CreateJSDL);
            var jobgroup = researchJobManagementClient.GetJobsByGroup(user, group);
            Assert.AreEqual(jobgroup.Count, 2);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<OGF.BES.Faults.NotAuthorizedFaultType>))]
        public void GetJobGroupOfOtherUser()
        {
            var group = "blastJobs";
            var job1 = "blast1";
            var job2 = "blast2";

            var jsdlJob1 = TestHelper.CreateGroupJSDL(group, job1);
            var jsdlJob2 = TestHelper.CreateGroupJSDL(group, job2);
            var user = "Researcher";
            var researchJobManagementClient = GenericWorkerJobManagementClient.CreateSecureClient(
                address: new EndpointAddress(new Uri(jobListingServiceUrl), new DnsEndpointIdentity(dnsEnpointId)),
                issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                username: user,
                password: "secret",
                serviceCert: X509Helper.GetX509Certificate2(
                    StoreLocation.LocalMachine, StoreName.My,
                    authThumprint,
                    X509FindType.FindByThumbprint));

            var endp1 = researchJobManagementClient.SubmitVENUSJob(jsdlJob1);
            var endp2 = researchJobManagementClient.SubmitVENUSJob(jsdlJob2);
            var otherendp = researchJobManagementClient.SubmitVENUSJob(TestHelper.CreateJSDL);
            var jobgroup = researchJobManagementClient.GetJobsByGroup("other user", group);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<OGF.BES.Faults.NotAuthorizedFaultType>))]
        public void CancelOtherUsersJobGroup()
        {
            var group = "blastJobs";
            var job1 = "blast1";
            var job2 = "blast2";

            var jsdlJob1 = TestHelper.CreateGroupJSDL(group, job1);
            var jsdlJob2 = TestHelper.CreateGroupJSDL(group, job2);
            var alice = "Alice";
            var bob = "Bob";
            var aliceJobManagementClient = GenericWorkerJobManagementClient.CreateSecureClient(
                address: new EndpointAddress(new Uri(jobListingServiceUrl), new DnsEndpointIdentity(dnsEnpointId)),
                issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                username: alice,
                password: "secret",
                serviceCert: X509Helper.GetX509Certificate2(
                    StoreLocation.LocalMachine, StoreName.My,
                    authThumprint,
                    X509FindType.FindByThumbprint));

            var endp1 = aliceJobManagementClient.SubmitVENUSJob(jsdlJob1);
            var endp2 = aliceJobManagementClient.SubmitVENUSJob(jsdlJob2);
            var otherendp = aliceJobManagementClient.SubmitVENUSJob(TestHelper.CreateJSDL);
            var jobgroup = aliceJobManagementClient.GetJobsByGroup("other user", group);

            var bobJobManagementClient = GenericWorkerJobManagementClient.CreateSecureClient(
                address: new EndpointAddress(new Uri(jobListingServiceUrl), new DnsEndpointIdentity(dnsEnpointId)),
                issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                username: bob,
                password: "secret",
                serviceCert: X509Helper.GetX509Certificate2(
                    StoreLocation.LocalMachine, StoreName.My,
                    authThumprint,
                    X509FindType.FindByThumbprint));
            bobJobManagementClient.CancelGroup(alice, group);
        }

        [TestMethod]
        public void HugeJobList_GetJobsTest()
        {
            var rt = new TwoRuntimes(genericWorkerContainer);
            var worker = rt.RT0;
            var submission = rt.RT1;
            var pagesize = 100; //same value as used in the ServiceConfiguration.cscfg
            var jobcount = pagesize + 10;
            var user = "Administrator";
            Enumerable.Range(0, jobcount).ToList().ForEach(i => { var job = submission.SubmitJob(user, Guid.NewGuid().ToString(), TestHelper.CreateJSDL); });

            var jobManagementPortal = GenericWorkerJobManagementClient.CreateSecureClient(
                address: new EndpointAddress(new Uri(jobListingServiceUrl), new DnsEndpointIdentity(dnsEnpointId)),
                issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                username: user,
                password: "secret",
                serviceCert: X509Helper.GetX509Certificate2(
                    StoreLocation.LocalMachine, StoreName.My,
                    authThumprint,
                    X509FindType.FindByThumbprint));            

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

        public void MassiveAmountsOfJobsTest()
        {
            var alice = "Alice";
            var aliceJobManagementClient = GenericWorkerJobManagementClient.CreateSecureClient(
                    address: new EndpointAddress(new Uri(jobListingServiceUrl), new DnsEndpointIdentity(dnsEnpointId)),
                    issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                    username: alice,
                    password: "secret",
                    serviceCert: X509Helper.GetX509Certificate2(
                        StoreLocation.LocalMachine, StoreName.My,
                        authThumprint,
                        X509FindType.FindByThumbprint));

            var jobcount = 350;
            Console.WriteLine(string.Format("submitting {0} jobs", jobcount));

            string inputFileContent = "1,2,3,4,5";
            string inputFileName = "input2.csv";

            var jobs = Enumerable.Range(0, jobcount).Select(i => GetMathJob(AzureRefBuilder, inputFileName, string.Format("result{0}.csv", i))).ToList();
            jobs.ForEach(j => { aliceJobManagementClient.SubmitVENUSJob(j); });

            upload(inputFileName, inputFileContent);
            PollForOutputFiles(appDesc, jobs, TimeSpan.FromSeconds(350 * mathJobExecutionTimeInSeconds));
            Enumerable.Range(0, jobcount).ToList().ForEach(i => computeNameAndDeleteIfExist(string.Format("result{0}.csv", i)));
        }

        [TestMethod]
        public void AllConfigSettingsNotNullOrEmptyTest()
        {
            var settings = typeof(LiveDemoCloudSettings).GetProperties().Where(p => p.PropertyType.Name == typeof(string).Name);
            var stringsettingswithvalue = settings.Where(p => p.GetValue(null, null) != null);
            var stringsettingswithoutvalue = settings.Where(p => p.GetValue(null, null) == null);
            var settingswithdummyvalue = stringsettingswithvalue.Where(p => string.IsNullOrWhiteSpace((string)p.GetValue(null, null)));

            Console.WriteLine("Settings without value:");
            stringsettingswithoutvalue.ToList().ForEach(p => Console.WriteLine(string.Format("Property: {0} Value: {1}", p.Name, (string)p.GetValue(null, null))));
            Console.WriteLine("*********************************************************");
            Console.WriteLine("Settings with dummy value:");
            settingswithdummyvalue.ToList().ForEach(p => Console.WriteLine(string.Format("Property: {0} Value: {1}", p.Name, (string)p.GetValue(null, null))));
            Console.WriteLine("*********************************************************");
            Console.WriteLine("Settings with value:");
            stringsettingswithvalue.ToList().ForEach(p => Console.WriteLine(string.Format("Property: {0} Value:  {1}", p.Name, (string)p.GetValue(null, null))));

            Assert.AreEqual(0, stringsettingswithoutvalue.Count(), "There are missing settings - check your registry or app.config");
        }

        private void PollForCondition(Func<bool> pred, TimeSpan pollinterval, TimeSpan pollduration)
        {
            ActAndPollForCondition(() => { }, pred, pollinterval, pollduration);
        }

        private void ActAndPollForCondition(Action act, Func<bool> pred, TimeSpan pollinterval, TimeSpan pollduration)
        {
            if (pollinterval > pollduration)
            {
                throw new ArgumentException("pollduration should be longer than the pollinterval");
            }
            if (pollinterval.Ticks <= 0)
            {
                throw new ArgumentException("pollinterval should be a postive value");
            }
            DateTime startTime = DateTime.Now;
            while (DateTime.Now - startTime > pollduration)
            {
                act();
                if (pred()) return;
                System.Threading.Thread.Sleep(pollinterval);
            }
        }

        private static void PollForOutputFiles(VENUSApplicationDescription appDesc, List<VENUSJobDescription> jobs, TimeSpan pollDuration)
        {
            var OutRefs = jobs.SelectMany(j => j.JobArgs).OfType<SingleReference>().Where(sr => sr.IsOutput(appDesc));
            DateTime startTime = DateTime.Now;
            var pollIntervall = (int)Math.Min(pollDuration.TotalMilliseconds, 500);
            while (!OutRefs.All(or => or.ExistsDataItem()))
            {
                if (DateTime.Now - startTime > pollDuration)
                {
                    throw new TimeoutException("resultfile not seen");
                }
                Thread.Sleep(pollIntervall);
            }
        }

        private static void UploadMathApplication(out string applicationIdentificationURI, out Reference appReference, out Reference descReference, out VENUSApplicationDescription appDesc)
        {
            applicationIdentificationURI = "http://www.microsoft.com/emic/cloud/demo/SimpleMathApp";
            appDesc = new VENUSApplicationDescription()
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

            appReference = new Reference(new AzureBlobReference(appBlob, LiveDemoCloudSettings.LiveTestsCloudConnectionString));
            descReference = new Reference(new AzureBlobReference(appDescBlob, LiveDemoCloudSettings.LiveTestsCloudConnectionString));
        }

        private static VENUSJobDescription GetMathJob(Func<string, Reference> refBuilderForSomeStorage, string inputFileName, string resultFileName)
        {
            Func<Func<string, Reference>, string, string, VENUSJobDescription> GetMathJob_f = (refBuilder, inputfileName, resultfileName) =>
            {
                var job = new VENUSJobDescription()
                {
                    ApplicationIdentificationURI = applicationIdentificationURI,
                    CustomerJobID = "EndToEndTest job ID 124",
                    AppPkgReference = appReference,
                    AppDescReference = descReference,
                    JobName = "Invoke to show at Aachen meeting",
                    JobArgs = new ArgumentCollection()
                    {
                         new SingleReference{  
                             Name = "InputFile", 
                             Ref = refBuilder(inputfileName)
                         },
                         new SingleReference
                         { 
                             Name = "OutputFile", 
                             Ref = refBuilder(resultfileName)
                         },
                         new SwitchArgument 
                         { 
                             Name = "Operation", 
                             Value = true 
                         }
                    }
                };
                return job;
            };
            return GetMathJob_f(refBuilderForSomeStorage, inputFileName, resultFileName);
        }


    }
}
