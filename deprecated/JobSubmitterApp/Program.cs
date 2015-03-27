//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using System.Threading;
using Ionic.Zip;
using Microsoft.EMIC.Cloud.ApplicationRepository;
using Microsoft.EMIC.Cloud.DataManagement;
using Microsoft.EMIC.Cloud.Demo;
using Microsoft.EMIC.Cloud.GenericWorker;
using Microsoft.EMIC.Cloud.Storage.Azure;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.Web;
using System.Runtime.Serialization;

namespace JobSubmitterApp
{
    class Program
    {
        const string applicationIdentificationURI = "http://www.microsoft.com/emic/cloud/demo/SimpleMathApp";

        static void Main(string[] args)
        {
            try
            {
                Console.Title = "JobSubmitterApp";

                Console.Write("Press return to upload the application");
                //Console.ReadLine();

                UploadApplications();

                CancellationTokenSource cts = new CancellationTokenSource();

                for (int i = 0; i < 5; i++)
                {
                    Console.Write("Press return to submit a job");

                    var submissionPortal = GenericWorkerJobManagementClient.CreateLocalJobSubmissionClient();

                    #region Upload job1 data

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

                    string jobID = "";
                    try
                    {
                        var parentID = JobID.CurrentJobID;
                        jobID = parentID.CreateChildJob(String.Format("childjob#{0}", i)).ToString();
                    }
                    catch (NotSupportedException)
                    {
                        jobID = String.Format("childjob#{0}", i);
                    }


                    var job = new VENUSJobDescription()
                    {
                        ApplicationIdentificationURI = applicationIdentificationURI,
                        CustomerJobID = jobID,
                        JobName = "Invoke to show at Aachen meeting",
                        AppPkgReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(applicationIdentificationURI) + "_App"), CloudSettings.UserDataStoreConnectionString)),
                        AppDescReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(applicationIdentificationURI) + "_App"), CloudSettings.UserDataStoreConnectionString)), 
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

                    submissionPortal.SubmitVENUSJob(job);
                    Console.WriteLine("Job {0} submitted", i);

                    #endregion

                }

                Console.WriteLine("Submitted jobs, polling results. Press <Return> to cancel polling and close...");

                cts.Cancel();
            }
            finally
            {
                Console.WriteLine("Press <Return> to close...");
            }
        }

        private static void UploadApplications()
        {

            var ApplicationDataStoreConnectionString = CloudSettings.UserDataStoreConnectionString;
            var account = CloudStorageAccount.Parse(ApplicationDataStoreConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer appDataContainer = blobClient.GetContainerReference("applicationcontainer");
            appDataContainer.CreateIfNotExist();

            Console.WriteLine("Uploading application");
            var path = new Uri(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase));
            Console.WriteLine(path.AbsolutePath);

            if (!File.Exists(path.AbsolutePath + "/SimpleMathConsoleApp.exe"))
                throw new FileNotFoundException();


            var mathConsoleZip = new MemoryStream();
            using (var zip = new ZipFile())
            {
                zip.AddFile(path.AbsolutePath + "/SimpleMathConsoleApp.exe", "");
                zip.Save(mathConsoleZip);
            }

            // After the bits are uploaded, provide the missing description

            var mathConsoleDesc = new VENUSApplicationDescription()
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

                Console.WriteLine(string.Format("Uploaded {0} bytes", zipBytes.Length));
                return applicationBlob;
            };

            Action<VENUSApplicationDescription, MemoryStream> uploadPkg = (appDesc, zipBytes) =>
            {
                uploadApp(appDesc.ApplicationIdentificationURI, zipBytes);
                uploadAppDesc(appDesc);
            };

            uploadPkg(mathConsoleDesc, mathConsoleZip);
        }
    }
}
