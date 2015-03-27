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
    public class GroupLevelNotificationTests
    {
        private const int testTimeOutInMilliSeconds = 3 * 90 * 1000;
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
            
            #endregion
            TestHelper.FlushTable(genericWorkerContainer);
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

        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void JobGroupNotificationOneFailedTest()
        {
            var rt = new TwoRuntimes(genericWorkerContainer);
            var worker = rt.RT0;
            var submission = rt.RT1;

            var group = "blastJobs";
            var jobPrefix = "Job";
            var owner = "anonymous";
            var numGroupMembers = 4;

            var jobs = new List<IJob>();
            for (var i = 0; i < numGroupMembers; i++)
            {
                var jobName = string.Format("{0}{1}", jobPrefix, i);
                var job = submission.SubmitJob(owner, jobName, TestHelper.CreateGroupJSDL(group, jobName));
                jobs.Add(job);
            }
            var jobManagementPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);

            var jobgroup = jobManagementPortal.GetJobsByGroup(owner, group);
            Assert.AreEqual(jobgroup.Count, numGroupMembers);

            var testqueue = "testnotifications" + Guid.NewGuid().ToString();

            var pluginConfigAzureQueue = new List<PluginAttr>();
            pluginConfigAzureQueue.Add(new PluginAttr(PluginConfigMandatoryKeys.NAME, JobStatusNotificationPluginNames.AZURE_QUEUE));
            pluginConfigAzureQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.QUEUE_NAME, testqueue));
            pluginConfigAzureQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.MESSAGE, "msg1: I like all statuses"));
            pluginConfigAzureQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.CONNECTION_STRING, CloudSettings.UserDataStoreConnectionString));

            var notificationClient = NotificationServiceClient.CreateUnprotectedClient(CloudSettings.NotificationServiceURL);
            notificationClient.CreateSubscriptionForGroupStatuses(group, new List<JobStatus> { JobStatus.Failed }, pluginConfigAzureQueue);

            var queueClient = account.CreateCloudQueueClient();
            var queue = queueClient.GetQueueReference(testqueue);
            queue.CreateIfNotExist();
            Assert.AreEqual(0, queue.PeekMessages(10).Count());
            Assert.IsTrue(TestHelper.AdvanceNewJobToStatus(worker, jobs.ElementAt(0), JobStatus.Failed));
            Func<bool> cond = () => queue.PeekMessages(10).Count() == 1;
            TestHelper.PollForCondition(cond, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(15));
            Assert.IsTrue(cond());
        }

        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        [ExpectedException(typeof(FaultException<OGF.BES.Faults.InvalidRequestMessageFaultType>))]
        public void JobGroupNotificationRepeatedStatusesTest()
        {
            var rt = new TwoRuntimes(genericWorkerContainer);
            var worker = rt.RT0;
            var submission = rt.RT1;

            var group = "blastJobs";
            var jobPrefix = "Job";
            var owner = "anonymous";
            var numGroupMembers = 4;

            var jobs = new List<IJob>();
            for (var i = 0; i < numGroupMembers; i++)
            {
                var jobName = string.Format("{0}{1}", jobPrefix, i);
                var job = submission.SubmitJob(owner, jobName, TestHelper.CreateGroupJSDL(group, jobName));
                jobs.Add(job);
            }
            var jobManagementPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);

            var jobgroup = jobManagementPortal.GetJobsByGroup(owner, group);
            Assert.AreEqual(jobgroup.Count, numGroupMembers);

            var testqueue = "testnotifications" + Guid.NewGuid().ToString();

            var pluginConfigAzureQueue = new List<PluginAttr>();
            pluginConfigAzureQueue.Add(new PluginAttr(PluginConfigMandatoryKeys.NAME, JobStatusNotificationPluginNames.AZURE_QUEUE));
            pluginConfigAzureQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.QUEUE_NAME, testqueue));
            pluginConfigAzureQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.MESSAGE, "msg1: I like all statuses"));
            pluginConfigAzureQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.CONNECTION_STRING, CloudSettings.UserDataStoreConnectionString));

            var notificationClient = NotificationServiceClient.CreateUnprotectedClient(CloudSettings.NotificationServiceURL);
            notificationClient.CreateSubscriptionForGroupStatuses(group, new List<JobStatus> { JobStatus.Failed, JobStatus.Finished, JobStatus.Finished }, pluginConfigAzureQueue);
        }

        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        [ExpectedException(typeof(FaultException<OGF.BES.Faults.InvalidRequestMessageFaultType>))]
        public void JobGroupNotificationNotSupportedStatusesTest()
        {
            var rt = new TwoRuntimes(genericWorkerContainer);
            var worker = rt.RT0;
            var submission = rt.RT1;

            var group = "blastJobs";
            var jobPrefix = "Job";
            var owner = "anonymous";
            var numGroupMembers = 4;

            var jobs = new List<IJob>();
            for (var i = 0; i < numGroupMembers; i++)
            {
                var jobName = string.Format("{0}{1}", jobPrefix, i);
                var job = submission.SubmitJob(owner, jobName, TestHelper.CreateGroupJSDL(group, jobName));
                jobs.Add(job);
            }
            var jobManagementPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);

            var jobgroup = jobManagementPortal.GetJobsByGroup(owner, group);
            Assert.AreEqual(jobgroup.Count, numGroupMembers);

            var testqueue = "testnotifications" + Guid.NewGuid().ToString();

            var pluginConfigAzureQueue = new List<PluginAttr>();
            pluginConfigAzureQueue.Add(new PluginAttr(PluginConfigMandatoryKeys.NAME, JobStatusNotificationPluginNames.AZURE_QUEUE));
            pluginConfigAzureQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.QUEUE_NAME, testqueue));
            pluginConfigAzureQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.MESSAGE, "msg1: I like all statuses"));
            pluginConfigAzureQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.CONNECTION_STRING, CloudSettings.UserDataStoreConnectionString));

            var notificationClient = NotificationServiceClient.CreateUnprotectedClient(CloudSettings.NotificationServiceURL);
            notificationClient.CreateSubscriptionForGroupStatuses(group, new List<JobStatus> { JobStatus.Failed, JobStatus.Finished, JobStatus.Running }, pluginConfigAzureQueue);
        }

        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void JobGroupNotificationTwoFailedTest()
        {
            var rt = new TwoRuntimes(genericWorkerContainer);
            var worker = rt.RT0;
            var submission = rt.RT1;

            var group = "blastJobs";
            var jobPrefix = "Job";
            var owner = "anonymous";
            var numGroupMembers = 4;

            var jobs = new List<IJob>();
            for (var i = 0; i < numGroupMembers; i++)
            {
                var jobName = string.Format("{0}{1}", jobPrefix, i);
                var job = submission.SubmitJob(owner, jobName, TestHelper.CreateGroupJSDL(group, jobName));
                jobs.Add(job);
            }
            var jobManagementPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);

            var jobgroup = jobManagementPortal.GetJobsByGroup(owner, group);
            Assert.AreEqual(jobgroup.Count, numGroupMembers);

            var testqueue = "testnotifications" + Guid.NewGuid().ToString();

            var pluginConfigAzureQueue = new List<PluginAttr>();
            pluginConfigAzureQueue.Add(new PluginAttr(PluginConfigMandatoryKeys.NAME, JobStatusNotificationPluginNames.AZURE_QUEUE));
            pluginConfigAzureQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.QUEUE_NAME, testqueue));
            pluginConfigAzureQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.MESSAGE, "msg1: I like all statuses"));
            pluginConfigAzureQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.CONNECTION_STRING, CloudSettings.UserDataStoreConnectionString));

            var notificationClient = NotificationServiceClient.CreateUnprotectedClient(CloudSettings.NotificationServiceURL);
            notificationClient.CreateSubscriptionForGroupStatuses(group, new List<JobStatus> { JobStatus.Failed, JobStatus.Finished }, pluginConfigAzureQueue);

            var queueClient = account.CreateCloudQueueClient();
            var queue = queueClient.GetQueueReference(testqueue);
            queue.CreateIfNotExist();
            Assert.AreEqual(0, queue.PeekMessages(10).Count());
            Assert.IsTrue(TestHelper.AdvanceNewJobToStatus(worker, jobs.ElementAt(0), JobStatus.Failed));
            Func<bool> cond = () => queue.PeekMessages(10).Count() == 1;
            Assert.IsTrue(TestHelper.AdvanceNewJobToStatus(worker, jobs.ElementAt(1), JobStatus.Failed));
            TestHelper.PollForCondition(cond, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(15));
            Assert.IsTrue(cond());
        }

        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void JobGroupNotificationAllFinishedTest()
        {
            var rt = new TwoRuntimes(genericWorkerContainer);
            var worker = rt.RT0;
            var submission = rt.RT1;

            var group = "blastJobs";
            var jobPrefix = "Job";
            var owner = "anonymous";
            var numGroupMembers = 4;

            var jobs = new List<IJob>();
            for (var i = 0; i < numGroupMembers; i++)
            {
                var jobName = string.Format("{0}{1}", jobPrefix, i);
                var job = submission.SubmitJob(owner, jobName, TestHelper.CreateGroupJSDL(group, jobName));
                jobs.Add(job);
            }
            var jobManagementPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);

            var jobgroup = jobManagementPortal.GetJobsByGroup(owner, group);
            Assert.AreEqual(jobgroup.Count, numGroupMembers);
            var testqueue = "testnotifications" + Guid.NewGuid().ToString();

            var pluginConfigAzureQueue = new List<PluginAttr>();
            pluginConfigAzureQueue.Add(new PluginAttr(PluginConfigMandatoryKeys.NAME, JobStatusNotificationPluginNames.AZURE_QUEUE));
            pluginConfigAzureQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.QUEUE_NAME, testqueue));
            pluginConfigAzureQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.MESSAGE, "msg1: I like all statuses"));
            pluginConfigAzureQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.CONNECTION_STRING, CloudSettings.UserDataStoreConnectionString));

            var notificationClient = NotificationServiceClient.CreateUnprotectedClient(CloudSettings.NotificationServiceURL);
            notificationClient.CreateSubscriptionForGroupStatuses(group, new List<JobStatus> { JobStatus.Finished }, pluginConfigAzureQueue);

            var queueClient = account.CreateCloudQueueClient();
            var queue = queueClient.GetQueueReference(testqueue);
            queue.CreateIfNotExist();
            Assert.AreEqual(0, queue.PeekMessages(10).Count());

            Assert.IsTrue(TestHelper.AdvanceNewJobToStatus(worker, jobs.ElementAt(1), JobStatus.Finished));
            Assert.AreEqual(0, queue.PeekMessages(10).Count());
            Assert.IsTrue(TestHelper.AdvanceNewJobToStatus(worker, jobs.ElementAt(0), JobStatus.Finished));
            Assert.AreEqual(0, queue.PeekMessages(10).Count());
            Assert.IsTrue(TestHelper.AdvanceNewJobToStatus(worker, jobs.ElementAt(2), JobStatus.Finished));
            Assert.AreEqual(0, queue.PeekMessages(10).Count());
            Assert.IsTrue(TestHelper.AdvanceNewJobToStatus(worker, jobs.ElementAt(3), JobStatus.Finished));
            Func<bool> cond = () => queue.PeekMessages(10).Count() == 1;
            TestHelper.PollForCondition(cond, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(15));
            Assert.IsTrue(cond());
        }
    }
}
