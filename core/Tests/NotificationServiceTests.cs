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
using Microsoft.EMIC.Cloud.Administrator.Host;
using Microsoft.EMIC.Cloud.DataManagement;
using Microsoft.EMIC.Cloud.Demo;
using Microsoft.EMIC.Cloud.GenericWorker;
using Microsoft.EMIC.Cloud.GenericWorker.AzureProvider;
using Microsoft.EMIC.Cloud.GenericWorker.SampleNotifications;
using Microsoft.EMIC.Cloud.Notification;
using Microsoft.EMIC.Cloud.Storage.Azure;
using Microsoft.EMIC.Cloud.UserAdministration;
using Microsoft.EMIC.Cloud.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using PluginAttr = Microsoft.EMIC.Cloud.Notification.SerializableKeyValuePair<string, string>;
using Microsoft.EMIC.Cloud.ApplicationRepository;

namespace Tests
{
    [TestClass]
    public class NotificationServiceTests
    {
        private const int testTimeOutInMilliSeconds = 3 * 60 * 1000;

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

            TestHelper.UploadMathApplication(out applicationIdentificationURI, out appReference, out descReference, out appDesc,userDataContainer);
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
            Thread.Sleep(6500);//To be on the save side
        }

        /// <summary>
        /// Test title: NotificationServiceTest
        /// Description:
        ///     This test case covers the subscription for notifications with different notification plugins and for various sets of statuses
        /// Author:
        ///     EMIC / emic_cce@microsoft.com
        /// Category:
        ///     Job Management, positive, local, end2end
        /// Running services/endpoints: 
        ///     running Job Management endpoint    
        /// Expected Results: 
        ///     The correct number of stored subscriptions for the various jobs
        ///     The correct number of queue messages produced by the queue notification plugin
        /// </summary>      
        //TODO: remove demo code
        //TODO: specifiy the numbers
        //TODO: write dedicated Subscribe, Unsubscribe Tests
        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds*3)]
        public void NotificationServiceTest()
        {
            var rt = new TwoRuntimes(genericWorkerContainer);
            var worker = rt.RT0;
            var submission = rt.RT1;
            string alice = "Alice";
            string bob = "Bob";
            string chris = "Chris";

            var alice1 = submission.SubmitJob(alice, Guid.NewGuid().ToString(), TestHelper.CreateJSDL);
            var alice2 = submission.SubmitJob(alice, Guid.NewGuid().ToString(), TestHelper.CreateJSDL);
            var bob1 = submission.SubmitJob(bob, Guid.NewGuid().ToString(), TestHelper.CreateJSDL);

            var chris1 = submission.SubmitJob(chris, Guid.NewGuid().ToString(), TestHelper.CreateJSDL);
            var chris2 = submission.SubmitJob(chris, Guid.NewGuid().ToString(), TestHelper.CreateJSDL);
            var chris3 = submission.SubmitJob(chris, Guid.NewGuid().ToString(), TestHelper.CreateJSDL);
            var chris4 = submission.SubmitJob(chris, Guid.NewGuid().ToString(), TestHelper.CreateJSDL);

            var notificationClient = NotificationServiceClient.CreateUnprotectedClient(CloudSettings.NotificationServiceURL);
            var jobManagementClient = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);

            var pluginConfigHTTP = new List<PluginAttr>();// using PluginAttr=KeyValuePair<string,string>

            pluginConfigHTTP.Add(new PluginAttr(PluginConfigMandatoryKeys.NAME, JobStatusNotificationPluginNames.HTTP_POST));
            pluginConfigHTTP.Add(new PluginAttr("url", "http://www.example.com/notify.php"));

            var queue_chris = "chrisnotifications" + Guid.NewGuid().ToString();

            var pluginConfigAzureQueue = new List<PluginAttr>();// using PluginAttr=KeyValuePair<string,string>

            pluginConfigAzureQueue.Add(new PluginAttr(PluginConfigMandatoryKeys.NAME, JobStatusNotificationPluginNames.AZURE_QUEUE));
            pluginConfigAzureQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.QUEUE_NAME, queue_chris));
            pluginConfigAzureQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.MESSAGE, "msg1: I like all statuses"));
            pluginConfigAzureQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.CONNECTION_STRING, CloudSettings.UserDataStoreConnectionString));

            var pluginConfigAzureQueue2 = new List<PluginAttr>();// using PluginAttr=KeyValuePair<string,string>

            pluginConfigAzureQueue2.Add(new PluginAttr(PluginConfigMandatoryKeys.NAME, JobStatusNotificationPluginNames.AZURE_QUEUE));
            pluginConfigAzureQueue2.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.QUEUE_NAME, queue_chris));
            pluginConfigAzureQueue2.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.MESSAGE, "msg2: I only like the running status"));
            pluginConfigAzureQueue2.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.CONNECTION_STRING, CloudSettings.UserDataStoreConnectionString));

            Assert.IsTrue(submission.GetSubscriptions(chris1).Count == 0, "weird, there are already subscription for this job");
            notificationClient.CreateSubscription(TestHelper.GetEpr(chris1), pluginConfigHTTP);
            Assert.IsTrue(submission.GetSubscriptions(chris1).Count == 1);
            notificationClient.CreateSubscription(TestHelper.GetEpr(chris1), pluginConfigAzureQueue);
            Assert.IsTrue(submission.GetSubscriptions(chris1).Count == 2);
            notificationClient.CreateSubscriptionForStatuses(TestHelper.GetEpr(chris1), new List<JobStatus>() { JobStatus.Running }, pluginConfigAzureQueue2);
            Assert.IsTrue(submission.GetSubscriptions(chris1).Count == 3);

            var queueClient = account.CreateCloudQueueClient();
            var queue = queueClient.GetQueueReference(queue_chris);
            var initMsgCount = 0;

            worker.MarkJobAsChekingInputData(chris1, "testing instance1");
            Assert.AreEqual(initMsgCount + 1, queue.PeekMessages(32).ToList().Count);
            worker.MarkJobAsRunning(chris1, "", "", "");
            Assert.AreEqual(initMsgCount + 3, queue.PeekMessages(32).ToList().Count);
            notificationClient.Unsubscribe(TestHelper.GetEpr(chris1));
            Assert.IsTrue(submission.GetSubscriptions(chris1).Count == 0, "weird, there are still subscriptions for this job");

            var bobJobs = jobManagementClient.GetJobs(bob);
            var aliceJobs = jobManagementClient.GetJobs(alice);
            var chrisJobs = jobManagementClient.GetJobs(chris);

            //subscribe to all status changes for bobs jobs
            bobJobs.ForEach(j => notificationClient.CreateSubscription(j, pluginConfigHTTP));
            //forget it that is way too much
            bobJobs.ForEach(j => notificationClient.Unsubscribe(j));

            //subscribe only to finished jobs for chris
            aliceJobs.ForEach(j => notificationClient.CreateSubscriptionForStatuses(j, new List<JobStatus>() { JobStatus.Finished }, pluginConfigHTTP));

            //subscribe only to terminated jobs for alice
            var termStatuses = new List<JobStatus>() { JobStatus.Finished, JobStatus.Failed };
            aliceJobs.ForEach(j => notificationClient.CreateSubscriptionForStatuses(j, termStatuses, pluginConfigHTTP));

            queue.Delete();
        }

        /// <summary>
        /// Test title: NotificationServiceSubscribeUnsubscribeTest
        /// Description:
        ///     the user submits a job and subscribes on a job and unsubscribes on the same job
        /// Author:
        ///     EMIC / emic_cce@microsoft.com
        /// Category:
        ///     Job Management, positive, local, end2end
        /// Running services/endpoints: 
        ///     running Job Management endpoint    
        /// Expected Results: 
        ///     no subscriptions for the job
        /// </summary>      
        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void NotificationServiceSubscribeUnsubscribeTest()
        {
            var rt = new TwoRuntimes(genericWorkerContainer);
            var worker = rt.RT0;
            var submission = rt.RT1;
            string alice = "Alice";

            var alice1 = submission.SubmitJob(alice, Guid.NewGuid().ToString(), TestHelper.CreateJSDL);
            var notificationClient = NotificationServiceClient.CreateUnprotectedClient(CloudSettings.NotificationServiceURL);

            var pluginConfigAzureQueue = new List<PluginAttr>();// using PluginAttr=KeyValuePair<string,string>

            pluginConfigAzureQueue.Add(new PluginAttr(PluginConfigMandatoryKeys.NAME, JobStatusNotificationPluginNames.AZURE_QUEUE));
            pluginConfigAzureQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.QUEUE_NAME, "test"));
            pluginConfigAzureQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.MESSAGE, "msg1: I like all statuses"));
            pluginConfigAzureQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.CONNECTION_STRING, CloudSettings.UserDataStoreConnectionString));

            notificationClient.CreateSubscriptionForStatuses(TestHelper.GetEpr(alice1), new List<JobStatus>() { JobStatus.Running }, pluginConfigAzureQueue);
            Assert.AreEqual(1, submission.GetSubscriptions(alice1).Count);
            notificationClient.Unsubscribe(TestHelper.GetEpr(alice1));
            Assert.AreEqual(0, submission.GetSubscriptions(alice1).Count, "weird, there are still subscriptions for this job");
        }



        /// <summary>
        /// Test title: NotificationServiceSubscribeWithoutPluginConfigTest
        /// Description:
        ///     the user submits a job and subscribes on the job without providing a plugin config
        /// Author:
        ///     EMIC / emic_cce@microsoft.com
        /// Category:
        ///     Job Management, negative, local, end2end
        /// Running services/endpoints: 
        ///     running Job Management endpoint    
        /// Expected Results: 
        ///     FaultException
        /// </summary>      
        [TestMethod]
        [ExpectedException(typeof(FaultException<OGF.BES.Faults.InvalidRequestMessageFaultType>))]
        [Timeout(testTimeOutInMilliSeconds)]
        public void NotificationServiceSubscribeWithoutPluginConfigTest()
        {
            var rt = new TwoRuntimes(genericWorkerContainer);
            var worker = rt.RT0;
            var submission = rt.RT1;
            var notificationClient = NotificationServiceClient.CreateUnprotectedClient(CloudSettings.NotificationServiceURL);
            var jobManagementClient = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);

            string chris = "Chris";
            var chris1 = submission.SubmitJob(chris, Guid.NewGuid().ToString(), TestHelper.CreateJSDL);

            var queue_chris = "chrisnotifications" + Guid.NewGuid().ToString();

            notificationClient.CreateSubscription(TestHelper.GetEpr(chris1), null);
        }

    }
}
