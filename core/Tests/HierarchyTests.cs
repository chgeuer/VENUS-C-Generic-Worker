//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EMIC.Cloud.GenericWorker;
using System.ComponentModel.Composition.Hosting;
using Microsoft.EMIC.Cloud.Demo;
using Microsoft.EMIC.Cloud.DataManagement;
using Microsoft.EMIC.Cloud.GenericWorker.AzureProvider;
using Microsoft.EMIC.Cloud.Storage.Azure;
using System.ServiceModel;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.Threading;
using Microsoft.EMIC.Cloud.Utilities;
using System.ServiceModel.Description;
using Microsoft.EMIC.Cloud.UserAdministration;
using Microsoft.EMIC.Cloud.Administrator.Host;
using System.Threading.Tasks;
using OGF.BES;
using Microsoft.EMIC.Cloud;

namespace Tests
{
    [TestClass]
    public class HierarchyTests
    {
        private const int testTimeOutInMilliSeconds = 3 * 3 * 60 * 1000;
        private const bool IsSecurityEnabled = false;

        private static ServiceHost genericWorkerHost;
        private static CancellationTokenSource cts;
        private static CompositionContainer genericWorkerContainer;

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
                     new AssemblyCatalog(typeof(AzureArgumentCatalogueReference).Assembly)
                ));

            TestHelper.FlushTable(genericWorkerContainer);

            genericWorkerHost.Description.Behaviors.Add(new MyServiceBehavior<BESFactoryPortTypeImplService>(genericWorkerContainer));
            genericWorkerHost.Description.Behaviors.Find<ServiceDebugBehavior>().IncludeExceptionDetailInFaults = true;
            genericWorkerHost.AddServiceEndpoint(typeof(BESFactoryPortType), WCFUtils.CreateUnprotectedBinding(), CloudSettings.GenericWorkerUrl);
            genericWorkerHost.Open();

            CreateProfileClient.CreateClientIdentityFileForService(CloudSettings.ProcessIdentityFilename);
            HostState state = new HostState();
            var t = CreateProfileHost.CreateTask(CloudSettings.ProcessIdentityFilename, cts, state);
            t.Start();
            
            #endregion
        }

        [TestCleanup]
        public void TestCleanup()
        {
           

            cts.Cancel();
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
            
            TestHelper.FlushTable(genericWorkerContainer);
            Thread.Sleep(6500);//To be on the save side
        }

        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void AcceptLocalJobTest()
        {
            TwoRuntimes rt = null;
            IGWRuntimeEnvironment submission = null;
            IGWRuntimeEnvironment worker1 = null;

            #region Given a worker
            using ("new TwoRuntimes".Stop())
            {
                rt = new TwoRuntimes(genericWorkerContainer);
                submission = rt.RT0;
                worker1 = rt.RT1;

                Assert.IsNotNull(submission);
                Assert.IsNotNull(worker1);
                Assert.AreNotSame(worker1, submission);

            }

            #endregion

            #region Single Job Submission


            string idSingleJob = "id-internalJobID-" + Guid.NewGuid().ToString();
            IJob iJobSingle = null;
            Action<JobStatus> AssertSingleJobStatus = TestHelper.CreateAssertStatus(idSingleJob, genericWorkerContainer);

            using ("SubmitSingleJob".Stop())
            {
                iJobSingle = submission.SubmitJob("John Doe", idSingleJob, TestHelper.CreateJSDL);
                Assert.IsNotNull(iJobSingle);
            }

            #endregion

            #region Single Job Can Submit Anyjob Locally

            using ("SingleSubmission".Stop())
            {
                worker1.TryDequeueJob(out iJobSingle);
                Assert.IsNotNull(iJobSingle);
                JobID jobid = null;
                JobID.TryParse(iJobSingle.CustomerJobID, out jobid);
                Assert.IsNull(jobid);

                Assert.IsTrue(worker1.AcceptLocalJob("Some job"));
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://1/2/3"));
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://1/"));
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://1"));

                worker1.MarkJobAsCancelled(iJobSingle,"canceled job");

                AssertSingleJobStatus(JobStatus.Cancelled);

            }

            #endregion

            #region Hierarchical Job Submission

            string idHiearhicalJob = "id-internalJobID-" + Guid.NewGuid().ToString();
            IJob iJobHierarchical = null;

            using ("SubmitHierarchicalJob".Stop())
            {
                iJobHierarchical = submission.SubmitJob("John Doe", idHiearhicalJob, TestHelper.CreateHierarchicalJSDL("jobid://Root"));
                Assert.IsNotNull(iJobHierarchical);
            }

            #endregion

            #region Hierarchical Job Submission Test

            using ("HierarchicalSubmission".Stop())
            {
                worker1.TryDequeueJob(out iJobHierarchical);
                Assert.IsNotNull(idHiearhicalJob);
                JobID jobid = null;
                JobID.TryParse(iJobHierarchical.CustomerJobID, out jobid);
                Assert.IsNotNull(jobid);

                Assert.IsTrue(worker1.AcceptLocalJob("Some job"));
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://1/2/3"));
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://1/"));
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://1"));

                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/1"));
                try 
                { 
                    worker1.AcceptLocalJob("jobid://Root/2/3");
                }
                catch(Exception ex)
                {
                    Assert.AreEqual(ExceptionMessages.LevelHierarchyError, ex.Message);
                }

                worker1.MarkJobAsCancelled(iJobHierarchical,"");

                AssertSingleJobStatus(JobStatus.Cancelled);
            }

            #endregion

            #region Test for failed or cancelled Ancestor

            string rootJobId = "id-internalJobID-" + Guid.NewGuid().ToString();
            IJob rootJob = null;

            using ("SubmitRootJob".Stop())
            {
                rootJob = submission.SubmitJob("John Doe", rootJobId, TestHelper.CreateHierarchicalJSDL("jobid://Root"));
                Assert.IsNotNull(rootJob);
            }

            Func<string, IJob> submitAndRunChild = (customerJobID) =>
            {
                IJob testChild = worker1.SubmitJob("John Doe", Guid.NewGuid().ToString(), TestHelper.CreateHierarchicalJSDL(customerJobID), true);
                string testid = testChild.InternalJobID;
                worker1.TryDequeueJob(out testChild);
                Assert.IsNotNull(testChild);
                Assert.AreEqual(testChild.InternalJobID, testid);
                Assert.IsTrue(worker1.MarkJobAsChekingInputData(testChild, "instance1"));
                Assert.IsTrue(worker1.MarkJobAsRunning(testChild, "status", "stdout", "stderr"));
                return testChild;
            };

            IJob firstChild, secondChild;
            using ("CancelledorFailed".Stop())
            {
                rootJob = TestHelper.GetRoot(worker1, rootJobId, rootJob);

                //Failed Test
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/1"));
                firstChild = submitAndRunChild("jobid://Root/1");
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/1/1"));
                submitAndRunChild("jobid://Root/1/1");
                Assert.IsTrue(worker1.MarkJobAsFailed(firstChild, "failedtest"));
                try
                {
                    worker1.AcceptLocalJob("jobid://Root/1/1/1");
                }
                catch (Exception ex)
                {
                    Assert.AreEqual(ExceptionMessages.AncestorIsFailedorCancelled, ex.Message);
                }

                //Cancelled Test
                TestHelper.GetRoot(worker1, rootJobId, rootJob);
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/2"));
                secondChild = submitAndRunChild("jobid://Root/2");
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/2/1"));
                submitAndRunChild("jobid://Root/2/1");
                Assert.IsTrue(worker1.MarkJobAsCancellationPending(secondChild));
                try
                {
                    worker1.AcceptLocalJob("jobid://Root/2/1/1");
                }
                catch (Exception ex)
                {
                    Assert.AreEqual(ExceptionMessages.AncestorIsFailedorCancelled, ex.Message);
                }

                TestHelper.GetRoot(worker1, rootJobId, rootJob);
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/3"));
                submitAndRunChild("jobid://Root/3");
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/3/1"));
                submitAndRunChild("jobid://Root/3/1");
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/3/1/1"));
            }

            #endregion

        }

       

        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void ShowHierarchyTest()
        {
            TwoRuntimes rt = null;
            IGWRuntimeEnvironment submission = null;
            IGWRuntimeEnvironment worker1 = null;

            #region Given a worker
            using ("new TwoRuntimes".Stop())
            {
                rt = new TwoRuntimes(genericWorkerContainer);
                submission = rt.RT0;
                worker1 = rt.RT1;

                Assert.IsNotNull(submission);
                Assert.IsNotNull(worker1);
                Assert.AreNotSame(worker1, submission);

            }

            #endregion

            #region Test for failed or cancelled Ancestor

            string rootJobId = "id-internalJobID-" + Guid.NewGuid().ToString();
            IJob rootJob = null;

            using ("SubmitRootJob".Stop())
            {
                rootJob = submission.SubmitJob("John Doe", rootJobId, TestHelper.CreateHierarchicalJSDL("jobid://Root"));
                Assert.IsNotNull(rootJob);
            }

            using ("SubmitJobsWithoutOrder".Stop())
            {
                string secondID;
                rootJob = TestHelper.GetRoot(worker1, rootJobId, rootJob);

                List<IJob> level1 = new List<IJob>();
                List<IJob> level2 = new List<IJob>();


                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/1"));
                level1.Add(TestHelper.SubmitChildren(worker1, "jobid://Root/1"));
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/1/1"));
                level2.Add(TestHelper.SubmitChildren(worker1, "jobid://Root/1/1"));


                //Cancelled Test
                TestHelper.GetRoot(worker1, rootJobId, rootJob);
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/2"));
                level1.Add(TestHelper.SubmitChildren(worker1, "jobid://Root/2"));
                secondID = level1.Last().InternalJobID;
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/2/1"));
                level2.Add(TestHelper.SubmitChildren(worker1, "jobid://Root/2/1"));



                TestHelper.GetRoot(worker1, rootJobId, rootJob);
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/3"));
                level1.Add(TestHelper.SubmitChildren(worker1, "jobid://Root/3"));
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/3/1"));
                level2.Add(TestHelper.SubmitChildren(worker1, "jobid://Root/3/1"));

                IJob secondJob = worker1.GetJobByID(secondID);
                Assert.IsTrue(worker1.MarkJobAsSubmittedBack(secondJob, "re-submitted"));
                worker1.TryDequeueJob(out secondJob);
                Assert.IsNotNull(secondJob);
                Assert.AreEqual(secondJob.InternalJobID, secondID);
                Assert.IsTrue(worker1.MarkJobAsChekingInputData(secondJob, "instance1"));
                Assert.IsTrue(worker1.MarkJobAsRunning(secondJob, "status", "stdout", "stderr"));
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/2/2"));
                level2.Insert(2, TestHelper.SubmitChildren(worker1, "jobid://Root/2/2"));

                List<IJob> submitted = new List<IJob>(level1);
                submitted.AddRange(level2);
                submitted.Insert(0, rootJob);

                List<IJob> runtimeJobHierarchy = worker1.GetJobHierarchyByRoot(rootJobId);

                //first check the number of submitted items
                Assert.AreEqual(submitted.Count, runtimeJobHierarchy.Count);

                //check whether the order is expected and submitted jobs are there
                for (int i = 0; i < submitted.Count; i++)
                {
                    Assert.AreEqual(submitted[i].InternalJobID, runtimeJobHierarchy[i].InternalJobID);

                }

            }

            #endregion

        }

        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void CancellationHierarchyTest()
        {
            TwoRuntimes rt = null;
            IGWRuntimeEnvironment submission = null;
            IGWRuntimeEnvironment worker1 = null;

            #region Given a worker
            using ("new TwoRuntimes".Stop())
            {
                rt = new TwoRuntimes(genericWorkerContainer);
                submission = rt.RT0;
                worker1 = rt.RT1;

                Assert.IsNotNull(submission);
                Assert.IsNotNull(worker1);
                Assert.AreNotSame(worker1, submission);

            }

            #endregion

            #region Test for failed or cancelled Ancestor

            string rootJobId = "id-internalJobID-" + Guid.NewGuid().ToString();
            IJob rootJob = null;

            using ("SubmitRootJob".Stop())
            {
                rootJob = submission.SubmitJob("John Doe", rootJobId, TestHelper.CreateHierarchicalJSDL("jobid://Root"));
                Assert.IsNotNull(rootJob);
            }


            using ("SubmitJobsWithoutOrder".Stop())
            {
                string secondID;
                rootJob = TestHelper.GetRoot(worker1, rootJobId, rootJob);

                List<IJob> level1 = new List<IJob>();
                List<IJob> level2 = new List<IJob>();


                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/1"));
                level1.Add(TestHelper.SubmitChildren(worker1, "jobid://Root/1"));
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/1/1"));
                level2.Add(TestHelper.SubmitChildren(worker1, "jobid://Root/1/1"));


                //Cancelled Test
                TestHelper.GetRoot(worker1, rootJobId, rootJob);
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/2"));
                level1.Add(TestHelper.SubmitChildren(worker1, "jobid://Root/2"));
                secondID = level1.Last().InternalJobID;
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/2/1"));
                level2.Add(TestHelper.SubmitChildren(worker1, "jobid://Root/2/1"));

                TestHelper.GetRoot(worker1, rootJobId, rootJob);
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/3"));
                level1.Add(TestHelper.SubmitChildren(worker1, "jobid://Root/3"));
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/3/1"));
                level2.Add(TestHelper.SubmitChildren(worker1, "jobid://Root/3/1"));

                IJob secondJob = worker1.GetJobByID(secondID);
                Assert.IsTrue(worker1.MarkJobAsSubmittedBack(secondJob, "re-submitted"));
                worker1.TryDequeueJob(out secondJob);
                Assert.IsNotNull(secondJob);
                Assert.AreEqual(secondJob.InternalJobID, secondID);
                Assert.IsTrue(worker1.MarkJobAsChekingInputData(secondJob, "instance1"));
                Assert.IsTrue(worker1.MarkJobAsRunning(secondJob, "status", "stdout", "stderr"));
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/2/2"));
                level2.Insert(2, TestHelper.SubmitChildren(worker1, "jobid://Root/2/2"));

                List<IJob> submitted = new List<IJob>(level1);
                submitted.AddRange(level2);
                submitted.Insert(0, rootJob);

                worker1.MarkJobAsCancellationPending(secondJob);
                List<IJob> runtimeJobHierarchy = worker1.GetJobHierarchyByRoot(rootJobId);

                //first check the number of submitted items
                Assert.AreEqual(submitted.Count, runtimeJobHierarchy.Count);

                //check whether the order is expected and submitted jobs are there
                for (int i = 0; i < submitted.Count; i++)
                {
                    Assert.AreEqual(submitted[i].InternalJobID, runtimeJobHierarchy[i].InternalJobID);
                    if (i == 2 || i == 5 || i == 6)
                    {
                        var status = worker1.GetJobStatus(submitted[i].InternalJobID);
                        if (status == JobStatus.CancelRequested)
                        {
                            status = JobStatus.Cancelled;
                        }
                        Assert.AreEqual(status, JobStatus.Cancelled);
                    }
                }
            }

            #endregion

        }

       

        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void GetRootTest()
        {
            TwoRuntimes rt = null;
            IGWRuntimeEnvironment submission = null;
            IGWRuntimeEnvironment worker1 = null;

            #region Given a worker
            using ("new TwoRuntimes".Stop())
            {
                rt = new TwoRuntimes(genericWorkerContainer);
                submission = rt.RT0;
                worker1 = rt.RT1;

                Assert.IsNotNull(submission);
                Assert.IsNotNull(worker1);
                Assert.AreNotSame(worker1, submission);

            }

            #endregion

            #region Test for failed or cancelled Ancestor

            string rootJobId = "id-internalJobID-" + Guid.NewGuid().ToString();
            IJob rootJob = null;

            using ("SubmitRootJob".Stop())
            {
                rootJob = submission.SubmitJob("John Doe", rootJobId, TestHelper.CreateHierarchicalJSDL("jobid://Root"));
                Assert.IsNotNull(rootJob);
            }


            List<IJob> submitted;
            using ("SubmitJobsWithoutOrder".Stop())
            {
                string secondID;
                rootJob = TestHelper.GetRoot(worker1, rootJobId, rootJob);

                List<IJob> level1 = new List<IJob>();
                List<IJob> level2 = new List<IJob>();


                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/1"));
                level1.Add(TestHelper.SubmitChildren(worker1, "jobid://Root/1"));
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/1/1"));
                level2.Add(TestHelper.SubmitChildren(worker1, "jobid://Root/1/1"));


                //Cancelled Test
                TestHelper.GetRoot(worker1, rootJobId, rootJob);
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/2"));
                level1.Add(TestHelper.SubmitChildren(worker1, "jobid://Root/2"));
                secondID = level1.Last().InternalJobID;
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/2/1"));
                level2.Add(TestHelper.SubmitChildren(worker1, "jobid://Root/2/1"));



                TestHelper.GetRoot(worker1, rootJobId, rootJob);
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/3"));
                level1.Add(TestHelper.SubmitChildren(worker1, "jobid://Root/3"));
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/3/1"));
                level2.Add(TestHelper.SubmitChildren(worker1, "jobid://Root/3/1"));

                IJob secondJob = worker1.GetJobByID(secondID);
                Assert.IsTrue(worker1.MarkJobAsSubmittedBack(secondJob, "re-submitted"));
                worker1.TryDequeueJob(out secondJob);
                Assert.IsNotNull(secondJob);
                Assert.AreEqual(secondJob.InternalJobID, secondID);
                Assert.IsTrue(worker1.MarkJobAsChekingInputData(secondJob, "instance1"));
                Assert.IsTrue(worker1.MarkJobAsRunning(secondJob, "status", "stdout", "stderr"));
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/2/2"));
                level2.Insert(2, TestHelper.SubmitChildren(worker1, "jobid://Root/2/2"));

                submitted = new List<IJob>(level1);
                submitted.AddRange(level2);
                submitted.Insert(0, rootJob);

            }

            using ("GetRoot".Stop())
            {
                var jobManagementPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);

                foreach (var hierarchicalJob in submitted)
                {
                    Assert.IsTrue(rootJob.InternalJobID == TestHelper.GetIJob(jobManagementPortal.GetRoot(TestHelper.GetEpr(hierarchicalJob)), submission).InternalJobID);
                }
            }

            #endregion

        }

        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void GetHierarchyTest()
        {
            TwoRuntimes rt = null;
            IGWRuntimeEnvironment submission = null;
            IGWRuntimeEnvironment worker1 = null;

            #region Given a worker
            using ("new TwoRuntimes".Stop())
            {
                rt = new TwoRuntimes(genericWorkerContainer);
                submission = rt.RT0;
                worker1 = rt.RT1;

                Assert.IsNotNull(submission);
                Assert.IsNotNull(worker1);
                Assert.AreNotSame(worker1, submission);

            }

            #endregion

            #region Test for failed or cancelled Ancestor

            string rootJobId = "id-internalJobID-" + Guid.NewGuid().ToString();
            IJob rootJob = null;

            using ("SubmitRootJob".Stop())
            {
                rootJob = submission.SubmitJob("John Doe", rootJobId, TestHelper.CreateHierarchicalJSDL("jobid://Root"));
                Assert.IsNotNull(rootJob);
            }

            List<IJob> submitted;
            List<IJob> level1;
            List<IJob> level2;

            using ("SubmitJobsWithoutOrder".Stop())
            {
                string secondID;
                rootJob = TestHelper.GetRoot(worker1, rootJobId, rootJob);

                level1 = new List<IJob>();
                level2 = new List<IJob>();


                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/1"));
                level1.Add(TestHelper.SubmitChildren(worker1, "jobid://Root/1"));
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/1/1"));
                level2.Add(TestHelper.SubmitChildren(worker1, "jobid://Root/1/1"));


                //Cancelled Test
                TestHelper.GetRoot(worker1, rootJobId, rootJob);
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/2"));
                level1.Add(TestHelper.SubmitChildren(worker1, "jobid://Root/2"));
                secondID = level1.Last().InternalJobID;
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/2/1"));
                level2.Add(TestHelper.SubmitChildren(worker1, "jobid://Root/2/1"));



                TestHelper.GetRoot(worker1, rootJobId, rootJob);
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/3"));
                level1.Add(TestHelper.SubmitChildren(worker1, "jobid://Root/3"));
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/3/1"));
                level2.Add(TestHelper.SubmitChildren(worker1, "jobid://Root/3/1"));

                IJob secondJob = worker1.GetJobByID(secondID);
                Assert.IsTrue(worker1.MarkJobAsSubmittedBack(secondJob, "re-submitted"));
                worker1.TryDequeueJob(out secondJob);
                Assert.IsNotNull(secondJob);
                Assert.AreEqual(secondJob.InternalJobID, secondID);
                Assert.IsTrue(worker1.MarkJobAsChekingInputData(secondJob, "instance1"));
                Assert.IsTrue(worker1.MarkJobAsRunning(secondJob, "status", "stdout", "stderr"));
                Assert.IsTrue(worker1.AcceptLocalJob("jobid://Root/2/2"));
                level2.Insert(2, TestHelper.SubmitChildren(worker1, "jobid://Root/2/2"));

                submitted = new List<IJob>(level1);
                submitted.AddRange(level2);
                submitted.Insert(0, rootJob);

            }

            using ("GetHierarchy".Stop())
            {
                var jobManagementPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);

                var someJob = level2[0];
                var rootByService = jobManagementPortal.GetRoot(TestHelper.GetEpr(someJob));
                Assert.IsTrue(rootJob.InternalJobID == TestHelper.GetIJob(rootByService, submission).InternalJobID);
                var allSubmittedJobs = jobManagementPortal.GetHierarchy(rootByService);
                Assert.IsTrue(allSubmittedJobs.Count == 8);
                Assert.IsTrue(allSubmittedJobs.Count == submitted.Count);

               
                while(allSubmittedJobs.Count>0)
                {
                    var epr = allSubmittedJobs[0];
                    bool isFound = false;
                    foreach (var job in submitted)
                    {
                        if (TestHelper.GetIJob(epr, submission).InternalJobID == job.InternalJobID)
                        {
                            allSubmittedJobs.Remove(epr);
                            submitted.Remove(job);
                            isFound = true;
                            break;
                        }
                    }

                    if (!isFound)
                    {
                        Assert.Fail("Some jobs in the hieararchy cannot be found in OGF/BES Hierarchy Extension");
                    }
                }
                Assert.IsTrue(allSubmittedJobs.Count == 0);
                Assert.IsTrue(submitted.Count == 0);
            }

            #endregion

        }
    }
}
