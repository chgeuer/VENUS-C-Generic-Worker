//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Tests
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.ComponentModel.Composition.Hosting;
    using Microsoft.EMIC.Cloud.Demo;
    using Microsoft.EMIC.Cloud.DataManagement;
    using Microsoft.EMIC.Cloud.GenericWorker.AzureProvider;
    using Microsoft.EMIC.Cloud.Storage.Azure;
    using Microsoft.EMIC.Cloud.GenericWorker;
    using System.ServiceModel;
    using OGF.BES;
    using Microsoft.EMIC.Cloud.Utilities;
    using System.ServiceModel.Description;
    using System.Linq;

    [TestClass]
    public class OGF_BES_Test
    {
        static CompositionContainer container;
        private static ServiceHost genericWorkerHost;

        [TestInitialize]
        public void TestInitialize()
        {
            container = new CompositionContainer(
                 new AggregateCatalog(
                     new TypeCatalog(typeof(CloudSettings)),
                     new AssemblyCatalog(typeof(LiteralArgument).Assembly),
                     new AssemblyCatalog(typeof(AzureGWRuntimeEnvironment).Assembly),
                     new AssemblyCatalog(typeof(AzureArgumentCatalogueReference).Assembly)
                ));

            genericWorkerHost = new ServiceHost(typeof(BESFactoryPortTypeImplService));
            genericWorkerHost.Description.Behaviors.Add(new MyServiceBehavior<BESFactoryPortTypeImplService>(container));
            genericWorkerHost.Description.Behaviors.Find<ServiceDebugBehavior>().IncludeExceptionDetailInFaults = true;
            genericWorkerHost.AddServiceEndpoint(typeof(BESFactoryPortType), WCFUtils.CreateUnprotectedBinding(), CloudSettings.GenericWorkerUrl);
            genericWorkerHost.Open();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            genericWorkerHost.Close();

            while (true)
            {
                if (genericWorkerHost.State == CommunicationState.Closed || genericWorkerHost.State == CommunicationState.Faulted)
                {
                    break;
                }
                System.Threading.Thread.Sleep(500);
            }

            genericWorkerHost = null;
        }

        [TestMethod]
        public void OGFTest()
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

            var submissionPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);

            #region When One Job Is Submitted
            
            var response = submissionPortal.SubmitVENUSJob(TestHelper.CreateJSDL);
            Assert.IsNotNull(response);
            var endpoint = response.CreateActivityResponse1.ActivityIdentifier;
            Assert.IsNotNull(endpoint);

            
            string id = TestHelper.GetIdFromEndPointReference(response.CreateActivityResponse1.ActivityIdentifier);

            Action<JobStatus> AssertStatus = TestHelper.CreateAssertStatus(id, container);
            List<EndpointReferenceType> endPointList =  new List<EndpointReferenceType>();
            endPointList.Add(endpoint);

            var statusResponse = submissionPortal.GetActivityStatuses(endPointList);
            Assert.IsNotNull(statusResponse);
            Assert.IsNotNull(statusResponse.GetActivityStatusesResponse1.Response);
            Assert.IsNotNull(statusResponse.GetActivityStatusesResponse1.Response[0]);
            Assert.AreEqual(id, TestHelper.GetIdFromEndPointReference(statusResponse.GetActivityStatusesResponse1.Response[0].ActivityIdentifier));
            Assert.AreEqual(ActivityStateEnumeration.Pending, statusResponse.GetActivityStatusesResponse1.Response[0].ActivityStatus.state);


            var jobDescription = submissionPortal.GetActivityDocuments(endPointList);
            Assert.IsNotNull(jobDescription);
            Assert.IsNotNull(jobDescription.GetActivityDocumentsResponse1.Response);
            Assert.IsNotNull(jobDescription.GetActivityDocumentsResponse1.Response[0]);
            Assert.AreEqual(id, TestHelper.GetIdFromEndPointReference(jobDescription.GetActivityDocumentsResponse1.Response[0].ActivityIdentifier));
            TestHelper.CompareWithSampleJob(jobDescription.GetActivityDocumentsResponse1.Response[0].JobDefinition);

            var cancelResponse = submissionPortal.TerminateActivities(endPointList);
            Assert.IsNotNull(cancelResponse);
            Assert.IsNotNull(cancelResponse.TerminateActivitiesResponse1.Response);
            Assert.IsNotNull(cancelResponse.TerminateActivitiesResponse1.Response[0]);
            Assert.AreEqual(id, TestHelper.GetIdFromEndPointReference(cancelResponse.TerminateActivitiesResponse1.Response[0].ActivityIdentifier));
            Assert.IsNull(cancelResponse.TerminateActivitiesResponse1.Response[0].Fault);

            while (true)
            {
                statusResponse = submissionPortal.GetActivityStatuses(endPointList);
                Assert.IsNotNull(statusResponse);
                Assert.IsNotNull(statusResponse.GetActivityStatusesResponse1.Response);
                Assert.IsNotNull(statusResponse.GetActivityStatusesResponse1.Response[0]);
                Assert.AreEqual(id, TestHelper.GetIdFromEndPointReference(statusResponse.GetActivityStatusesResponse1.Response[0].ActivityIdentifier));
                if (ActivityStateEnumeration.Cancelled == statusResponse.GetActivityStatusesResponse1.Response[0].ActivityStatus.state)
                {
                    break;
                }
            }

            #endregion
        }
    }
}
