//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel.Composition.Hosting;
using Microsoft.EMIC.Cloud.Demo;
using Microsoft.EMIC.Cloud.DataManagement;
using Microsoft.EMIC.Cloud.GenericWorker.AzureProvider;
using Microsoft.EMIC.Cloud.Storage.Azure;
using Microsoft.EMIC.Cloud.ApplicationRepository;
using Microsoft.WindowsAzure.StorageClient;
using System.IO;
using System.Runtime.Serialization;
using System.Web;
using Ionic.Zip;
using Microsoft.EMIC.Cloud.GenericWorker;
using OGF.BES;
using System.Threading;
using Microsoft.WindowsAzure;
using System.ServiceModel;
using Microsoft.EMIC.Cloud.Utilities;
using System.ServiceModel.Description;
using Microsoft.EMIC.Cloud.UserAdministration;
using Microsoft.EMIC.Cloud.Administrator.Host;
using System.Threading.Tasks;
using System.Data.Services.Client;
using Microsoft.EMIC.Cloud.GenericWorker.AzureAccounting;

namespace Tests
{
    [TestClass]
    public class AzureAccountingTests
    {
        private const int testTimeOutInMilliSeconds = 2 * 3 * 60 * 1000;
        private static ServiceHost genericWorkerHost;
        static CompositionContainer container;
        private static CloudStorageAccount account;
        private static CloudBlobContainer userDataContainer;
        private static CloudBlobClient blobClient;
        private static CancellationTokenSource cts;

        private static string TranslatePort81_85(string url)
        {
            return url;
        }

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

            TestHelper.FlushTable(container);

            account = CloudStorageAccount.Parse(CloudSettings.UserDataStoreConnectionString);
            blobClient = account.CreateCloudBlobClient();
            userDataContainer = blobClient.GetContainerReference(CloudSettings.UserDataStoreBlobName);
            userDataContainer.CreateIfNotExist();
            BlobContainerPermissions resultPermissions = userDataContainer.GetPermissions();
            resultPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
            userDataContainer.SetPermissions(resultPermissions);

            cts = new CancellationTokenSource();
            #region service setup

            genericWorkerHost = new ServiceHost(typeof(BESFactoryPortTypeImplService));
            genericWorkerHost.Description.Behaviors.Add(new MyServiceBehavior<BESFactoryPortTypeImplService>(container));
            genericWorkerHost.Description.Behaviors.Find<ServiceDebugBehavior>().IncludeExceptionDetailInFaults = true;
            genericWorkerHost.AddServiceEndpoint(typeof(BESFactoryPortType), WCFUtils.CreateUnprotectedBinding(), TranslatePort81_85(CloudSettings.GenericWorkerUrl));
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
            TestHelper.FlushTable(container);
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

            Thread.Sleep(6500);//To be on the save side
        }

        private AccountingTableDataContext GetAccountingDataContext()
        {
            return new AccountingTableDataContext(
               account.TableEndpoint.AbsoluteUri,
               account.Credentials, CloudSettings.GenericWorkerAccountingTableName);
        }

        private void UploadApplication(string applicationIdentificationURI, out Reference appReference, out Reference descReference)
        {
            
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
                        },
                        new CommandLineArgument() {
                            Name = "WaitTime",
                            FormatString = "-wait {0}",
                            Required = Required.Optional,
                            CommandLineArgType = CommandLineArgType.SingleLiteralArgument
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
            appReference = new Reference(new AzureBlobReference(appBlob, CloudSettings.UserDataStoreConnectionString));
            descReference = new Reference(new AzureBlobReference(appDescBlob, CloudSettings.UserDataStoreConnectionString));           

            #endregion
        }

        private static void CreateVenusJob(string applicationIdentificationURI, string inputFileContent, string customerJobID, Reference appReference, Reference descReference, 
            out string resultFile, out VENUSJobDescription job, int waitTime)
        {
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
                return ComputeNameAndDeleteIfExist(name);
            });

            //string inputFileContent = "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20";
            string remoteFileAddress = upload("input2.csv", inputFileContent);
            resultFile = "result.csv";

            string resultAddress = computeNameAndDeleteIfExist(resultFile);
            string resultZipName = "myresults.zip";
            string resultZipAddress = computeNameAndDeleteIfExist(resultZipName);
            #endregion

            job = new VENUSJobDescription()
            {
                ApplicationIdentificationURI = applicationIdentificationURI,
                CustomerJobID = customerJobID,
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
                     },
                     new LiteralArgument 
                     { 
                         Name = "WaitTime", 
                         LiteralValue = waitTime.ToString() 
                     }
                },
            };
        }

        private static string ComputeNameAndDeleteIfExist(string name)
        {
            var r = userDataContainer.GetBlobReference(name);
            r.DeleteIfExists();
            return r.Uri.AbsoluteUri;
        }

        /// <summary>
        /// Test title: AccountingTestFinishedJob
        /// Description:
        ///     in this test a job is submitted and executed in the system. After the job is finished the accounting entries for the job are checked. 
        /// Author:
        ///     EMIC / emic_cce@microsoft.com
        /// Category:
        ///     Job Management, positive, local, end2end
        /// Running services/endpoints: 
        ///     running Job Management endpoint    
        /// Expected Results: 
        ///     The accounting entity has to have all mandatory fields filled with consistent data:
        ///         start and stoptime, with stoptime after the start time
        ///         one non empty input and output file, and logged inbound and outbound network traffic 
        /// </summary>  
        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void AccountingTestFinishedJob()
        {
            var driver = container.GetExportedValue<GenericWorkerDriver>();

            Task.Factory.StartNew(() => driver.Run(cts.Token));
            string applicationIdentificationURI = "http://www.microsoft.com/emic/cloud/demo/SimpleMathApp";
            Reference appReference = null;
            Reference descReference = null;

            UploadApplication(applicationIdentificationURI, out appReference, out descReference);

            string resultFile;
            VENUSJobDescription job;
            CreateVenusJob(applicationIdentificationURI, "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20", "Accounting Regular Test 1 - Finished", appReference, descReference, out resultFile, out job, 0);

            var submissionPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);

            CreateActivityResponse resp = submissionPortal.SubmitVENUSJob(job);

            WaitForBlob(resultFile);
            
            bool isFound = false;
            AccountingTableEntity accountingEntity = null;
            int numberOfTries = 20;
            while (numberOfTries >= 0)
            {
                accountingEntity = GetAccountingDataContext().AllAccountingInfo.Where(entity => entity.CustomerJobID == job.CustomerJobID).SingleOrDefault();
                if (accountingEntity != null && accountingEntity.IsFinished)
                {
                    isFound = true;
                    break;
                }
                numberOfTries--;
                Thread.Sleep(2000);

            }

            Assert.IsTrue(isFound);
            Assert.IsFalse(String.IsNullOrEmpty(accountingEntity.InstanceID));
            
            //VM Usage Test
            Assert.IsNotNull(accountingEntity.EndTime);
            Assert.IsNotNull(accountingEntity.StartTime);
            Assert.IsTrue(accountingEntity.StartTime <= accountingEntity.EndTime);

            //Storage Test
            Assert.IsTrue(accountingEntity.NumberOfInputFiles == 1);
            Assert.IsTrue(accountingEntity.NumberofOutputFiles == 1);
            Assert.IsTrue(accountingEntity.SizeofInputFiles > 0);
            Assert.IsTrue(accountingEntity.SizeofOutputFiles > 0);
            Assert.IsTrue(accountingEntity.SizeofInputFiles > accountingEntity.SizeofOutputFiles);

            //NetworkTest
            Assert.IsTrue(accountingEntity.SentBytesStart > 0);
            Assert.IsTrue(accountingEntity.SentBytesEnd > 0);
            Assert.IsTrue(accountingEntity.ReceivedBytesStart > 0);
            Assert.IsTrue(accountingEntity.ReceivedBytesEnd > 0);
            Assert.IsTrue(accountingEntity.ReceivedBytesEnd >= accountingEntity.ReceivedBytesStart);
            Assert.IsTrue(accountingEntity.SentBytesEnd >= accountingEntity.SentBytesStart);
        }

        /// <summary>
        /// Submits a job with the autogeneration policy regarding missing result files. The job will be successfully executed but fails to generate a specified result file. The test makes sure that the status of the job will be finished and that an empty file is created.
        /// </summary>
        [TestMethod]
        public void StatusCodeZeroMissingOutputFileWithAutoGenerationTest()
        {
            var driver = container.GetExportedValue<GenericWorkerDriver>();

            Task.Factory.StartNew(() => driver.Run(cts.Token));
            string applicationIdentificationURI = "http://www.microsoft.com/emic/cloud/demo/SimpleMathApp";
            Reference appReference = null;
            Reference descReference = null;

            UploadApplication(applicationIdentificationURI, out appReference, out descReference);

            string resultFile;
            VENUSJobDescription job;
            CreateVenusJob(applicationIdentificationURI, "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20", "Accounting Regular Test 1 - Finished", appReference, descReference, out resultFile, out job, 0);
            job.Uploads.Add(new Reference(new AzureBlobReference(ComputeNameAndDeleteIfExist("missingUpLoadFile"), CloudSettings.UserDataStoreConnectionString)));
            job.MissingResultFilePolicy = MissingResultFilePolicy.GenerateZeroFiles;
            var submissionPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);

            CreateActivityResponse resp = submissionPortal.SubmitVENUSJob(job);

            WaitForBlob(resultFile);

            bool isFound = false;
            AccountingTableEntity accountingEntity = null;
            int numberOfTries = 20;
            while (numberOfTries >= 0)
            {
                accountingEntity = GetAccountingDataContext().AllAccountingInfo.Where(entity => entity.CustomerJobID == job.CustomerJobID).SingleOrDefault();
                if (accountingEntity != null && accountingEntity.IsFinished)
                {
                    isFound = true;
                    break;
                }
                numberOfTries--;
                Thread.Sleep(2000);

            }

            Assert.IsTrue(isFound);
            Assert.IsFalse(String.IsNullOrEmpty(accountingEntity.InstanceID));
            Assert.AreEqual("Finished",accountingEntity.Status);
            //Storage Test
            Assert.IsTrue(accountingEntity.NumberofOutputFiles == 2);
        }

        /// <summary>
        /// Submits a job with the standard policy regarding missing result files. The job will be successfully executed but fails to generate a specified result file. The test makes sure that the status of the job will be failed.
        /// </summary>
        [TestMethod]
        public void StatusCodeZeroMissingOutputFileWithoutAutoGenerationTest()
        {
            var driver = container.GetExportedValue<GenericWorkerDriver>();

            Task.Factory.StartNew(() => driver.Run(cts.Token));
            string applicationIdentificationURI = "http://www.microsoft.com/emic/cloud/demo/SimpleMathApp";
            Reference appReference = null;
            Reference descReference = null;

            UploadApplication(applicationIdentificationURI, out appReference, out descReference);

            string resultFile;
            VENUSJobDescription job;
            CreateVenusJob(applicationIdentificationURI, "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20", "Accounting Regular Test 1 - Finished", appReference, descReference, out resultFile, out job, 0);
            job.Uploads.Add(new Reference(new AzureBlobReference(ComputeNameAndDeleteIfExist("missingUpLoadFile"), CloudSettings.UserDataStoreConnectionString)));
            var submissionPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);

            CreateActivityResponse resp = submissionPortal.SubmitVENUSJob(job);

            WaitForBlob(resultFile);

            bool isFound = false;
            AccountingTableEntity accountingEntity = null;
            int numberOfTries = 20;
            while (numberOfTries >= 0)
            {
                accountingEntity = GetAccountingDataContext().AllAccountingInfo.Where(entity => entity.CustomerJobID == job.CustomerJobID).SingleOrDefault();
                if (accountingEntity != null && accountingEntity.IsFinished)
                {
                    isFound = true;
                    break;
                }
                numberOfTries--;
                Thread.Sleep(2000);

            }

            Assert.IsTrue(isFound);
            Assert.IsFalse(String.IsNullOrEmpty(accountingEntity.InstanceID));
            Assert.AreEqual( "Failed",accountingEntity.Status);
            //Storage Test
            Assert.IsTrue(accountingEntity.NumberofOutputFiles == 1);
        }

        /// <summary>
        /// Test title: AccountingTestFailedJob
        /// Description:
        ///     in this test a job is submitted and executed in the system. After the job is failed the accounting entries for the job are checked. 
        /// Author:
        ///     EMIC / emic_cce@microsoft.com
        /// Category:
        ///     Job Management, positive, local, end2end
        /// Running services/endpoints: 
        ///     running Job Management endpoint    
        /// Expected Results: 
        ///     The accounting entity has to have all mandatory fields filled with consistent data:
        ///         start and stoptime, with stoptime after the start time
        ///         one non empty input file, and logged inbound and outbound network traffic 
        /// </summary>  
        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void AccountingTestFailedJob()
        {
            var driver = container.GetExportedValue<GenericWorkerDriver>();

            Task.Factory.StartNew(() => driver.Run(cts.Token));
            string applicationIdentificationURI = "http://www.microsoft.com/emic/cloud/demo/SimpleMathApp";
            Reference appReference = null;
            Reference descReference = null;

            UploadApplication(applicationIdentificationURI, out appReference, out descReference);

            string resultFile;
            VENUSJobDescription job;
            CreateVenusJob(applicationIdentificationURI, "trial, some input, they cannot be summed, eexception should be thrown", "Accounting Regular Test 2 - Failed", appReference, descReference, out resultFile, out job, 0);

            var submissionPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);

            CreateActivityResponse resp = submissionPortal.SubmitVENUSJob(job);
            var endpoint = resp.CreateActivityResponse1.ActivityIdentifier;

            string id = TestHelper.GetIdFromEndPointReference(resp.CreateActivityResponse1.ActivityIdentifier);
            List<EndpointReferenceType> endPointList = new List<EndpointReferenceType>();
            endPointList.Add(endpoint);

            while (true)
            {
                var statusResponse = submissionPortal.GetActivityStatuses(endPointList);
                Assert.IsNotNull(statusResponse);
                Assert.IsNotNull(statusResponse.GetActivityStatusesResponse1.Response);
                Assert.IsNotNull(statusResponse.GetActivityStatusesResponse1.Response[0]);
                Assert.AreEqual(id, TestHelper.GetIdFromEndPointReference(statusResponse.GetActivityStatusesResponse1.Response[0].ActivityIdentifier));

                if (statusResponse.GetActivityStatusesResponse1.Response[0].ActivityStatus.state == ActivityStateEnumeration.Failed)
                    break;
                else
                    Thread.Sleep(1000);
            }

            bool isFound = false;
            AccountingTableEntity accountingEntity = null;
            int numberOfTries = 20;
            while (numberOfTries >= 0)
            {
                accountingEntity = GetAccountingDataContext().AllAccountingInfo.Where(entity => entity.CustomerJobID == job.CustomerJobID).SingleOrDefault();
                if (accountingEntity != null && accountingEntity.IsFinished)
                {
                    isFound = true;
                    break;
                }
                numberOfTries--;
                Thread.Sleep(2000);

            }
            Assert.IsTrue(isFound);

            Assert.IsFalse(String.IsNullOrEmpty(accountingEntity.InstanceID));

            //VM Usage Test
            Assert.IsNotNull(accountingEntity.EndTime);
            Assert.IsNotNull(accountingEntity.StartTime);
            Assert.IsTrue(accountingEntity.StartTime <= accountingEntity.EndTime);

            //Storage Test
            Assert.IsTrue(accountingEntity.NumberOfInputFiles == 1);
            Assert.IsTrue(accountingEntity.NumberofOutputFiles == 0);
            Assert.IsTrue(accountingEntity.SizeofInputFiles > 0);
            Assert.IsTrue(accountingEntity.SizeofOutputFiles == 0);
            Assert.IsTrue(accountingEntity.SizeofInputFiles > accountingEntity.SizeofOutputFiles);

            //NetworkTest
            Assert.IsTrue(accountingEntity.SentBytesStart > 0);
            Assert.IsTrue(accountingEntity.SentBytesEnd > 0);
            Assert.IsTrue(accountingEntity.ReceivedBytesStart > 0);
            Assert.IsTrue(accountingEntity.ReceivedBytesEnd > 0);
            Assert.IsTrue(accountingEntity.ReceivedBytesEnd >= accountingEntity.ReceivedBytesStart);
            Assert.IsTrue(accountingEntity.SentBytesEnd >= accountingEntity.SentBytesStart);


        }

        /// <summary>
        /// Test title: AccountingTestCancelledJob
        /// Description:
        ///     in this test a job is submitted and cancelled during execution in the system. After cancelation the accounting entries for the job are checked. 
        /// Author:
        ///     EMIC / emic_cce@microsoft.com
        /// Category:
        ///     Job Management, positive, local, end2end
        /// Running services/endpoints: 
        ///     running Job Management endpoint    
        /// Expected Results: 
        ///     The accounting entity has to have all mandatory fields filled with consistent data:
        ///         start and stoptime, with stoptime after the start time
        ///         one non empty input file, and logged inbound and outbound network traffic 
        /// </summary>  
        [TestMethod]
        [Timeout(10*60*1000)]
        public void AccountingTestCancelledJob()
        {
            var driver = container.GetExportedValue<GenericWorkerDriver>();

            Task.Factory.StartNew(() => driver.Run(cts.Token));
            string applicationIdentificationURI = "http://www.microsoft.com/emic/cloud/demo/SimpleMathApp";
            Reference appReference = null;
            Reference descReference = null;

            UploadApplication(applicationIdentificationURI, out appReference, out descReference);

            string resultFile;
            VENUSJobDescription job;
            CreateVenusJob(applicationIdentificationURI, "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20", "Accounting Regular Test 3 - Cancelled", appReference, descReference, out resultFile, out job, 500000);

            var submissionPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);

            CreateActivityResponse resp = submissionPortal.SubmitVENUSJob(job);
            var endpoint = resp.CreateActivityResponse1.ActivityIdentifier;

            string id = TestHelper.GetIdFromEndPointReference(resp.CreateActivityResponse1.ActivityIdentifier);
            List<EndpointReferenceType> endPointList = new List<EndpointReferenceType>();
            endPointList.Add(endpoint);
            while (true)
            {
                var statusResponse = submissionPortal.GetActivityStatuses(endPointList);
                Assert.IsNotNull(statusResponse);
                Assert.IsNotNull(statusResponse.GetActivityStatusesResponse1.Response);
                Assert.IsNotNull(statusResponse.GetActivityStatusesResponse1.Response[0]);
                Assert.AreEqual(id, TestHelper.GetIdFromEndPointReference(statusResponse.GetActivityStatusesResponse1.Response[0].ActivityIdentifier));

                if (statusResponse.GetActivityStatusesResponse1.Response[0].ActivityStatus.state == ActivityStateEnumeration.Running)
                    break;
                else
                    Thread.Sleep(1000);
            }

             bool isFound = false;
             AccountingTableEntity accountingEntity = null;
             int numberOfTries = 20;
             while (numberOfTries >= 0)
             {
                 accountingEntity = GetAccountingDataContext().AllAccountingInfo.Where(entity => entity.CustomerJobID == job.CustomerJobID).SingleOrDefault();
                 if (accountingEntity != null)
                 {
                     isFound = true;
                     break;
                 }
                 numberOfTries--;
                 Thread.Sleep(2000);

             }
             Assert.IsTrue(isFound);

            Assert.IsFalse(String.IsNullOrEmpty(accountingEntity.InstanceID));
            Assert.IsTrue(!accountingEntity.IsFinished);

            submissionPortal.TerminateActivities(endPointList);

            while (true)
            {
                accountingEntity = GetAccountingDataContext().AllAccountingInfo.Where(entity => entity.CustomerJobID == job.CustomerJobID).SingleOrDefault();

                var statusResponse = submissionPortal.GetActivityStatuses(endPointList);
                Assert.IsNotNull(statusResponse);
                Assert.IsNotNull(statusResponse.GetActivityStatusesResponse1.Response);
                Assert.IsNotNull(statusResponse.GetActivityStatusesResponse1.Response[0]);
                Assert.AreEqual(id, TestHelper.GetIdFromEndPointReference(statusResponse.GetActivityStatusesResponse1.Response[0].ActivityIdentifier));

                if (statusResponse.GetActivityStatusesResponse1.Response[0].ActivityStatus.state == ActivityStateEnumeration.Cancelled && accountingEntity.IsFinished)
                    break;
                else
                    Thread.Sleep(1000);
            }

            //get AccountingTable
            accountingEntity = GetAccountingDataContext().AllAccountingInfo.Where(entity => entity.CustomerJobID == job.CustomerJobID).SingleOrDefault();

            Assert.IsNotNull(accountingEntity);
            Assert.IsFalse(String.IsNullOrEmpty(accountingEntity.InstanceID));
            Assert.IsTrue(accountingEntity.IsFinished);

            //VM Usage Test
            Assert.IsNotNull(accountingEntity.EndTime);
            Assert.IsNotNull(accountingEntity.StartTime);
            Assert.IsTrue(accountingEntity.StartTime <= accountingEntity.EndTime);

            //Storage Test
            Assert.IsTrue(accountingEntity.NumberOfInputFiles == 1);
            Assert.IsTrue(accountingEntity.NumberofOutputFiles == 0);
            Assert.IsTrue(accountingEntity.SizeofInputFiles > 0);
            Assert.IsTrue(accountingEntity.SizeofOutputFiles == 0);
            Assert.IsTrue(accountingEntity.SizeofInputFiles > accountingEntity.SizeofOutputFiles);

            //NetworkTest
            Assert.IsTrue(accountingEntity.SentBytesStart > 0);
            Assert.IsTrue(accountingEntity.SentBytesEnd > 0);
            Assert.IsTrue(accountingEntity.ReceivedBytesStart > 0);
            Assert.IsTrue(accountingEntity.ReceivedBytesEnd > 0);
            Assert.IsTrue(accountingEntity.ReceivedBytesEnd >= accountingEntity.ReceivedBytesStart);
            Assert.IsTrue(accountingEntity.SentBytesEnd >= accountingEntity.SentBytesStart);


        }

        /// <summary>
        /// Test title: AccountingTestCancelledBeforeRunning
        /// Description:
        ///     in this test a job is submitted and cancelled before it is executed in the system. 
        /// Author:
        ///     EMIC / emic_cce@microsoft.com
        /// Category:
        ///     Job Management, positive, local, end2end
        /// Running services/endpoints: 
        ///     running Job Management endpoint    
        /// Expected Results: 
        ///     The job should not have a corresponding accounting entity
        /// </summary>  
        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void AccountingTestCancelledBeforeRunning()
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

            using ("GetJobByID".Stop())
            {
                var j = submission.GetJobByID(id);
                Assert.IsTrue(submission.MarkJobAsCancelled(j,"canceled job"));
                AssertStatus(JobStatus.Cancelled);
            }

            
            AccountingTableEntity accountingEntity = null;
            try
            {
                //get AccountingTable
                accountingEntity = GetAccountingDataContext().AllAccountingInfo.Where(entity => entity.PartitionKey == j1.Owner && entity.RowKey == j1.InternalJobID).SingleOrDefault();
            }
            catch (DataServiceQueryException)
            {
            }

            Assert.IsNull(accountingEntity);

            #endregion
        }

        private static void WaitForBlob(string blobName)
        {
            int attempts = 0;

            while (true)
            {
                var r = userDataContainer.GetBlobReference(blobName);
                try
                {
                    r.FetchAttributes();
                    return;
                }
                catch (StorageClientException)
                {
                    if (attempts++ == 200)
                    {
                        throw new TimeoutException("No result seen");
                    }

                    Thread.Sleep(2000);
                }
            }
        }
    }
}
