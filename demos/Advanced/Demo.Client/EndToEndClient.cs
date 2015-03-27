//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ionic.Zip;
using Microsoft.EMIC.Cloud.ApplicationRepository;
using Microsoft.EMIC.Cloud.Demo;
using Microsoft.EMIC.Cloud.GenericWorker;
using Microsoft.EMIC.Cloud.DataManagement;
using Microsoft.EMIC.Cloud.Security;
using Microsoft.EMIC.Cloud.Storage.Azure;
using Microsoft.EMIC.Cloud.Utilities;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.ServiceModel.Channels;
using System.Web;
using System.Runtime.Serialization;

namespace ConsoleApplication1
{
    /// <summary>
    /// This demo sends a simple SimpleMathConsoleApp job multiple times. It is useful for stress testing.
    /// </summary>
    class EndToEndClient
    {
        const string applicationIdentificationURI = "http://www.microsoft.com/emic/cloud/demo/SimpleMathApp";

        internal static Binding CreateSecureServiceBinding()
        {
            var issuerBinding = new WS2007HttpBinding(SecurityMode.Message);
            issuerBinding.Security.Message.EstablishSecurityContext = false;
            issuerBinding.Security.Message.NegotiateServiceCredential = false;
            issuerBinding.Security.Message.ClientCredentialType = MessageCredentialType.UserName;

            var submissionServiceBinding = new WS2007FederationHttpBinding(WSFederationHttpSecurityMode.Message);
            submissionServiceBinding.Security.Message.EstablishSecurityContext = false;
            submissionServiceBinding.Security.Message.NegotiateServiceCredential = false;
            submissionServiceBinding.Security.Message.IssuerAddress = new EndpointAddress(
                new Uri(CloudSettings.CorporateSTSUrl),
                new DnsEndpointIdentity("localhost"));
            submissionServiceBinding.Security.Message.IssuerBinding = issuerBinding;

            return submissionServiceBinding;
        }

        internal static Binding CreateUnprotectedBinding()
        {
            var serviceBinding = new WS2007HttpBinding(
                SecurityMode.None,
                reliableSessionEnabled: false);

            return serviceBinding;
        }

        static void Main(string[] args)
        {
            try
            {
                Console.Title = "End-to-End Client";

                Console.Write("Press return to upload the application");
                Console.ReadLine();

                Reference appReference = null;
                Reference descReference = null;

                // upload and install the application
                UploadApplication(applicationIdentificationURI, out appReference, out descReference);

                CancellationTokenSource cts = new CancellationTokenSource();


                // Submit jobs in a loop
                for (int i = 0; i < 1000; i++)
                {
                    Console.Write("Press return to submit a job");
                    Console.ReadLine();

                    // create client
                    var submissionPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(CloudSettings.GenericWorkerUrl);

                    #region Upload job1 data

                    // create blob container for upload
                    var researcherAccount = CloudStorageAccount.Parse(CloudSettings.UserDataStoreConnectionString);
                    var researcherblobClient = researcherAccount.CreateCloudBlobClient();
                    CloudBlobContainer appDataContainer = researcherblobClient.GetContainerReference("applicationcontainer");
                    appDataContainer.CreateIfNotExist();
                    var userDataContainer = researcherblobClient.GetContainerReference(CloudSettings.UserDataStoreBlobName);
                    userDataContainer.CreateIfNotExist();
                    BlobContainerPermissions resultPermissions = userDataContainer.GetPermissions();
                    resultPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
                    userDataContainer.SetPermissions(resultPermissions);
                    

                    Func<string, string, string> upload = ((name, content) =>
                    {
                        var blob = userDataContainer.GetBlobReference(name);
                        blob.UploadText(content);
                        var blobAddress = blob.Uri.AbsoluteUri;
                        return blobAddress;
                    });

                    Func<string, string> computeName = ((name) =>
                    {
                        return userDataContainer.GetBlobReference(name).Uri.AbsoluteUri;
                    });

                    string inputFileContent = "1,2,3,4,5";
                    string remoteFileAddress = upload("input.csv", inputFileContent);
                    var resultAddress = computeName(string.Format("result--{0}.csv", i));

                    #endregion

                    #region Submit job1

                    // create job description
                    var job = new VENUSJobDescription()
                    {
                        ApplicationIdentificationURI = applicationIdentificationURI,
                        CustomerJobID = "Some customer-selected job ID 123",
                        JobName = "Invoke to show at Aachen meeting",
                        AppPkgReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(applicationIdentificationURI) + "_App"), CloudSettings.UserDataStoreConnectionString)),
                        AppDescReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(applicationIdentificationURI) + "_Desc"), CloudSettings.UserDataStoreConnectionString)), 
                        JobArgs = new ArgumentCollection()
                        {
                             new AzureArgumentSingleReference 
                             { 
                                 Name = "InputFile", 
                                 DataAddress = remoteFileAddress, 
                                 ConnectionString = CloudSettings.UserDataStoreConnectionString
                             },
                             new AzureArgumentSingleReference 
                             { 
                                 Name = "OutputFile", 
                                 DataAddress = resultAddress, 
                                 ConnectionString = CloudSettings.UserDataStoreConnectionString
                             }
                        }
                    };

                    // submit job
                    submissionPortal.SubmitVENUSJob(job);
                    Console.WriteLine("Job {0} submitted", i);

                    #endregion

                    #region Poll for job1 results

                    Task job1ResultPoller = new Task(() => Poll(userDataContainer, resultAddress,
                        "job " + i, cts.Token, TimeSpan.FromSeconds(5)), cts.Token, TaskCreationOptions.AttachedToParent);
                    job1ResultPoller.Start();

                    #endregion

                }

                Console.WriteLine("Submitted jobs, polling results. Press <Return> to cancel polling and close...");
                Console.ReadLine();

                cts.Cancel();
            }
            finally
            {
                Console.WriteLine("Press <Return> to close...");
                Console.ReadLine();
            }
        }

        private static void UploadApplication(string applicationIdentificationURI, out Reference appReference, out Reference descReference)
        {
            var path = @"..\..\..\..\..\core\Test.SimpleMathConsoleApp\bin\Debug\SimpleMathConsoleApp.exe";

            var account = CloudStorageAccount.Parse(CloudSettings.UserDataStoreConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer appDataContainer = blobClient.GetContainerReference("applicationcontainer");
            appDataContainer.CreateIfNotExist();

            //create the application description
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
                CloudBlob xmlBlob = appDataContainer.GetBlobReference(blobName);
                xmlBlob.Properties.ContentType = "text/xml";
                xmlBlob.UploadByteArray(msxml.ToArray());
                return xmlBlob;
            });

            Func<string, MemoryStream, CloudBlob> uploadApp = (appURI, zipBytes) =>
            {
                var blobName = HttpUtility.UrlEncode(appURI) + "_App";
                CloudBlob applicationBlob = appDataContainer.GetBlobReference(blobName);
                applicationBlob.UploadByteArray(zipBytes.ToArray());

                return applicationBlob;
            };

            MemoryStream ms = new MemoryStream();
            using (var zip = new ZipFile())
            {
                zip.AddFile(path, "");
                zip.Save(ms);
            }

            CloudBlob appBlob = uploadApp(appDesc.ApplicationIdentificationURI, ms);
            CloudBlob appDescBlob = uploadAppDesc(appDesc);
            appReference = new Reference(new AzureBlobReference(appBlob, CloudSettings.UserDataStoreConnectionString));
            descReference = new Reference(new AzureBlobReference(appDescBlob, CloudSettings.UserDataStoreConnectionString));

            #endregion
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
            AzureArgumentSingleReference reference = catalog.Get(logicalName) as AzureArgumentSingleReference;
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
}