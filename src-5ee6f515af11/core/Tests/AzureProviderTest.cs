//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using Microsoft.EMIC.Cloud.Demo;
using Microsoft.EMIC.Cloud.GenericWorker.AzureProvider;
using Microsoft.EMIC.Cloud.GenericWorker;
using Microsoft.EMIC.Cloud.DataManagement;
using Microsoft.EMIC.Cloud.Storage.Azure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.EMIC.Cloud.ApplicationRepository;
using PluginAttr = Microsoft.EMIC.Cloud.Notification.SerializableKeyValuePair<string, string>;
using OGF.BES;
using System.Xml;
using Microsoft.EMIC.Cloud.Notification;
using Microsoft.EMIC.Cloud.GenericWorker.SampleNotifications;
using System.ServiceModel;
using Microsoft.EMIC.Cloud;
using System.Text;

namespace Tests
{
    [TestClass]
    public class AzureProviderTest
    {
        static CompositionContainer container;
        private const int testTimeOutInMilliSeconds = 3 * 50 * 1000;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            container = new CompositionContainer(
                 new AggregateCatalog(
                     new TypeCatalog(typeof(CloudSettings)),
                     new AssemblyCatalog(typeof(LiteralArgument).Assembly),
                     new AssemblyCatalog(typeof(AzureGWRuntimeEnvironment).Assembly),
                     new AssemblyCatalog(typeof(AzureArgumentCatalogueReference).Assembly),
                     new AssemblyCatalog(typeof(AzureQueueJobStatusNotificationPlugin).Assembly)
                ));            
        }

        [TestInitialize]
        public void TestInitialize()
        {
            TestHelper.FlushTable(container);
            TestHelper.FlushBlobContainer(container);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestHelper.FlushTable(container);
            TestHelper.FlushBlobContainer(container);
        }

        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void GivenTwoWorkers_WhenOneJobIsSubmitted_ThenOnlyOneWorkerCanClaimTheJob()
        {
            #region Given two workers

            var rt = new TwoRuntimes(container);
            var submission = rt.RT0;
            var worker1 = rt.RT1;
            var worker2 = rt.RT2;

            Assert.IsNotNull(submission);
            Assert.IsNotNull(worker1);
            Assert.IsNotNull(worker2);
            Assert.AreNotSame(worker1, worker2);
            Assert.AreNotSame(worker1, submission);
            Assert.AreNotSame(submission, worker2);

            #endregion

            #region When One Job Is Submitted

            string id = "id-internalJobID-" + Guid.NewGuid().ToString();
            var iJob = submission.SubmitJob("John Doe", id, TestHelper.CreateJSDL);
            Assert.IsNotNull(iJob);

            Action<JobStatus> AssertStatus = TestHelper.CreateAssertStatus(id, container);

            AssertStatus(JobStatus.Submitted);

            #endregion

            #region Then Only One Worker Can Claim The Job

            //var j1 = worker1.DequeueJob();
            IJob j1;
            Assert.IsTrue(worker1.TryDequeueJob(out j1), "job could not be dequeued");

            AssertStatus(JobStatus.Submitted);
            IJob j2;
            Assert.IsTrue(worker2.TryDequeueJob(out j2), "job could not be dequeued");

            AssertStatus(JobStatus.Submitted);

            Assert.AreEqual<string>(id, j1.InternalJobID);
            Assert.AreEqual<string>(id, j2.InternalJobID);

            Assert.IsTrue(worker2.MarkJobAsChekingInputData(j2, "worker2"),
                "1st worker could not claim the job");
            AssertStatus(JobStatus.CheckingInputData);

            Assert.IsFalse(worker1.MarkJobAsChekingInputData(j1, "worker1"),
                "2nd worker could claim the job?!?");
            AssertStatus(JobStatus.CheckingInputData);

            Assert.IsTrue(worker2.MarkJobAsRunning(j2, "Running now", "stdout", "stderr"));
            AssertStatus(JobStatus.Running);

            #endregion
        }

        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void GivenAWorker_WhenOneJobIsSubmitted_ThenItCanBeStopped()
        {
            TwoRuntimes rt = null;
            IGWRuntimeEnvironment submission = null;
            IGWRuntimeEnvironment worker1 = null;

            #region Given a worker
            using ("new TwoRuntimes".Stop())
            {
                rt = new TwoRuntimes(container);
                submission = rt.RT0;
                worker1 = rt.RT1;

                Assert.IsNotNull(submission);
                Assert.IsNotNull(worker1);
                Assert.AreNotSame(worker1, submission);
            }

            #endregion

            #region When One Job Is Submitted

            string id = "id-internalJobID-" + Guid.NewGuid().ToString();
            Action<JobStatus> AssertStatus = TestHelper.CreateAssertStatus(id, container);
            IJob iJob = null;

            using ("SubmitJob".Stop())
            {
                iJob = submission.SubmitJob("John Doe", id, TestHelper.CreateJSDL);
                Assert.IsNotNull(iJob);

                AssertStatus(JobStatus.Submitted);
            }

            #endregion

            #region Then it can be stopped

            IJob j1 = null;

            using ("Dequeue".Stop())
            {
                Assert.IsTrue(worker1.TryDequeueJob(out j1));

                Assert.AreEqual<string>(id, j1.InternalJobID);
                AssertStatus(JobStatus.Submitted);
            }

            using ("C1".Stop())
            {
                Assert.IsTrue(worker1.MarkJobAsChekingInputData(j1, "GivenAWorker_WhenOneJobIsSubmitted_ThenItCanBeStopped"));
                AssertStatus(JobStatus.CheckingInputData);
            }

            using ("Running".Stop())
            {
                Assert.IsTrue(worker1.MarkJobAsRunning(j1, "w1 running", "stdout", "stderr"));
                AssertStatus(JobStatus.Running);
            }

            using ("Running2".Stop())
            {
                Assert.IsTrue(worker1.MarkJobAsRunning(j1, "w1 still running", "stdout", "stderr"));
                AssertStatus(JobStatus.Running);
            }

            using ("GetJobByID".Stop())
            {
                var j = submission.GetJobByID(id);
                Assert.IsTrue(submission.MarkJobAsCancellationPending(j));
                AssertStatus(JobStatus.CancelRequested);
            }

            using ("Running not possible".Stop())
            {
                Assert.IsFalse(worker1.MarkJobAsRunning(j1, "w1 running", "stdout", "stderr"));
                AssertStatus(JobStatus.CancelRequested);
            }

            #endregion
        }

        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void AJobRegularStateCycle()
        {
            // Submitted Running Running Running Running Finished

            #region Given one worker

            var rt = new TwoRuntimes(container);
            var submission = rt.RT0;
            var worker1 = rt.RT1;

            Assert.IsNotNull(submission);
            Assert.IsNotNull(worker1);
            Assert.AreNotSame(worker1, submission);

            #endregion

            #region When One Job Is Submitted

            string id = "id-internalJobID-" + Guid.NewGuid().ToString();
            var iJob = submission.SubmitJob("John Doe", id, TestHelper.CreateJSDL);
            Assert.IsNotNull(iJob);

            Action<JobStatus> AssertStatus = TestHelper.CreateAssertStatus(id, container);

            AssertStatus(JobStatus.Submitted);

            #endregion

            #region Then Only One Worker Can Claim The Job
            IJob j1;
            Assert.IsTrue(worker1.TryDequeueJob(out j1), "job could not be dequeued");
            Assert.AreEqual<string>(id, j1.InternalJobID);
            AssertStatus(JobStatus.Submitted);

            Assert.IsTrue(worker1.MarkJobAsChekingInputData(j1, "worker1"));
            AssertStatus(JobStatus.CheckingInputData);

            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(worker1.MarkJobAsRunning(j1, "w1 running " + i.ToString(), "stdout", "stderr"));
                AssertStatus(JobStatus.Running);
            }

            Assert.IsTrue(worker1.MarkJobAsFinished(j1, "worker 1 finished"));
            AssertStatus(JobStatus.Finished);

            #endregion
        }

        [TestMethod]
        [Timeout(60 * 1000)]
        public void AJobFailingStateCycle()
        {
            // Submitted Running Running Running Running Failed

            #region Given one worker

            var rt = new TwoRuntimes(container);
            var submission = rt.RT0;
            var worker1 = rt.RT1;

            Assert.IsNotNull(submission);
            Assert.IsNotNull(worker1);
            Assert.AreNotSame(worker1, submission);

            #endregion

            #region When One Job Is Submitted

            string id = "id-internalJobID-" + Guid.NewGuid().ToString();
            var iJob = submission.SubmitJob("John Doe", id, TestHelper.CreateJSDL);
            Assert.IsNotNull(iJob);

            Action<JobStatus> AssertStatus = TestHelper.CreateAssertStatus(id, container);

            AssertStatus(JobStatus.Submitted);

            #endregion

            #region Then Only One Worker Can Claim The Job

            IJob j1;
            Assert.IsTrue(worker1.TryDequeueJob(out j1));

            Assert.AreEqual<string>(id, j1.InternalJobID);
            AssertStatus(JobStatus.Submitted);

            Assert.IsTrue(worker1.MarkJobAsChekingInputData(j1, "worker1.MarkJobAsOwnershipClaimed"),
                "worker1.MarkJobAsOwnershipClaimed");
            AssertStatus(JobStatus.CheckingInputData);

            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(worker1.MarkJobAsRunning(j1, "w1 running " + i.ToString(), "stdout", "stderr"));
                AssertStatus(JobStatus.Running);
            }

            Assert.IsTrue(worker1.MarkJobAsFailed(j1, "worker1.MarkJobAsFailed"));
            AssertStatus(JobStatus.Failed);

            #endregion
        }

        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void AJobCancellingStateCycle()
        {
            // Submitted Running Running Running Running CancelRequested Stopped

            #region Given one worker

            var rt = new TwoRuntimes(container);
            var submission = rt.RT0;
            var worker = rt.RT1;
            var inspector = rt.RT2;

            Assert.IsNotNull(submission);
            Assert.IsNotNull(worker);
            Assert.AreNotSame(worker, submission);

            #endregion

            #region When One Job Is Submitted

            string owner = "John Doe";
            string id = "id-internalJobID-" + Guid.NewGuid().ToString();
            var submissionJob = submission.SubmitJob(owner, id, TestHelper.CreateJSDL);
            Assert.IsNotNull(submissionJob);

            Action<JobStatus> AssertStatus = (status) =>
            {
                var j = inspector.GetJobStatus(id);
                Assert.IsNotNull(j, "j");
                Assert.AreEqual<JobStatus>(status, j.Value);
                Console.WriteLine(status);
            };

            AssertStatus(JobStatus.Submitted);

            #endregion

            #region Then Only One Worker Can Claim The Job

            IJob workerJob;
            Assert.IsTrue(worker.TryDequeueJob(out workerJob));
            Assert.AreEqual<string>(id, workerJob.InternalJobID);
            AssertStatus(JobStatus.Submitted);

            Assert.IsTrue(worker.MarkJobAsChekingInputData(workerJob, "worker1"));
            AssertStatus(JobStatus.CheckingInputData);

            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(worker.MarkJobAsRunning(workerJob, "w1 running " + i.ToString(), "stdout", "stderr"));
                AssertStatus(JobStatus.Running);
            }

            submissionJob = submission.GetJobByID(id);
            Assert.IsTrue(submission.MarkJobAsCancellationPending(submissionJob), "cancelRequestedSuccess");
            AssertStatus(JobStatus.CancelRequested);

            workerJob = worker.GetJobByID(id);
            Assert.AreEqual<JobStatus>(JobStatus.CancelRequested, workerJob.Status);

            Assert.IsTrue(worker.MarkJobAsCancelled(workerJob, "worker cancelled"), "cancelSuccess");
            AssertStatus(JobStatus.Cancelled);

            #endregion
        }

        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void AJobCancellingBeforeStartingStateCycle()
        {
            // Submitted CancelRequested Stopped

            #region Given one worker

            var rt = new TwoRuntimes(container);
            var submission = rt.RT0;
            var worker = rt.RT1;
            var inspector = rt.RT2;

            Assert.IsNotNull(submission);
            Assert.IsNotNull(worker);
            Assert.AreNotSame(worker, submission);

            #endregion

            #region When One Job Is Submitted

            string owner = "John Doe";
            string id = "id-internalJobID-AJobCancellingBeforeStartingStateCycle-" + Guid.NewGuid().ToString();
            var submissionJob = submission.SubmitJob(owner, id, TestHelper.CreateJSDL);
            Assert.IsNotNull(submissionJob);

            Action<JobStatus> AssertStatus = TestHelper.CreateAssertStatus(id, container);

            AssertStatus(JobStatus.Submitted);

            #endregion

            #region Then Only One Worker Can Claim The Job

            IJob workerJob;
            Assert.IsTrue(worker.TryDequeueJob(out workerJob));

            Assert.AreEqual<string>(id, workerJob.InternalJobID);
            AssertStatus(JobStatus.Submitted);

            // If the job was submitted when we cancel it, there will be no worker feeling responsible for 
            // transitioning into the Cancelled state, so we need to do it ourselves. 
            //
            // So there is no CancelRequestedState here...
            //
            Assert.IsTrue(submission.MarkJobAsCancelled(submissionJob, "job canceled"));
            AssertStatus(JobStatus.Cancelled);

            #endregion
        }
        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void AllConfigSettingsNotNullOrEmptyTest()
        {
            var settings = typeof(CloudSettings).GetProperties().Where(p => p.PropertyType.Name == typeof(string).Name);
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

            Assert.AreEqual(0, stringsettingswithoutvalue.Count(),"There are missing settings - check your registry or app.config");
        }

        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void ChildManagementTest()
        {
            var rt = new TwoRuntimes(container);
            var worker = rt.RT0;
            var submission = rt.RT1;
            string alice = "Alice";

            var aliceIJob1 = submission.SubmitJob(alice, Guid.NewGuid().ToString(), TestHelper.CreateJSDL);
            var aliceIJob2 = submission.SubmitJob(alice, Guid.NewGuid().ToString(), TestHelper.CreateJSDL);
            var aliceIJob3 = submission.SubmitJob(alice, Guid.NewGuid().ToString(), TestHelper.CreateJSDL);
            var azJob = aliceIJob1 as AzureJob;
            Assert.IsNotNull(azJob);
            Assert.IsFalse(azJob.HasChildren());
            azJob.AddChild(aliceIJob2.InternalJobID);
            Assert.IsTrue(azJob.HasChildren());
            Assert.AreEqual(1, azJob.GetChildren().Count);
            azJob.AddChild(aliceIJob3.InternalJobID);
            Assert.IsTrue(azJob.HasChildren());
            Assert.AreEqual(2, azJob.GetChildren().Count);
            azJob.RemoveChild(aliceIJob2.InternalJobID);
            Assert.AreEqual(1, azJob.GetChildren().Count);
            Assert.IsTrue(azJob.HasChildren());
            azJob.RemoveChild(aliceIJob3.InternalJobID);
            Assert.AreEqual(0, azJob.GetChildren().Count);
            Assert.IsFalse(azJob.HasChildren());
        }

        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void RemoveChildrenTest()
        {
            var rt = new TwoRuntimes(container);
            var worker = rt.RT0;
            var submission = rt.RT1;
            string alice = "Alice";

            var aliceIJob1 = submission.SubmitJob(alice, Guid.NewGuid().ToString(), TestHelper.CreateJSDL);
            var aliceIJob2 = submission.SubmitJob(alice, Guid.NewGuid().ToString(), TestHelper.CreateJSDL);
            var aliceIJob3 = submission.SubmitJob(alice, Guid.NewGuid().ToString(), TestHelper.CreateJSDL);

            var azJob = aliceIJob1 as AzureJob;
            Assert.IsNotNull(azJob);
            Assert.IsFalse(azJob.HasChildren());
            azJob.AddChild(aliceIJob2.InternalJobID);
            Assert.IsTrue(azJob.HasChildren());
            Assert.AreEqual(1, azJob.GetChildren().Count);
            azJob.AddChild(aliceIJob3.InternalJobID);
            Assert.IsTrue(azJob.HasChildren());
            Assert.AreEqual(2, azJob.GetChildren().Count);

            azJob.RemoveChildren();
            Assert.IsFalse(azJob.HasChildren());

        }

        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void OutputAppendTest()
        {
            var rt = new TwoRuntimes(container);
            var worker = rt.RT0;
            var submission = rt.RT1;
            string alice = "Alice";

            var aliceIJob1 = submission.SubmitJob(alice, Guid.NewGuid().ToString(), TestHelper.CreateJSDL);
            var output = new StringBuilder();
            output.Append("text1");
            submission.UpdateJobOutputs(aliceIJob1, output, null, null);
            output.Append("text2");
            submission.UpdateJobOutputs(aliceIJob1, output, null, null);
            output.Append("text3");
            submission.UpdateJobOutputs(aliceIJob1, output, null, null);
            var azJob = submission.GetJobByID(aliceIJob1.InternalJobID) as AzureJob;
            Assert.IsNotNull(azJob);
            Assert.AreEqual("text1text2text3", azJob.Stdout);
        }

        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void NotificationManagerTest() 
        {
            var rt = new TwoRuntimes(container);
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


            var pluginConfigHTTP = new List<PluginAttr>();// using PluginAttr=KeyValuePair<string,string>

            pluginConfigHTTP.Add(new PluginAttr(PluginConfigMandatoryKeys.NAME, JobStatusNotificationPluginNames.HTTP_POST));
            pluginConfigHTTP.Add(new PluginAttr("url", "http://www.example.com/notify.php"));

            var queue_chris = "chrisnotifications"+Guid.NewGuid().ToString();

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

            var statuses = new List<JobStatus>(){JobStatus.CheckingInputData,JobStatus.Submitted,JobStatus.Running,JobStatus.Finished,JobStatus.Failed};
            Assert.IsTrue(submission.GetSubscriptions(chris1).Count == 0, "weird, there are already subscription for this job");
            submission.Subscribe(chris1,new Subscription(){PluginConfig=pluginConfigHTTP, Statuses=statuses});

            Assert.IsTrue(submission.GetSubscriptions(chris1).Count == 1);
            submission.Subscribe(chris1, new Subscription() { PluginConfig = pluginConfigAzureQueue, Statuses = statuses });
            Assert.IsTrue(submission.GetSubscriptions(chris1).Count == 2);
            submission.Subscribe(chris1, new Subscription() { PluginConfig = pluginConfigAzureQueue2, Statuses = new List<JobStatus>() { JobStatus.Running } });
            Assert.IsTrue(submission.GetSubscriptions(chris1).Count == 3);

            var account = CloudStorageAccount.Parse(CloudSettings.UserDataStoreConnectionString);
            var queueClient = account.CreateCloudQueueClient();
            var queue = queueClient.GetQueueReference(queue_chris);
            var initMsgCount = 0;

            worker.MarkJobAsChekingInputData(chris1, "testing instance1");
            var currMsgCount = queue.PeekMessages(32).ToList().Count;
            Assert.IsTrue(currMsgCount == initMsgCount + 1,"currMsgCount:"+currMsgCount+" expected: "+(initMsgCount+1));
            worker.MarkJobAsRunning(chris1, "", "", "");
            currMsgCount = queue.PeekMessages(32).ToList().Count;
            Assert.IsTrue(currMsgCount == initMsgCount + 3, "currMsgCount:" + currMsgCount + " expected: " + (initMsgCount + 3));
            worker.MarkJobAsFinished(chris1, "");
            currMsgCount = queue.PeekMessages(32).ToList().Count;
            Assert.IsTrue(currMsgCount == initMsgCount + 4, "currMsgCount:" + currMsgCount + " expected: " + (initMsgCount + 4));
            queue.Delete();            
        }

        /// <summary>
        /// This test is intended to verify that unsubscribing notification works with enabled subscription cache.
        /// </summary>
        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void NotificationUnsubscribeTest()
        {
            var rt = new TwoRuntimes(container);
            var worker = rt.RT0;

            var queue_chris = "chrisnotifications" + Guid.NewGuid().ToString();

            var pluginConfigAzureQueue = new List<PluginAttr>();// using PluginAttr=KeyValuePair<string,string>

            pluginConfigAzureQueue.Add(new PluginAttr(PluginConfigMandatoryKeys.NAME, JobStatusNotificationPluginNames.AZURE_QUEUE));
            pluginConfigAzureQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.QUEUE_NAME, queue_chris));
            pluginConfigAzureQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.MESSAGE, "msg1: I like all statuses"));
            pluginConfigAzureQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.CONNECTION_STRING, CloudSettings.UserDataStoreConnectionString));

            string chris = "Chris";

            var chris1 = worker.SubmitJob(chris, Guid.NewGuid().ToString(), TestHelper.CreateJSDL);
            var statuses = new List<JobStatus>() { JobStatus.CheckingInputData, JobStatus.Submitted, JobStatus.Running, JobStatus.Finished, JobStatus.Failed };
            Assert.IsTrue(worker.GetSubscriptions(chris1).Count == 0, "weird, there are already subscription for this job");
            worker.Subscribe(chris1, new Subscription() { PluginConfig = pluginConfigAzureQueue, Statuses = statuses });
            Assert.IsTrue(worker.GetSubscriptions(chris1).Count == 1);
            var account = CloudStorageAccount.Parse(CloudSettings.UserDataStoreConnectionString);
            var queueClient = account.CreateCloudQueueClient();
            var queue = queueClient.GetQueueReference(queue_chris);
            var initMsgCount = 0;

            worker.MarkJobAsChekingInputData(chris1, "testing instance1");
            Assert.IsTrue(queue.PeekMessages(32).ToList().Count == initMsgCount + 1);
            worker.MarkJobAsRunning(chris1, "", "", "");
            Assert.IsTrue(queue.PeekMessages(32).ToList().Count == initMsgCount + 2);
            worker.UnSubscribe(chris1);
            Assert.IsTrue(worker.GetSubscriptions(chris1).Count == 0, "weird, there are still subscriptions for this job");

            worker.MarkJobAsFinished(chris1, "");
            Assert.IsTrue(queue.PeekMessages(32).ToList().Count == initMsgCount + 2,"Unsubscribe did not work");
            queue.Delete();     
        }

        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void DataDrivenTest()
        {
            var rt = new TwoRuntimes(container);
            var submission = rt.RT0;

            var account = CloudStorageAccount.Parse(CloudSettings.GenericWorkerCloudConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference("testcontainer");
            blobContainer.CreateIfNotExist();
            var InputJob2 = blobContainer.GetBlobReference("testblob");
            InputJob2.UploadText("testcontent");

            var InputJob1 = blobContainer.GetBlobReference("NonExistingBlob");
            InputJob1.DeleteIfExists();

            VENUSApplicationDescription applicationDescription = new VENUSApplicationDescription()
            {
                CommandTemplate = new VENUSCommandTemplate()
                {
                    Args = new List<CommandLineArgument>() 
                    {
                        new CommandLineArgument(){
                            Name = "InputFile",
                            FormatString = "{0}",
                            Required = Required.Mandatory,
                            CommandLineArgType = CommandLineArgType.SingleReferenceInputArgument
                        }
                    }
                }
            };

            VENUSJobDescription completejob = new VENUSJobDescription
            {
                JobArgs = new ArgumentCollection() 
                    { 
                        new AzureArgumentSingleReference
                        {
                            Name = "InputFile",
                            DataAddress = InputJob2.Uri.AbsoluteUri,
                            ConnectionString = CloudSettings.UserDataStoreConnectionString
                        }
                    }
            };

            VENUSJobDescription uncompletejob = new VENUSJobDescription
            {
                JobArgs = new ArgumentCollection() 
                    { 
                        new AzureArgumentSingleReference
                        {
                            Name = "InputFile",
                            DataAddress = InputJob1.Uri.AbsoluteUri,
                            ConnectionString = CloudSettings.UserDataStoreConnectionString
                        }
                    }
            };
            Func<IJob, bool> isProcessable = (j) => (j.Status == JobStatus.Submitted && j.Ready(applicationDescription)); //we need to use the same func here and in GWDriver
            var job1 = submission.SubmitJob("John Doe", Guid.NewGuid().ToString(), uncompletejob);
            var job2 = submission.SubmitJob("John Doe", Guid.NewGuid().ToString(), completejob);

            Assert.IsFalse(isProcessable(job1));
            Assert.IsTrue(isProcessable(job2));

            InputJob1.UploadText("content created by job2");
            Assert.IsTrue(isProcessable(job1));
        }

        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void TestJobPrios()
        {
            var rt = new TwoRuntimes(container);
            var submission = rt.RT0;

            var account = CloudStorageAccount.Parse(CloudSettings.GenericWorkerCloudConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference("testcontainer");
            blobContainer.CreateIfNotExist();

            var ExistingBlob = blobContainer.GetBlobReference("ExistingBlob");
            ExistingBlob.UploadText("testcontent");

            var NonExistingBlob = blobContainer.GetBlobReference("NonExistingBlob");
            NonExistingBlob.DeleteIfExists();

            VENUSApplicationDescription applicationDescription = new VENUSApplicationDescription()
            {
                CommandTemplate = new VENUSCommandTemplate()
                {
                    Args = new List<CommandLineArgument>() 
                    {
                        new CommandLineArgument(){
                            Name = "InputFile",
                            FormatString = "{0}",
                            Required = Required.Mandatory,
                            CommandLineArgType = CommandLineArgType.SingleReferenceInputArgument
                        }
                    }
                }
            };

            Func<bool,JobPriority,VENUSJobDescription> GetJobDesc = (isComplete,jobPriority) => 
            {            
                var jobDesc = new VENUSJobDescription()
                {
                    CustomerJobID = "some job",
                    JobArgs = new ArgumentCollection() 
                };
                var availableInputFile = new AzureArgumentSingleReference
                {
                    Name = "InputFile",
                    DataAddress = ExistingBlob.Uri.AbsoluteUri,
                    ConnectionString = CloudSettings.UserDataStoreConnectionString
                };
                var unavailableInputFile = new AzureArgumentSingleReference
                {
                    Name = "InputFile",
                    DataAddress = NonExistingBlob.Uri.AbsoluteUri,
                    ConnectionString = CloudSettings.UserDataStoreConnectionString
                };

                if (isComplete)
                {
                    jobDesc.JobArgs.Add(availableInputFile);
                }
                else
                {
                    jobDesc.JobArgs.Add(unavailableInputFile);                
                }
                jobDesc.JobPrio = jobPriority;
                return jobDesc;
            };

            var completeHighPrio = GetJobDesc(true, JobPriority.High);
            var completeStdPrio = GetJobDesc(true, JobPriority.Default);
            var incompleteHighPrio = GetJobDesc(false, JobPriority.High);
            var incompleteStdPrio = GetJobDesc(false, JobPriority.Default);


            Func<IJob, bool> isProcessable = (j) => (j.Status == JobStatus.Submitted && j.Ready(applicationDescription)); //we need to use the same func here and in GWDriver
            var job1 = submission.SubmitJob("John Doe", Guid.NewGuid().ToString(), incompleteStdPrio);
            var job2 = submission.SubmitJob("John Doe", Guid.NewGuid().ToString(), completeStdPrio);
            var job3 = submission.SubmitJob("John Doe", Guid.NewGuid().ToString(), completeStdPrio);
            var job4 = submission.SubmitJob("John Doe", Guid.NewGuid().ToString(), completeStdPrio);
            var job5 = submission.SubmitJob("John Doe", Guid.NewGuid().ToString(), completeStdPrio);
            var job6 = submission.SubmitJob("John Doe", Guid.NewGuid().ToString(), completeHighPrio);
            var job7 = submission.SubmitJob("John Doe", Guid.NewGuid().ToString(), completeHighPrio);
            var job8 = submission.SubmitJob("John Doe", Guid.NewGuid().ToString(), incompleteHighPrio);


            Assert.IsFalse(isProcessable(job1));
            Assert.IsTrue(isProcessable(job2));

            NonExistingBlob.UploadText("content created by job2");
            Assert.IsTrue(isProcessable(job1));
        }

        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void RefArray_JobReadyTest()
        {
            var rt = new TwoRuntimes(container);
            var submission = rt.RT0;

            var account = CloudStorageAccount.Parse(CloudSettings.GenericWorkerCloudConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference("testcontainer");
            blobContainer.CreateIfNotExist();
            ReferenceArray refArr = new ReferenceArray
            {
                Name = "inputfiles",
                References = new ReferenceCollection()
            };

            for (int i = 0; i < 3; i++)
            {
                var name = string.Format("inp{0}.txt", i);
                var blob = blobContainer.GetBlobReference(name);
                blob.UploadText("testContent" + i);
                var azBlobRef = new AzureBlobReference(blob, CloudSettings.GenericWorkerCloudConnectionString);
                refArr.References.Add(new Reference(name, azBlobRef));
            }

            VENUSApplicationDescription applicationDescription = new VENUSApplicationDescription()
            {
                CommandTemplate = new VENUSCommandTemplate()
                {
                    Args = new List<CommandLineArgument>() 
                    {
                        new CommandLineArgument(){
                            Name = "inputfiles",
                            FormatString = "{0}",
                            Required = Required.Mandatory,
                            CommandLineArgType = CommandLineArgType.MultipleReferenceInputArgument
                        }
                    }
                }
            };
            var jobDesc = new VENUSJobDescription()
            {
                JobArgs = new ArgumentCollection(){
                    refArr
                }
            };
            Assert.IsTrue(refArr.IsInput(applicationDescription));
            Assert.IsTrue(refArr.ExistsDataItem());

            var job = submission.SubmitJob("Me", "some internal id", jobDesc);
            Assert.IsTrue(job.Ready(applicationDescription));
            for (int i = 0; i < 3; i++)
            {
                var name = string.Format("inp{0}.txt", i);
                var blob = blobContainer.GetBlobReference(name);
                blob.DeleteIfExists();
            }
            Assert.IsFalse(refArr.ExistsDataItem());
            Assert.IsFalse(job.Ready(applicationDescription));
        }
   
        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void AddFirstJobOfAGroup()
        {
            var rt = new TwoRuntimes(container);
            var submission = rt.RT0;
            var worker1 = rt.RT1;

            var group="blastJobs";
            var job1="blast1";
            var job2 = "blast2";
            string internalJobId1 = "id-internalJobID1";
            string internalJobId2 = "id-internalJobID2";
            string groupName, jobName;

            var jsdlJob1 = TestHelper.CreateGroupJSDL(group,job1);
            var jsdlJob2 = TestHelper.CreateGroupJSDL(group,job2);

            Assert.AreEqual<string>(string.Format("GroupID://{0}/{1}",group,job1),jsdlJob1.CustomerJobID);

            Assert.IsTrue(JobID.TryParseGroup(jsdlJob1.CustomerJobID, out groupName, out jobName));
            Assert.AreEqual<string>(groupName, group);
            Assert.AreEqual<string>(jobName, job1);
            var groupjob1 = submission.SubmitJob("John Doe", internalJobId1, jsdlJob1);
            Assert.IsTrue(submission.GetJobGroupByJob(groupjob1).Count == 1);
            var groupJob2 = submission.SubmitJob("John Doe", internalJobId2, jsdlJob2);
            Assert.IsTrue(submission.GetJobGroupByJob(groupjob1).Count == 2);
        }

        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void TestClearingGroupedJobTable()
        {
            var rt = new TwoRuntimes(container);
            var submission = rt.RT0;
            var worker = rt.RT1;

            var group = "blastJobs";
            var job1 = "blast1";
            var job2 = "blast2";
            var owner = "anonymous";
            string internalJobId1 = "id-internalJobID1";
            string internalJobId2 = "id-internalJobID2";
            string groupName, jobName;

            var jsdlJob1 = TestHelper.CreateGroupJSDL(group, job1);
            var jsdlJob2 = TestHelper.CreateGroupJSDL(group, job2);

            Assert.AreEqual<string>(string.Format("GroupID://{0}/{1}", group, job1), jsdlJob1.CustomerJobID);

            Assert.IsTrue(JobID.TryParseGroup(jsdlJob1.CustomerJobID, out groupName, out jobName));
            Assert.AreEqual<string>(groupName, group);
            Assert.AreEqual<string>(jobName, job1);

            var groupjob1 = submission.SubmitJob(owner, internalJobId1, jsdlJob1);
            Assert.IsTrue(submission.GetJobGroupByJob(groupjob1).Count == 1);
            var groupjob2 = submission.SubmitJob(owner, internalJobId2, jsdlJob2);
            TestHelper.AdvanceNewJobToStatus(worker, groupjob1, JobStatus.Failed);
            TestHelper.AdvanceNewJobToStatus(worker, groupjob2, JobStatus.Failed);
            Assert.AreEqual(3, worker.AllJobs.Count());
            worker.DeleteTerminatedJobs(owner);
            Assert.AreEqual(0, worker.AllJobs.Count());
        }

        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void TestClearingFlatJobTable()
        {
            var rt = new TwoRuntimes(container);
            var worker = rt.RT0;
            var submission = rt.RT1;
            string owner = "UnitTest";
            string id = "id-internalJobID-" + Guid.NewGuid().ToString();

            //finished job
            var finishedJob = submission.SubmitJob(owner, id, TestHelper.CreateJSDL);
            Assert.IsTrue(worker.MarkJobAsChekingInputData(finishedJob, "worker1"));
            Assert.IsTrue(worker.MarkJobAsRunning(finishedJob, "", "", ""));
            Assert.IsTrue(worker.MarkJobAsFinished(finishedJob, ""));

            //finished job from other researcher
            var otherResearcherJob = submission.SubmitJob("other researcher", id, TestHelper.CreateJSDL);
            Assert.IsTrue(worker.MarkJobAsChekingInputData(otherResearcherJob, "worker1"));
            Assert.IsTrue(worker.MarkJobAsRunning(otherResearcherJob, "", "", ""));
            Assert.IsTrue(worker.MarkJobAsFinished(otherResearcherJob, ""));

            //failed job
            id = "id-internalJobID-" + Guid.NewGuid().ToString();
            var failedJob = submission.SubmitJob(owner, id, TestHelper.CreateJSDL);
            Assert.IsTrue(worker.MarkJobAsChekingInputData(failedJob, "worker1"));
            Assert.IsTrue(worker.MarkJobAsRunning(failedJob, "", "", ""));
            Assert.IsTrue(worker.MarkJobAsFailed(failedJob, ""));

            //submitted job
            id = "id-internalJobID-" + Guid.NewGuid().ToString();
            var submittedJob = submission.SubmitJob(owner, id, TestHelper.CreateJSDL);

            var numJobs = worker.AllJobs.Count();
            var numTerminatedJobs = worker.AllJobs.Where(j => j.Owner==owner && (j.Status == JobStatus.Finished || j.Status == JobStatus.Failed || j.Status == JobStatus.Cancelled)).Count();
            worker.DeleteTerminatedJobs(owner);
            var numRemainingJobs = worker.AllJobs.Count();
            Assert.IsTrue(numRemainingJobs == (numJobs - numTerminatedJobs));
        }

        [TestMethod]
        [Timeout(4*testTimeOutInMilliSeconds)]
        public void TestClearingHierarchicalJobTable()
        {
            var rt = new TwoRuntimes(container);
            var worker = rt.RT0;
            var submission = rt.RT1;
            string owner = "UnitTest";
            string id = "id-internalJobID-" + Guid.NewGuid().ToString();

            #region submit two single jobs and advance them to a terminated state
            //finished job
            var finishedJob = submission.SubmitJob(owner, id, TestHelper.CreateJSDL);
            Assert.IsTrue(worker.MarkJobAsChekingInputData(finishedJob, "worker1"));
            Assert.IsTrue(worker.MarkJobAsRunning(finishedJob, "", "", ""));
            Assert.IsTrue(worker.MarkJobAsFinished(finishedJob, ""));

            //failed job
            id = "id-internalJobID-" + Guid.NewGuid().ToString();
            var failedJob = submission.SubmitJob(owner, id, TestHelper.CreateJSDL);
            Assert.IsTrue(worker.MarkJobAsChekingInputData(failedJob, "worker1"));
            Assert.IsTrue(worker.MarkJobAsRunning(failedJob, "", "", ""));
            Assert.IsTrue(worker.MarkJobAsFailed(failedJob, "")); 
            #endregion

            #region Submit a hierarchy of 7 jobs
            var rootJobId = "id-root";
            var rootJob = submission.SubmitJob(owner, rootJobId, TestHelper.CreateHierarchicalJSDL("jobid://Root"));
            Assert.IsNotNull(rootJob);


            rootJob = TestHelper.GetRoot(worker, rootJobId, rootJob);
            var jobIDRoot_1 = "jobid://Root/1";
            Assert.IsTrue(worker.AcceptLocalJob(jobIDRoot_1));
            var jobRoot_1 = TestHelper.SubmitChildren(worker, jobIDRoot_1);

            var jobIDRoot_1_1 = "jobid://Root/1/1";
            var jobRoot_1_1 = TestHelper.SubmitChildren(worker, jobIDRoot_1_1);
            rootJob = TestHelper.GetRoot(worker, rootJobId, rootJob);

            var jobIDRoot_2 = "jobid://Root/2";
            var jobRoot_2 = TestHelper.SubmitChildren(worker, jobIDRoot_2);
            var jobIDRoot_2_1 = "jobid://Root/2/1";
            var jobRoot_2_1 = TestHelper.SubmitChildren(worker, jobIDRoot_2_1);
            rootJob = jobRoot_1;
            rootJobId = jobRoot_1.InternalJobID;
            rootJob = TestHelper.GetRoot(worker, rootJobId, rootJob);

            var jobIDRoot_1_2 = "jobid://Root/1/2";
            var jobRoot_1_2 = TestHelper.SubmitChildren(worker, jobIDRoot_1_2);

            rootJob = jobRoot_2;
            rootJobId = jobRoot_2.InternalJobID;
            rootJob = TestHelper.GetRoot(worker, rootJobId, rootJob);
            var jobIDRoot_2_2 = "jobid://Root/2/2";
            var jobRoot_2_2 = TestHelper.SubmitChildren(worker, jobIDRoot_2_2);            
            #endregion

            var numJobs = worker.AllJobs.Count();
            var numTerminatedJobs = worker.AllJobs.Where(j => j.Status == JobStatus.Finished || j.Status == JobStatus.Failed || j.Status == JobStatus.Cancelled).Count();
            Assert.AreEqual(2, numTerminatedJobs); //make sure that only the single jobs are terminated
            worker.DeleteTerminatedJobs(owner);
            var numRemainingJobs = worker.AllJobs.Count();
            Assert.IsTrue(numRemainingJobs <= numJobs - 2);
            numJobs = worker.AllJobs.Count();
            Assert.AreEqual(7, numJobs); //make sure that only the hierarchy is still in the system 
            #region Move some jobs to failed/cancelled state
            Assert.IsTrue(submission.MarkJobAsFailed(submission.GetJobByID(jobRoot_1.InternalJobID), "failure"));
            Assert.IsTrue(submission.MarkJobAsFailed(submission.GetJobByID(jobRoot_2.InternalJobID), "failure"));
            //all jobs but the root job are now in cancelrequested state
            Assert.IsTrue(submission.MarkJobAsCancelled(submission.GetJobByID(jobRoot_1.InternalJobID), "failure"));
            Assert.IsTrue(submission.MarkJobAsCancelled(submission.GetJobByID(jobRoot_2.InternalJobID), "failure"));
            Assert.IsTrue(submission.MarkJobAsCancelled(submission.GetJobByID(jobRoot_2_1.InternalJobID), "failure"));
            Assert.IsTrue(submission.MarkJobAsCancelled(submission.GetJobByID(jobRoot_2_2.InternalJobID), "failure"));
            Assert.IsTrue(submission.MarkJobAsCancelled(submission.GetJobByID(jobRoot_1_1.InternalJobID), "failure"));
            Assert.IsTrue(submission.MarkJobAsCancelled(submission.GetJobByID(jobRoot_1_2.InternalJobID), "failure"));

            #endregion
            #region Deleting the terminated jobs should have no effect, because one job in the hierarchy is still not terminated
            worker.DeleteTerminatedJobs(owner);
            numRemainingJobs = worker.AllJobs.Count();
            Assert.AreEqual(numJobs,numRemainingJobs); 
            #endregion
            #region Move the last job in the hierarchy to failed state and try to delete the terminated jobs
            Assert.IsTrue(submission.MarkJobAsFailed(submission.GetJobByID("id-root"), "failure"));
            Assert.IsTrue(submission.MarkJobAsCancelled(submission.GetJobByID("id-root"), "failure"));
            worker.DeleteTerminatedJobs(owner);
            numRemainingJobs = worker.AllJobs.Count(); 
            #endregion

            Assert.AreEqual(0,numRemainingJobs); //make sure the hierarchy was deleted since all its members were terminaed
        }
    }
}