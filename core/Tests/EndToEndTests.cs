//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Ionic.Zip;
using KTH.GenericWorker.CDMI;
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
using OGF.BES;
using System.Text.RegularExpressions;

namespace Tests
{  

    [TestClass]
    public class OtherTests
    {
        private const int testTimeOutInMilliSeconds = 3 * 2 * 60  * 1000;
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

        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void BinaryCallingBinary()
        {
            Task.Factory.StartNew(() => driver.Run(cts.Token));
            string applicationIdentificationURI = "http://www.microsoft.com/emic/cloud/demo/BinCallBin";

            #region Upload application

            Console.WriteLine("Uploading application");

            VENUSApplicationDescription appDesc = new VENUSApplicationDescription()
            {
                ApplicationIdentificationURI = applicationIdentificationURI,
                CommandTemplate = new VENUSCommandTemplate()
                {
                    Path = @"subdir1;subdir2;..\..\abc",
                    Executable = "Test.BinaryCallingOtherBinary.exe",
                    Args = new List<CommandLineArgument>() 
                    {
                        new CommandLineArgument() { 
                            Name = "RelPathToExec", 
                            FormatString = "{0}", 
                            Required = Required.Mandatory,
                            CommandLineArgType = CommandLineArgType.SingleLiteralArgument
                        }
                    }
                }
            };

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
                zip.AddFile(@"..\..\..\core\Test.BinaryCallingOtherBinary\bin\Debug\Test.BinaryCallingOtherBinary.exe", "");
                zip.AddFile(@"..\..\..\core\Test.BinaryCallingOtherBinary\bin\Debug\run.bat", @"subdir2\");
                zip.Save(ms);
            }
            CloudBlob appBlob = uploadApp(appDesc.ApplicationIdentificationURI, ms);
            CloudBlob appDescBlob = uploadAppDesc(appDesc);

            var appReference = new Reference(new AzureBlobReference(appBlob, CloudSettings.UserDataStoreConnectionString));
            var descReference = new Reference(new AzureBlobReference(appDescBlob, CloudSettings.UserDataStoreConnectionString));

            #endregion

            #region Submit job

            Func<string, string> computeNameAndDeleteIfExist = ((name) =>
            {
                var r = userDataContainer.GetBlobReference(name);
                r.DeleteIfExists();
                return r.Uri.AbsoluteUri;
            });

            var outfile = "BinaryCallBinary.txt";
            string resultAddress = computeNameAndDeleteIfExist(outfile);
            #endregion

            var job = new VENUSJobDescription()
            {
                ApplicationIdentificationURI = applicationIdentificationURI,
                CustomerJobID = "EndToEndTest job ID 124",
                JobName = "Invoke to show at Aachen meeting",
                AppPkgReference = appReference,
                AppDescReference = descReference,
                JobArgs = new ArgumentCollection()
                {
                    new LiteralArgument{
                        Name = "RelPathToExec",
                        LiteralValue = "run.bat"
                    }
                },
                Uploads = new ReferenceCollection()
                {
                     new Reference(new AzureBlobReference(resultAddress,CloudSettings.UserDataStoreConnectionString)),
                }
            };

            var submissionPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);

            CreateActivityResponse resp = submissionPortal.SubmitVENUSJob(job);

            bool foundResult = false;

            #region Poll for result

            int attempts = 0;
            while (!foundResult)
            {
                try
                {
                    var r = userDataContainer.GetBlobReference(outfile);
                    var t = r.DownloadText();
                    foundResult = true;
                }
                catch (StorageClientException ex)
                {
                    if (ex.ErrorCode != StorageErrorCode.BlobNotFound)
                    {
                        throw;
                    }

                    if (attempts++ == 20)
                    {
                        throw new TimeoutException("No result seen");
                    }

                    Thread.Sleep(2000);
                }
            }
            #endregion

            Assert.IsTrue(foundResult);
        }

        /// <summary>
        /// Test title: ScalingServiceMockTest
        /// Description:
        ///     This test case updates the number of instances for a mock deployment from 2 to 4, and retrieves the number of instances after the update
        /// Author:
        ///     EMIC / emic_cce@microsoft.com
        /// Category:
        ///     Resource Management, positive, local, end2end
        /// Running services/endpoints: 
        ///     running Scaling Service endpoint    
        /// Expected Results: 
        ///     number of instances is 4
        /// </summary>
        [TestMethod]
        [Timeout(testTimeOutInMilliSeconds)]
        public void ScalingServiceMockTest()
        {
            var testCloudProvider = "TestCloudProvider";
            var initialInstanceCount = 2;
            var updateInstanceCountTo = 4;

            var scalingClient = ScalingServiceClient.CreateUnprotectedClient(CloudSettings.ScalingServiceUrl);
            var deployments = scalingClient.ListDeployments();
            Assert.IsTrue(deployments.Count == 1);
            var deployment = deployments.ElementAt(0);

            Assert.IsTrue(deployment.CloudProviderID == testCloudProvider);
            Assert.IsTrue(deployment.InstanceCount == initialInstanceCount);
            scalingClient.UpdateDeployment(new DeploymentSize() { CloudProviderID = testCloudProvider, InstanceCount = updateInstanceCountTo });

            var instancesAfterUpdate = scalingClient.ListDeployments().ElementAt(0).InstanceCount;
            Assert.AreEqual(updateInstanceCountTo, instancesAfterUpdate);
            scalingClient.Close();
        }


        /*
         * 1. Test title: ReinstallApplicationAzureBlob_Test
         *      This test case covers the execution of a job after its application package is updated on azure blob store
         * 2. Running services/endpoints: running Job Management endpoint     
         * 3. Inputfiles: none
         * 4. Commandline: none
         * 5. Outputfiles: none
         * 6. Expected Results: 
         *      gw driver has to detect that the package was updated and needs to be reinstalled
         */
        //TODO: the test also tests if gw driver can detect that the local and the cloud package are equal. in this case the package is not reinstalled
        //[TestMethod]
        //[Timeout(testTimeOutInMilliSeconds*3)]
        public void ReinstallApplicationAzureBlob_Test()
        {
            Func<string, MemoryStream, Reference> uploadApp = (appURI, zipBytes) =>
            {
                var blobName = HttpUtility.UrlEncode(appURI) + "_App";
                CloudBlob applicationBlob = userDataContainer.GetBlobReference(blobName);
                applicationBlob.UploadByteArray(zipBytes.ToArray());

                return new Reference(new AzureBlobReference(applicationBlob, CloudSettings.UserDataStoreConnectionString));
            };
            ReinstallApplication(uploadApp);
        }

        /*
         * 1. Test title: ReinstallApplicationCDMI_Test
         *      This test case covers the execution of a job after its application package is updated on cdmi blob store
         * 2. Running services/endpoints: running Job Management endpoint     
         * 3. Inputfiles: none
         * 4. Commandline: none
         * 5. Outputfiles: none
         * 6. Expected Results: 
         *      gw driver has to detect that the package was updated and needs to be reinstalled
         */
        //TODO: the test also tests if gw driver can detect that the local and the cloud package are equal. in this case the package is not reinstalled
        [TestMethod]
        public void ReinstallApplicationCDMI_Test()
        {
            Func<string, MemoryStream, Reference> uploadApp = (appURI, zipBytes) =>
            {
                bool useSecureBinding = false;
                var cdmiAddress = useSecureBinding ? "https://emicloudbuild:8080" : "http://emicloudbuild:2365";
                var username = "user";
                var password = "cdmipass";

                var fileName = "zipPackage";
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
                using (var f = File.Create(fileName))
                {
                    f.Write(zipBytes.ToArray(), 0, (int)zipBytes.Length);
                }
                var blobRef = new Reference(fileName, cdmiRef);
                blobRef.Upload(".");
                Assert.IsTrue(blobRef.ExistsDataItem());
                File.Delete(fileName);
                var bytes = blobRef.DownloadContents();
                using (var zip = ZipFile.Read(bytes))
                {
                    Assert.IsTrue(zip.Entries.Count > 0);
                }
                return blobRef;
            };
            ReinstallApplication(uploadApp);
        }

        private void ReinstallApplication(Func<string, MemoryStream, Reference> uploadApp)
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

            MemoryStream ms = new MemoryStream();
            using (var zip = new ZipFile())
            {
                zip.AddFile(@"..\..\..\core\Test.SimpleMathConsoleApp\bin\Debug\SimpleMathConsoleApp.exe", "");
                zip.Save(ms);
            }

            var appReference = uploadApp(appDesc.ApplicationIdentificationURI, ms);
            CloudBlob appDescBlob = uploadAppDesc(appDesc);

            #endregion
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
            var resultFile = "result.csv";

            string resultAddress = computeNameAndDeleteIfExist(resultFile);
            string resultZipName = "myresults.zip";
            string resultZipAddress = computeNameAndDeleteIfExist(resultZipName);
            #endregion

            var job = new VENUSJobDescription()
            {
                ApplicationIdentificationURI = applicationIdentificationURI,
                CustomerJobID = "EndToEndTest job ID 124",
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
            };

            var submissionPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);

            CreateActivityResponse resp = submissionPortal.SubmitVENUSJob(job);

            TestHelper.WaitForBlob(resultFile, userDataContainer);

            Action<string> deleteBlob = (blobName) =>
            {
                var blob = userDataContainer.GetBlobReference(blobName);
                blob.DeleteIfExists();
                while (true)
                {
                    try
                    {
                        blob.FetchAttributes();
                    }
                    catch
                    {
                        return;
                    }
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            };

            deleteBlob(resultFile);


            job.CustomerJobID = DateTime.Now.Ticks.ToString();
            ////submitting another job for installed app

            var owner = "anonymous";
            Assert.IsFalse(driver.FilesystemMapper.IsRemoteAppNewer(owner, job, appDesc));

            submissionPortal.SubmitVENUSJob(job);

            TestHelper.WaitForBlob(resultFile, userDataContainer);
            deleteBlob(resultFile);

            //update application -> next job should trigger reinstall
            uploadApp(appDesc.ApplicationIdentificationURI, ms);
            job.CustomerJobID = DateTime.Now.Ticks.ToString();

            Assert.IsTrue(driver.FilesystemMapper.IsRemoteAppNewer(owner, job, appDesc));
        }
    }

    [TestClass]
    public class EndToEndTests
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
        protected static CompositionContainer genericWorkerContainer,containerGW;
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

        [TestMethod]
        public void LimitedBlockAppendTest()
        {
            var blobName = Guid.NewGuid().ToString();
            var blobRef = userDataContainer.GetBlockBlobReference(blobName);
            var textPart1 = "part1";
            var textPart2 = "part2";
            var completeText = textPart1 + textPart2;
            var bytesForOnePart = System.Text.Encoding.Unicode.GetByteCount(textPart1);
            var truncatedStr = completeText.limitSizeInBytesUTF16(bytesForOnePart);

            blobRef.AppendText(textPart1, bytesForOnePart);
            var text = blobRef.DownloadText();
            Assert.AreEqual(textPart1, text);

            blobRef.AppendText(completeText, bytesForOnePart);
            text = blobRef.DownloadText();
            Assert.IsTrue(text.Length > (3 * textPart1.Length));
            Assert.IsTrue(Regex.Match(text, string.Format("{0}<.*>{1}",textPart1,textPart2)).Success);
        }


        public void BlockIdTest()
        {
            var blobName = "testblob";
            var blobRef = userDataContainer.GetBlockBlobReference(blobName);
            blobRef.UploadText("AB");
            var numBlocks = 100;
            var blockList = new List<string>();
            for (int i = 0; i < numBlocks; i++)
            {
                var newId = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace('+', 'A').Replace('\\', 'B');
                using (var ms = new MemoryStream(new byte[2] { 65, 66 }))
                {
                    blockList.Add(newId);
                    blobRef.PutBlock(newId, ms, null);
                    blobRef.PutBlockList(blockList);
                }
            }
        }

        [TestMethod]
        public void LimitedStorageBlockAppendTest()
        {
            //Todo: all limits have to be configurable
            var blobName = "largeblockblobtest";
            var blobRef = userDataContainer.GetBlockBlobReference(blobName);
            blobRef.DeleteIfExists();
            int maxBytes = 400000;
            var maxPossibleSize = 2 * 1000 * 1000 + maxBytes;  //hardcoded limit + maxUsedBlockSize
            using (var ms = new MemoryStream())
            {                
                var b = new byte[maxBytes];
                var c = new char[maxBytes/2];
                var numNewBytes = System.Text.Encoding.Unicode.GetBytes(c, 0, c.Length, b, 0);
                ms.Write(b, 0, b.Length);
                var text = System.Text.Encoding.Unicode.GetString(b, 0, b.Length);
                var sumBytes =0;
                while( sumBytes < (maxBytes*100))
                {
                    sumBytes += numNewBytes;
                    blobRef.AppendText(text, maxBytes);
                }                    
            }
            var bl = blobRef.DownloadBlockList().ToList();
            var blockList = bl.Select(b => b.Name);
            var fullSize = bl.Select(b => b.Size).Sum();

            Assert.IsTrue(fullSize <= maxPossibleSize);
        }

        [TestMethod]
        public void SimpleJobTest()
        {
            Task.Factory.StartNew(() => driver.Run(cts.Token));
            var jobManagementPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);

            string inputFileContent = "1,2,3,4,5";
            string inputFileName = "input2.csv";
            string resultFileName = "result.csv";
            string localFileName = "abcd";
            Func<string, Reference> AzureRefBuilder = (fileName) =>
            {
                var azref = new AzureBlobReference(TestHelper.computeNameAndDeleteIfExist(fileName, userDataContainer), CloudSettings.UserDataStoreConnectionString);
                return new Reference(localFileName, azref);
            };
            var localLocation = AzureRefBuilder(inputFileName).GetFileLocation(".");
            Assert.IsTrue(localLocation.EndsWith(localFileName));

            var job = TestHelper.GetMathJob(AzureRefBuilder, inputFileName, resultFileName, applicationIdentificationURI, appReference, descReference);
            jobManagementPortal.SubmitVENUSJob(job);

            TestHelper.upload(inputFileName, inputFileContent, userDataContainer);
            TestHelper.PollForOutputFiles(appDesc, new List<VENUSJobDescription>() { job }, TimeSpan.FromSeconds(300));
            TestHelper.computeNameAndDeleteIfExist(resultFileName, userDataContainer);
        }

        [TestMethod]
        public void VenusJobDetailsTest()
        {
            string inputFileName = "input2.csv";
            string resultFileName = "result.csv";
            string localFileName = "abcd";

            Func<string, Reference> CDMIRefBuilder = (fileName) =>
            {
                bool useSecureBinding = false;
                var cdmiAddress = useSecureBinding ? "https://cdmi.pdc2.pdc.kth.se:8080" : "http://cdmi.pdc2.pdc.kth.se:2365";
                var username = "christian";
                var password = "venusc";

                var inputUri = string.Format("{0}/{1}", cdmiAddress, fileName);
                var cdmiRef = new CDMIBlobReference()
                {
                    URI = inputUri,
                    FileLocation = fileName,
                    Credentials = new NetworkCredential(username, password),
                    RequestFactory = url =>
                    {
                        var request = (HttpWebRequest)WebRequest.Create(url);
                        return request;
                    }
                };
                return new Reference(localFileName, cdmiRef);
            };
            Func<string, Reference> AzureRefBuilder = (fileName) =>
            {
                var azref = new AzureBlobReference(TestHelper.computeNameAndDeleteIfExist(fileName, userDataContainer), CloudSettings.UserDataStoreConnectionString);
                return new Reference(localFileName, azref);
            };
            var azJob = TestHelper.GetMathJob(AzureRefBuilder, inputFileName, resultFileName, applicationIdentificationURI, appReference, descReference);
            foreach (var arg in azJob.JobArgs)
            {
                Assert.IsNotNull(arg.Name);
                var ser = arg.Serialize(new System.Xml.XmlDocument());
                Assert.IsNotNull(ser);
                Assert.IsNotNull(ser.OuterXml);
            }

            var cdmiJob = TestHelper.GetMathJob(CDMIRefBuilder, inputFileName, resultFileName, applicationIdentificationURI, appReference, descReference);
            foreach (var arg in cdmiJob.JobArgs)
            {
                Assert.IsNotNull(arg.Name);
                var ser = arg.Serialize(new System.Xml.XmlDocument());
                Assert.IsNotNull(ser);
                Assert.IsNotNull(ser.OuterXml);
            }
        }

    }
}
