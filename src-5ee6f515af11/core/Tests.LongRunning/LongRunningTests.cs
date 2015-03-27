//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;
using Microsoft.EMIC.Cloud.Administrator.Host;
using Microsoft.EMIC.Cloud.ApplicationRepository;
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
using System.Linq;

namespace Tests.LongRunning
{
    [TestClass]
    public class LongRunningTests
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

        [TestMethod]
        public void HugeJobGroup_DeleteTerminatedGroupTest()
        {
            var rt = new TwoRuntimes(genericWorkerContainer);
            var worker = rt.RT0;
            var submission = rt.RT1;
            var numGroupMembers = 101; //a batch operation can operate on 100 entities

            var group = "blastJobs";
            var jobPrefix = "Job";
            var user = "anonymous";

            for (var i = 0; i < numGroupMembers; i++)
            {
                var jobName = string.Format("{0}{1}", jobPrefix, i);
                var job = submission.SubmitJob(user, jobName, TestHelper.CreateGroupJSDL(group, jobName));
                Thread.Sleep(10);
                TestHelper.AdvanceNewJobToStatus(worker, job, JobStatus.Failed);
            }
            submission.DeleteTerminatedJobs(user);
            var jobManagementPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);
            Assert.AreEqual(0, jobManagementPortal.GetNumberOfJobs(user, new List<JobStatus>() { JobStatus.Failed }));
        }

        [TestMethod]
        public void HugeJobList_DeleteTerminatedJobsTest()
        {
            var rt = new TwoRuntimes(genericWorkerContainer);
            var worker = rt.RT0;
            var submission = rt.RT1;
            var jobcount = 101;
            var user = "Alice";
            Enumerable.Range(0, jobcount).ToList().ForEach(i => { var job = submission.SubmitJob(user, Guid.NewGuid().ToString(), TestHelper.CreateJSDL); Thread.Sleep(TimeSpan.FromMilliseconds(10)); TestHelper.AdvanceNewJobToStatus(worker, job, JobStatus.Failed); });
            submission.DeleteTerminatedJobs(user);
            var jobManagementPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);
            Assert.AreEqual(0, jobManagementPortal.GetNumberOfJobs(user, new List<JobStatus>() { JobStatus.Failed }));
        }

        [TestMethod]
        [Timeout(TestTimeout.Infinite)]
        public void HugeJobHierarchy_DeleteTerminatedHierarchyTest()
        {
            var rt = new TwoRuntimes(genericWorkerContainer);
            var worker1 = rt.RT0;
            var submission = rt.RT1;
            var owner = "John Doe";

            var jobcount = 101; //a batch operation can operate on 100 entities
            string rootJobId = "id-internalJobID-" + Guid.NewGuid().ToString();
            IJob rootJob = null;

            rootJob = submission.SubmitJob(owner, rootJobId, TestHelper.CreateHierarchicalJSDL("jobid://Root"));
            var rootJobEpr = TestHelper.GetEpr(rootJob);
            Assert.IsNotNull(rootJob);

            rootJob = TestHelper.GetRoot(worker1, rootJobId, rootJob);

            for (int i = 1; i < jobcount; i++)
            {
                var customerJobID = string.Format("jobid://Root/{0}", i);
                var job = worker1.SubmitJob(owner, Guid.NewGuid().ToString(), TestHelper.CreateHierarchicalJSDL(customerJobID), true);
                Thread.Sleep(1);
                TestHelper.AdvanceNewJobToStatus(worker1, job, JobStatus.Failed);
            }
            TestHelper.AdvanceNewJobToStatus(worker1, rootJob, JobStatus.Failed);
            submission.DeleteTerminatedJobs(owner);
            var jobManagementPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);
            Assert.AreEqual(0, jobManagementPortal.GetNumberOfJobs(owner, new List<JobStatus>() { JobStatus.Failed }));
        }
    }
}
