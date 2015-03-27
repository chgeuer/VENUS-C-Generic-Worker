//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.EMIC.Cloud.GenericWorker;
using Microsoft.EMIC.Cloud.DataManagement;
using Microsoft.EMIC.Cloud.Storage.Azure;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.EMIC.Cloud.ApplicationRepository;
using Ionic.Zip;
using System.Web;
using System.Runtime.Serialization;
using System.Configuration;
using System.ServiceModel;
using Microsoft.EMIC.Cloud.Security;
using System.Security.Cryptography.X509Certificates;
using OGF.BES;
using Microsoft.WindowsAzure;

namespace InstallSameAppByDifferentUserAndSubmitJobs
{
    class Program
    {
        static CloudBlobContainer appDataContainer;

        private static void WaitForBlob(string blobName)
        {
            int attempts = 0;

            while (true)
            {
                var r = appDataContainer.GetBlobReference(blobName);
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

        static void Main(string[] args)
        {
            Action<ConsoleColor, string> print = (color, str) =>
            {
                Console.ForegroundColor = color;
                Console.Out.WriteLine(str);
                Console.ResetColor();
            };

            var issuerAddress = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.STS.URL"];
            var authThumprint = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.AuthCert.Thumbprint"];
            var dnsEnpointId = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.AuthCert.DnsEndpointId"];

            var jobJobManagementServiceServiceUrl = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.JobManagementService.URL"];
            var storageConnectionString = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.Demo.User.ConnectionString"];

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

            var UserDataStoreConnectionString = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.Demo.User.ConnectionString"];
            var account = CloudStorageAccount.Parse(UserDataStoreConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            appDataContainer = blobClient.GetContainerReference("applicationcontainer");
            appDataContainer.CreateIfNotExist();

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
                zip.AddFile(@"..\..\..\..\core\Test.SimpleMathConsoleApp\bin\Debug\SimpleMathConsoleApp.exe", "");
                zip.Save(ms);
            }

            CloudBlob appBlob = uploadApp(appDesc.ApplicationIdentificationURI, ms);
            CloudBlob appDescBlob = uploadAppDesc(appDesc);

            #endregion

            var appReference = new Reference(new AzureBlobReference(appBlob, storageConnectionString));
            var descReference = new Reference(new AzureBlobReference(appDescBlob, storageConnectionString));

            #region Submit job

            Func<string, string, string> upload = ((name, content) =>
            {
                var blob = appDataContainer.GetBlobReference(name);
                blob.UploadText(content);
                var blobAddress = blob.Uri.AbsoluteUri;
                return blobAddress;
            });

            Func<string, string> computeNameAndDeleteIfExist = ((name) =>
            {
                var r = appDataContainer.GetBlobReference(name);
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
                         ConnectionString = storageConnectionString
                     },
                     new AzureArgumentSingleReference
                     { 
                         Name = "OutputFile", 
                         DataAddress = resultAddress, 
                         ConnectionString = storageConnectionString
                     },
                     new SwitchArgument 
                     { 
                         Name = "Operation", 
                         Value = true 
                     }
                },
            };

            Func<string, GenericWorkerJobManagementClient> CreateSecureClient = (username) =>
            {
                print(ConsoleColor.Red, string.Format("Using GenericWorkerJobManagement at: {0}", jobJobManagementServiceServiceUrl));
                print(ConsoleColor.Red, string.Format("fetching tokens from {0}", issuerAddress));

                // In the development setting (when talking to localhost), we need to override WCF security checks
                // with the DnsEndpointIdentity.
                //

                return GenericWorkerJobManagementClient.CreateSecureClient(
                    address: new EndpointAddress(new Uri(jobJobManagementServiceServiceUrl), new DnsEndpointIdentity(dnsEnpointId)),
                    issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                    username: username,
                    password: "secret",
                    serviceCert: X509Helper.GetX509Certificate2(
                        StoreLocation.LocalMachine, StoreName.My,
                        authThumprint,
                        X509FindType.FindByThumbprint));
            };

            var aliceSubmissionPortal = CreateSecureClient("Alice");
            var bobSubmissionPortal = CreateSecureClient("Bob");

            CreateActivityResponse resp = aliceSubmissionPortal.SubmitVENUSJob(job);

            WaitForBlob(resultFile);

            Action<string> deleteBlob = (blobName) =>
            {
                var blob = appDataContainer.GetBlobReference(blobName);
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

            //reupload app
            appBlob = uploadApp(appDesc.ApplicationIdentificationURI, ms);
            appDescBlob = uploadAppDesc(appDesc);

            job.CustomerJobID = DateTime.Now.Ticks.ToString();
            ////submitting another job for installed app            
            aliceSubmissionPortal.SubmitVENUSJob(job);

            WaitForBlob(resultFile);
            deleteBlob(resultFile);

            bobSubmissionPortal.SubmitVENUSJob(job);
            WaitForBlob(resultFile);
            deleteBlob(resultFile);

            bobSubmissionPortal.SubmitVENUSJob(job);
            WaitForBlob(resultFile);
            deleteBlob(resultFile);
        }
    }
}
