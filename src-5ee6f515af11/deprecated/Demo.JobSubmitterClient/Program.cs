//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using Ionic.Zip;
using Microsoft.EMIC.Cloud.ApplicationRepository;
using Microsoft.EMIC.Cloud.DataManagement;
using Microsoft.EMIC.Cloud.Demo;
using Microsoft.EMIC.Cloud.GenericWorker;
using Microsoft.EMIC.Cloud.Security;
using Microsoft.EMIC.Cloud.Storage.Azure;
using Microsoft.EMIC.Cloud.Utilities;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Demo.JobSubmitterClient
{
    class Program
    {
        const string applicationIdentificationURI = "http://www.microsoft.com/emic/cloud/demo/JobSubmitterApp";

        //    static AppStoreClient CreateClient(string password)
        //    {
        //        EndpointAddress service = null;
        //        EndpointAddress sts = null;

        //        bool useLocalFaric = true;
        //        if (useLocalFaric)
        //        {
        //            service = new EndpointAddress(new Uri("http://localhost:81/AppStore/SecureService.svc"),
        //                                          new DnsEndpointIdentity("my.genericworker.net"));
        //            sts = new EndpointAddress(new Uri("http://localhost:81/STS/UsernamePassword.svc"),
        //                                          new DnsEndpointIdentity("my.genericworker.net"));
        //        }
        //        else
        //        {
        //            service = new EndpointAddress(new Uri("http://my.genericworker.net/AppStore/SecureService.svc"));
        //            sts = new EndpointAddress(new Uri("http://my.genericworker.net/STS/UsernamePassword.svc"));
        //        }

        //        var bindingElements = WCFUtils.CreateSecureUsernamePasswordClientBinding(sts).CreateBindingElements();
        //        var httpBe = bindingElements.OfType<HttpTransportBindingElement>().FirstOrDefault();
        //        if (httpBe != null)
        //        {
        //            httpBe.KeepAliveEnabled = false;
        //        }

        //        var serviceCertificateThumbprint = "E04FB18B3317F79D5D70B1B6FF9A4C1D45630B01";

        //        var appStore = new AppStoreClient(new CustomBinding(bindingElements), service);
        //        appStore.ClientCredentials.UserName.UserName = "Administrator";
        //        appStore.ClientCredentials.UserName.Password = password; // "secret";
        //        appStore.ClientCredentials.ServiceCertificate.DefaultCertificate = X509Helper.GetX509Certificate2(
        //                                                                                                          StoreLocation.LocalMachine, StoreName.My,
        //                                                                                                          serviceCertificateThumbprint,
        //                                                                                                          X509FindType.FindByThumbprint);

        //        return appStore;
        //    }
        static void Main(string[] args)
        {

            //Console.Write("Enter your password: ");
            //var password = Console.ReadLine();

            //const bool reuseClient = true;
            //Func<AppStoreClient> G;
            //if (reuseClient)
            //{
            //    var appStore = CreateClient(password);
            //    G = () => appStore;
            //}
            //else
            //{
            //    G = () => CreateClient(password);
            //}

            //try
            //{
            //    for (int i = 0; i < 10; i++)
            //    {

            //        var start = DateTime.UtcNow;
            //        var apps = G().ListApplications();
            //        var milliSeconds = (int) DateTime.UtcNow.Subtract(start).TotalMilliseconds;
            //        Console.WriteLine(string.Format("{0,10}ms - {1}", milliSeconds, apps.FirstOrDefault()));
            //    }

            //    Console.Out.WriteLine("Finished listing");
            //}
            //catch (Exception exception)
            //{
            //    Console.WriteLine(exception.GetFullDetailsConcatenated());
            //}


            //Console.Out.Write("Press <return> to close");
            //Console.ReadLine();
        }


        static void MainOld(string[] args)
        {
            try
            {
                //// $HACK turn on security later
                //bool securityTurnedOn = true;

                //Console.Title = "JobSubmitter Client";



                //UploadApplications();

                //CancellationTokenSource cts = new CancellationTokenSource();

                //for (int i = 0; i < 5; i++)
                //{
                //    Console.Write("Press return to submit a job");
                //    Console.ReadLine();



                //    Binding serviceBinding = securityTurnedOn ? CreateSecureServiceBinding() : CreateUnprotectedBinding();
                //    var submissionPortal = new GenericWorkerJobSubmissionClient(
                //        serviceBinding,
                //        new EndpointAddress(
                //            new Uri(CloudSettings.GenericWorkerUrl)));

                //    if (securityTurnedOn)
                //    {
                //        submissionPortal.ClientCredentials.UserName.UserName = "Researcher";
                //        submissionPortal.ClientCredentials.UserName.Password = "secret";
                //        submissionPortal.ClientCredentials.ServiceCertificate.DefaultCertificate = X509Helper.GetX509Certificate2(
                //            StoreLocation.LocalMachine, StoreName.My, "CN=localhost");
                //    }

                //    #region Upload job1 data

                //    var researcherAccount = CloudStorageAccount.Parse(CloudSettings.UserDataStoreConnectionString);
                //    var researcherblobClient = researcherAccount.CreateCloudBlobClient();
                //    var userDataContainer = researcherblobClient.GetContainerReference(CloudSettings.UserDataStoreBlobName);
                //    userDataContainer.CreateIfNotExist();
                //    BlobContainerPermissions resultPermissions = userDataContainer.GetPermissions();
                //    resultPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
                //    userDataContainer.SetPermissions(resultPermissions);

                //    //Func<string, string, string> upload = ((name, content) =>
                //    //{
                //    //    var blob = userDataContainer.GetBlobReference(name);
                //    //    blob.UploadText(content);
                //    //    var blobAddress = blob.Uri.AbsoluteUri;
                //    //    return blobAddress;
                //    //});

                //    //Func<string, string> computeName = ((name) =>
                //    //{
                //    //    return userDataContainer.GetBlobReference(name).Uri.AbsoluteUri;
                //    //});

                //    //string inputFileContent = "1,2,3,4,5";
                //    //string remoteFileAddress = upload("input.csv", inputFileContent);
                //    //var resultAddress = computeName(string.Format("result--{0}.csv", i));

                //    #endregion

                //    #region Submit job1

                //    VENUSJobDescription job;
                //    if (i % 2 == 0)
                //    {
                //        job = new VENUSJobDescription()
                //        {
                //            ApplicationIdentificationURI = applicationIdentificationURI,
                //            CustomerJobID = "Some customer-selected job ID :" + i,
                //            JobName = "Invoke to show at Aachen meeting",
                //            JobArgs = new ArgumentCollection()
                //            {
                //            }
                //        };
                //    }
                //    else
                //    {
                //        job = new VENUSJobDescription()
                //        {
                //            ApplicationIdentificationURI = applicationIdentificationURI,
                //            CustomerJobID = "jobid://Some_customer-selected_job_ID_:" + i,
                //            JobName = "Invoke to show at Aachen meeting",
                //            JobArgs = new ArgumentCollection()
                //            {
                //            }
                //        };
                //    }

                //    submissionPortal.SubmitVENUSJob(job);
                //    Console.WriteLine("Job {0} submitted", i);

                //    #endregion

                //    #region Poll for job1 results

                //    //Task job1ResultPoller = new Task(() => Poll(userDataContainer, resultAddress,
                //    //    "job " + i, cts.Token, TimeSpan.FromSeconds(5)), cts.Token, TaskCreationOptions.AttachedToParent);
                //    //job1ResultPoller.Start();

                //    #endregion

                //}

                //Console.WriteLine("Submitted jobs, polling results. Press <Return> to cancel polling and close...");
                //Console.ReadLine();

                //cts.Cancel();
            }
            finally
            {
                Console.WriteLine("Press <Return> to close...");
                Console.ReadLine();
            }
        }

        private static void UploadApplications()
        {

            Console.Write("Press return to check existence of application in cloud");
            Console.ReadLine();


            Console.WriteLine("Uploading application");
            if (!File.Exists(@"..\..\..\JobSubmitterApp\bin\Debug\JobSubmitterApp.exe"))
                throw new FileNotFoundException();

            var ms = new MemoryStream();
            using (var zip = new ZipFile())
            {
                zip.AddFile(@"..\..\..\JobSubmitterApp\bin\Debug\JobSubmitterApp.exe", "");
                zip.AddFile(@"..\..\..\JobSubmitterApp\bin\Debug\Microsoft.EMIC.Cloud.GenericWorker.dll", "");
                zip.AddFile(@"..\..\..\JobSubmitterApp\bin\Debug\Microsoft.WindowsAzure.StorageClient.dll", "");
                zip.AddFile(@"..\..\..\JobSubmitterApp\bin\Debug\Ionic.Zip.dll", "");
                zip.AddFile(@"..\..\..\JobSubmitterApp\bin\Debug\Microsoft.EMIC.Cloud.Administrator.ServiceContracts.dll", "");
                zip.AddFile(@"..\..\..\JobSubmitterApp\bin\Debug\Microsoft.EMIC.Cloud.ApplicationRepository.dll", "");
                zip.AddFile(@"..\..\..\JobSubmitterApp\bin\Debug\Microsoft.EMIC.Cloud.Storage.Azure.dll", "");
                zip.AddFile(@"..\..\..\JobSubmitterApp\bin\Debug\Microsoft.EMIC.Cloud.Utilities.dll", "");
                zip.AddFile(@"..\..\..\JobSubmitterApp\bin\Debug\OGF.BES.dll", "");
                zip.AddFile(@"..\..\..\JobSubmitterApp\bin\Debug\VENUSAppStore.AzureProvider.dll", "");
                zip.AddFile(@"..\..\..\..\core\Test.SimpleMathConsoleApp\bin\Debug\SimpleMathConsoleApp.exe", "");
                zip.Save(ms);
            }

            var researcherAccount = CloudStorageAccount.Parse(CloudSettings.UserDataStoreConnectionString);
            var researcherblobClient = researcherAccount.CreateCloudBlobClient();
            var userDataContainer = researcherblobClient.GetContainerReference(CloudSettings.UserDataStoreBlobName);
            userDataContainer.CreateIfNotExist();
            BlobContainerPermissions resultPermissions = userDataContainer.GetPermissions();
            resultPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
            userDataContainer.SetPermissions(resultPermissions);

            var appblob = userDataContainer.GetBlobReference(Guid.NewGuid().ToString() + "zippedBytes");
            appblob.UploadFromStream(ms);

        }

        private static void Poll(CloudBlobContainer container, string blobName, string jobName,
            CancellationToken cts, TimeSpan interval)
        {
            CloudBlob blobRef = container.GetBlobReference(blobName);
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    var result = blobRef.DownloadText();
                    Console.WriteLine("Successfully downloaded result of job \"{0}\": {1}", jobName, result);
                    return;
                }
                catch (StorageClientException)
                {
                    Thread.Sleep(interval);
                }
            }
        }

        private static void Poll(string logicalName, string jobName, CancellationToken cts, TimeSpan interval)
        {
            var catalog = new AzureCatalogueHandler(CloudSettings.UserDataStoreConnectionString);
            while (!cts.IsCancellationRequested)
            {
                if (catalog.Exists(logicalName))
                {
                    break;
                }
                Thread.Sleep(interval);
            }
            var reference = catalog.Get(logicalName) as AzureArgumentSingleReference;
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    var bytes = reference.DownloadContents();
                    var result = Encoding.UTF8.GetString(bytes);
                    if (result.Length > 0)
                    {
                        Console.WriteLine("Successfully downloaded result of job \"{0}\": {1}", jobName, result);
                        return;
                    }
                }
                catch (Exception)
                {
                    Thread.Sleep(interval);
                }
            }
        }
    }
}
