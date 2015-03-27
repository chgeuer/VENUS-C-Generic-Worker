//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.ServiceModel;
using System.Threading;
using Ionic.Zip;
using Microsoft.EMIC.Cloud.ApplicationRepository;
using Microsoft.EMIC.Cloud.GenericWorker;
using Microsoft.EMIC.Cloud.DataManagement;
using Microsoft.EMIC.Cloud.Storage.Azure;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.Web;
using System.Runtime.Serialization;
using Microsoft.EMIC.Cloud.Security;
using System.Security.Cryptography.X509Certificates;

namespace PowerShellTest
{
    class PowerShellTestProgram
    {
        /// <summary>
        /// PowerShellTest is a simple application to demonstrate how a Windows PowerShell script can be executed in the GenericWorker. 
        /// </summary>
        /// <param name="args">The arguments.</param>
        static void Main(string[] args)
        {
            string psApplicationUri = "http://msdn.microsoft.com/en-us/library/dd835506(v=VS.85).aspx" + DateTime.UtcNow.ToString();

            Console.Title = "PowerShell runner";

            Console.WriteLine("Press <return> to start");
            Console.ReadLine();
            Action<ConsoleColor, string> print = (color, str) =>
            {
                Console.ForegroundColor = color;
                Console.Out.WriteLine(str);
                Console.ResetColor();
            };

            #region Upload app

            // create application package
            MemoryStream AppZipBytes = new MemoryStream();
            using (var zip = new ZipFile())
            {
                zip.AddFile("ps.cmd");
                // zip.AddEntry("ps.cmd", "@\"%windir%\\system32\\WindowsPowerShell\\v1.0\\powershell.exe\" -ExecutionPolicy Unrestricted -File %1");
                zip.Save(AppZipBytes);
            }
            AppZipBytes.Seek(0L, SeekOrigin.Begin);
           
            // create application description
            VENUSApplicationDescription psDescription = new VENUSApplicationDescription()
            {
                ApplicationIdentificationURI = psApplicationUri,
                CommandTemplate = new VENUSCommandTemplate()
                {
                    Path = string.Empty,
                    Executable = "ps.cmd",
                    Args = new List<CommandLineArgument>() 
                    {
                        new CommandLineArgument(){
                            Name = "scriptfile",
                            FormatString = "\"{0}\"",
                            Required = Required.Mandatory,
                            CommandLineArgType = CommandLineArgType.SingleReferenceInputArgument
                        }
                    }
                }
            };

            var dataStoreConnectionString = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.Demo.User.ConnectionString"];
            var account = CloudStorageAccount.Parse(dataStoreConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer appDataContainer = blobClient.GetContainerReference("applicationcontainer");
            appDataContainer.CreateIfNotExist();
            CloudBlobContainer userDataContainer = blobClient.GetContainerReference("userdatacontainer" + DateTime.Now.ToString("yyyyMMddhhmm"));
            userDataContainer.CreateIfNotExist();
            BlobContainerPermissions resultPermissions = userDataContainer.GetPermissions();
            resultPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
            userDataContainer.SetPermissions(resultPermissions);

            // upload application description
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

            // upload application
            Func<string, MemoryStream, CloudBlob> uploadApp = (appURI, zipBytes) =>
            {
                var blobName = HttpUtility.UrlEncode(appURI) + "_App";
                CloudBlob applicationBlob = appDataContainer.GetBlobReference(blobName);
                applicationBlob.UploadByteArray(zipBytes.ToArray());

                print(ConsoleColor.Green, string.Format("Uploaded {0} bytes", zipBytes.Length));
                return applicationBlob;
            };

            var appBlob = uploadApp(psDescription.ApplicationIdentificationURI, AppZipBytes);
            var appDescBlob = uploadAppDesc(psDescription);

            #endregion

            #region Upload user data

            // upload file method
            Func<string, string, string> uploadFile = ((blobname, filename) =>
            {
                var blob = userDataContainer.GetBlobReference(blobname);
                blob.UploadFile(filename);
                var blobAddress = blob.Uri.AbsoluteUri;

                print(ConsoleColor.Blue, string.Format("Uploaded \"{0}\" to {1}", new FileInfo(filename).FullName, blobAddress));
                print(ConsoleColor.Blue, "");

                return blobAddress;
            });

            string addressInputFile = uploadFile("script.ps1", "script.ps1");

            // compute the name of the output file
            Func<string, string> computeName = ((name) =>
            {
                var result = userDataContainer.GetBlobReference(name).Uri.AbsoluteUri;

                print(ConsoleColor.Blue, string.Format("Expecting result at {0}", result));

                return result;
            });

            #endregion

            #region Define jobs

            // create job description
            var psJob = new VENUSJobDescription()
            {
                ApplicationIdentificationURI = psApplicationUri,
                CustomerJobID = "PowerShell script invocation " + DateTime.Now.ToLocalTime().ToString(),
                JobName = "some job name",
                AppPkgReference = new Reference(new AzureBlobReference(appBlob, dataStoreConnectionString)),
                AppDescReference = new Reference(new AzureBlobReference(appDescBlob, dataStoreConnectionString)),
                JobArgs = new ArgumentCollection()
                {
                        new SingleReference 
                        { 
                            Name = "scriptfile",
                            Ref = new Reference
                            {
                                ProviderSpecificReference = new AzureBlobReference
                                {
                                    DataAddress = addressInputFile, 
                                    ConnectionString = dataStoreConnectionString,                                 
                                }
                            }
                        }
                }
            };

            #endregion

            #region Submit jobs

            var GenericWorkerUrl = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.GenericWorker.URL"];

            print(ConsoleColor.Red, string.Format("Submitting all jobs to {0}", GenericWorkerUrl));
            print(ConsoleColor.White, System.Text.Encoding.UTF8.GetString(psJob.AsMemoryStream().ToArray()));

            // configuration parameters are taken from the configuration file
            var issuerAddress = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.STS.URL"];
            var authThumprint = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.AuthCert.Thumbprint"];
            var dnsEnpointId = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.AuthCert.DnsEndpointId"];
            var secNotificationServiceUrl = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.SecureNotificationService.URL"];
            var unsecNotificationSvcUrl = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.NotificationService.URL"];
            var secureGenericWorkerURL = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.UsernamePasswordSecuredNotificationService.URL"];
            var jobJobManagementServiceServiceUrl = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.JobManagementService.URL"];
            var storageConnectionString = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.Demo.User.ConnectionString"];

            //var submissionPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(GenericWorkerUrl);
            var submissionPortal = GenericWorkerJobManagementClient.CreateSecureClient(
                    address: new EndpointAddress(new Uri(jobJobManagementServiceServiceUrl), new DnsEndpointIdentity(dnsEnpointId)),
                    issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                    username: "Researcher",
                    password: "secret",
                    serviceCert: X509Helper.GetX509Certificate2(
                        StoreLocation.LocalMachine, StoreName.My,
                        authThumprint,
                        X509FindType.FindByThumbprint));
            
            submissionPortal.SubmitVENUSJob(psJob);
            print(ConsoleColor.Red, "PowerShell job submitted...");

            Console.WriteLine("Press <return> to exit");
            Console.ReadLine();

            #endregion

            
        }

        private static void Poll(CloudBlobContainer container, string blobName, string filename, CancellationToken cts, TimeSpan interval)
        {
            CloudBlob blobRef = container.GetBlobReference(blobName);
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    blobRef.DownloadToFile(filename);

                    Console.WriteLine("Successfully downloaded result of job {0}",
                        new FileInfo(filename).FullName);

                    return;
                }
                catch (StorageClientException)
                {
                    Thread.Sleep(interval);
                }
            }
        }
    }
}
