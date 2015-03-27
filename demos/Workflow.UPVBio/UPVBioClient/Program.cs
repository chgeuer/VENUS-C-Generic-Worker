//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿
namespace UPVBioClient
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Security.Cryptography.X509Certificates;
    using System.ServiceModel;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.EMIC.Cloud.GenericWorker;
    using Microsoft.EMIC.Cloud.DataManagement;
    using Microsoft.EMIC.Cloud.Security;
    using Microsoft.EMIC.Cloud.Storage.Azure;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;
    using System.Web;
    using System.Xml.Serialization;
    using System.Xml;
    using System.Xml.Linq;

    class Program
    {
        /// <summary>
        /// UPVBioClient is an application that shows how jobs are prepared, created and executed. 
        /// Three executables are executed in the cloud with this application.  
        /// These executables must be uploaded first to the server by running the UPVBioInstaller application. 
        /// </summary>
        /// <param name="Args"></param>
        static void Main(string[] Args)
        {
            Console.Title = "UPVBio End-to-End Client";

            Console.Write("Press <return> to start");
            Console.ReadLine();

            string inputFile = "input_file.fasta";
            string resultFileName = "assembledResult";
            int numFragments = 3;


            var UserDataStoreConnectionString = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.Demo.User.ConnectionString"];
            var account = CloudStorageAccount.Parse(UserDataStoreConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer userDataContainer = blobClient.GetContainerReference("userdatacontainer" + DateTime.Now.ToString("yyyyMMddhhmm"));
            userDataContainer.CreateIfNotExist();

            try
            {
                var cts = new CancellationTokenSource();
                // run the job and poll the results
                RunJobAndPollForResult(inputFile, resultFileName, numFragments, cts.Token, 
                    userDataContainer, UserDataStoreConnectionString);

                // start the Job Management application
                var JobManagementUrl = ConfigurationManager.AppSettings["JobManagementUrl"];
                Process.Start(JobManagementUrl);


                if (AskUser("Polling for job result now, should we stop? ") == Choice.Yes)
                {
                    // cancel the jobs if requested
                    cts.Cancel();
                }
            }
            finally
            {
                // remove the user data container
                Console.WriteLine("Deleting container " + userDataContainer.Uri.AbsoluteUri);
                userDataContainer.Delete();
            }
        }

        private static void RunJobAndPollForResult(string inputFile, string resultFileName, int numFragments, CancellationToken ct, CloudBlobContainer userDataContainer, string UserDataStoreConnectionString)
        {
            Action<ConsoleColor, string> print = (color, str) =>
            {
                Console.ForegroundColor = color;
                Console.Out.WriteLine(str);
                Console.ResetColor();
            };

            // configuration parameters are taken from the configuration file
            var ApplicationStoreConnectionString = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.Demo.User.ConnectionString"];
            var account = CloudStorageAccount.Parse(UserDataStoreConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer appDataContainer = blobClient.GetContainerReference("applicationcontainer");
            appDataContainer.CreateIfNotExist();

            string upvBioURIPrefix = "http://www.upvbio.eu/cloud/demo/gw/UPVBIOApp/";
            string splitterAppIdentificationURI = upvBioURIPrefix + "Splitter";
            string blastAppIdentificationURI = upvBioURIPrefix + "Blast";
            string assemblerAppIdentificationURI = upvBioURIPrefix + "Assembler";

            string addressInputFile = null;

            #region Upload user data

            BlobContainerPermissions resultPermissions = userDataContainer.GetPermissions();
            resultPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
            userDataContainer.SetPermissions(resultPermissions);

            // method for file uploading
            Func<string, string, string> uploadFile = ((blobname, filename) =>
            {
                var blob = userDataContainer.GetBlobReference(blobname);
                blob.UploadFile(filename);
                var blobAddress = blob.Uri.AbsoluteUri;

                print(ConsoleColor.Blue, string.Format("Uploaded \"{0}\" to {1}", new FileInfo(filename).FullName, blobAddress));
                print(ConsoleColor.Blue, "");

                return blobAddress;
            });

            addressInputFile = uploadFile(inputFile, inputFile);

            // method for constructing the url of the file in the server
            Func<string, string> computeName = ((name) =>
            {
                var result = userDataContainer.GetBlobReference(name).Uri.AbsoluteUri;

                print(ConsoleColor.Blue, string.Format("Expecting result at {0}", result));

                return result;
            });

            #endregion

            #region Define jobs

            Func<ReferenceCollection, bool, CloudBlob> uploadReferenceCollection = ((referenceCollection, isUploadsCollection) =>
            {
                var postfix = (isUploadsCollection)?"UploadsCollection":"DownloadsCollection";
                var blobName = Guid.NewGuid().ToString() + postfix;


                var ra = new ReferenceArray() { Name = postfix, References = referenceCollection };

                var xmlDoc = new XmlDocument();
                var serializedRefArr = ra.Serialize(xmlDoc);
                xmlDoc.AppendChild(serializedRefArr);

                CloudBlob xmlBlob;

                xmlBlob = userDataContainer.GetBlobReference(blobName);
                xmlBlob.Properties.ContentType = "text/xml";
                xmlBlob.UploadText(xmlDoc.InnerXml);

                return xmlBlob;
            });

            #region Splitter job
            // define Splitter job description
            var splitterJob = new VENUSJobDescription()
            {
                ApplicationIdentificationURI = splitterAppIdentificationURI,
                CustomerJobID = "UPVBIO Splitter Job " + DateTime.Now.ToLocalTime().ToString(),
                JobName = "some job name",
                AppPkgReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(splitterAppIdentificationURI) + "_App"), ApplicationStoreConnectionString)),
                AppDescReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(splitterAppIdentificationURI) + "_Desc"), ApplicationStoreConnectionString)),
                JobArgs = new ArgumentCollection()
                {
                        new SingleReference
                        {
                            Name="inputfile",
                            Ref= new Reference(
                                    inputFile,
                                    new AzureBlobReference
                                    {
                                        DataAddress = addressInputFile, 
                                        ConnectionString = UserDataStoreConnectionString,                            
                                    })
                        },
                        new LiteralArgument
                        {
                            Name = "numfragments",
                            LiteralValue = String.Format("{0}", numFragments)
                        },
                        new LiteralArgument
                        {
                            Name = "startfragment",
                            LiteralValue = "0"
                        }                        
                }    
            };

         
            //add uploads to job
            //create your own ReferenceCollection:
            var refCol = new ReferenceCollection();

            //add References to it as you did with the Uploads and Downloads property:
            for (int i=0;i<numFragments;i++)
            {
                var seqfileName = string.Format("seqfile{0}.sqf",i);
                refCol.Add(new Reference(seqfileName, new AzureBlobReference(computeName(seqfileName), UserDataStoreConnectionString)));
            }

            //new:
            var uploadsRefBlobSplitter = uploadReferenceCollection(refCol, true);
            splitterJob.UploadsReference = new Reference(new AzureBlobReference(uploadsRefBlobSplitter, UserDataStoreConnectionString));

            //splitterJob.Uploads = refCol;
            #endregion

            #region BLAST job
            // define Blast job description
            Func<int, VENUSJobDescription> getBLASTJob = (index) =>
            {
                var blastJob = new VENUSJobDescription()
                {
                    ApplicationIdentificationURI = blastAppIdentificationURI,
                    CustomerJobID = "UPVBIO BLAST Job-" + index + "-" + DateTime.Now.ToLocalTime().ToString(),
                    JobName = "some job name",
                    AppPkgReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(blastAppIdentificationURI) + "_App"), ApplicationStoreConnectionString)),
                    AppDescReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(blastAppIdentificationURI) + "_Desc"), ApplicationStoreConnectionString)),

                    JobArgs = new ArgumentCollection()
                    {
                            
                            new LiteralArgument
                            {
                                Name = "programName",
                                LiteralValue = "blastx"
                            },
                            new LiteralArgument
                            {
                                Name = "databaseName",
                                LiteralValue = "db"
                            },
                            new SingleReference
                            {
                                Name="inputfile",
                                Ref= new Reference(
                                    "seqfile"+index+".sqf",
                                    new AzureBlobReference
                                    {
                                        DataAddress = computeName("seqfile"+index+".sqf"), 
                                        ConnectionString = UserDataStoreConnectionString,                            
                                    })
                            },
                            new LiteralArgument
                            {
                                Name = "expectationValue",
                                LiteralValue = "1e-10"
                            },
                            new SingleReference
                            {
                                Name="outputfile",
                                Ref= new Reference(
                                    "result"+index+".txt",
                                    new AzureBlobReference
                                    {
                                        DataAddress = computeName("result"+index+".txt"), 
                                        ConnectionString = UserDataStoreConnectionString,                            
                                    })
                            },
                            new LiteralArgument
                            {
                                Name = "numDbSequencesDescription",
                                LiteralValue = "10"
                            },
                            new LiteralArgument
                            {
                                Name = "numDbSequencesAlignment",
                                LiteralValue = "10"
                            }                    
                    }, 
                    Downloads = new ReferenceCollection() //Database files
                    {
                        new Reference(new AzureBlobReference(computeName("db.pin"), UserDataStoreConnectionString)),
                        new Reference(new AzureBlobReference(computeName("db.phr"), UserDataStoreConnectionString)),
                        new Reference(new AzureBlobReference(computeName("db.psq"), UserDataStoreConnectionString))
                    }
                };
                return blastJob;
            };

            //Upload DB Files
            uploadFile("db.phr", "db.phr");
            uploadFile("db.pin", "db.pin");
            uploadFile("db.psq", "db.psq");

            List<VENUSJobDescription> allBLASTJobs = new List<VENUSJobDescription>();
            for (int i = 0; i < numFragments; i++)
                allBLASTJobs.Add(getBLASTJob(i));

            #endregion

            #region Assembler job specifications
            // define Assembler job description
            var assemblerJob = new VENUSJobDescription()
            {
                ApplicationIdentificationURI = assemblerAppIdentificationURI,
                CustomerJobID = "UPVBIO Assembler Job " + DateTime.Now.ToLocalTime().ToString(),
                JobName = "some job name",
                AppPkgReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(assemblerAppIdentificationURI) + "_App"), ApplicationStoreConnectionString)),
                AppDescReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(assemblerAppIdentificationURI) + "_Desc"), ApplicationStoreConnectionString)),
                JobArgs = new ArgumentCollection()
                {
                        new SingleReference
                        {
                            Name="resultFileName",
                            Ref = new Reference(resultFileName, new AzureBlobReference(computeName(resultFileName), UserDataStoreConnectionString)),
                        }
                }
            };
            //add downloads to job
            var downloadsRefCol = new ReferenceCollection();
            for (int i = 0; i < numFragments; i++)
            {
                var resultfileName = string.Format("result{0}.txt", i);
                downloadsRefCol.Add(new Reference(resultfileName, new AzureBlobReference(computeName(resultfileName), UserDataStoreConnectionString)));
            }

            var downloadsRefBlobAssembler = uploadReferenceCollection(downloadsRefCol,false);
            assemblerJob.DownloadsReference = new Reference(new AzureBlobReference(downloadsRefBlobAssembler, UserDataStoreConnectionString));
            //assemblerJob.Downloads = downloadsRefCol;
            #endregion

            #endregion

            #region Submit jobs

            print(ConsoleColor.White, System.Text.Encoding.UTF8.GetString(splitterJob.AsMemoryStream().ToArray()));

            // read certificate configuration
            var authThumprint = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.AuthCert.Thumbprint"];
            var dnsEnpointId = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.AuthCert.DnsEndpointId"];

            // method for creating job management client
            Func<GenericWorkerJobManagementClient> CreateSecureClient = () =>
            {
                var svcUrl = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.SecureGenericWorker.URL"];
                print(ConsoleColor.Red, string.Format("Submitting all jobs to {0}", svcUrl));
                var issuerAddress = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.STS.URL"];
                print(ConsoleColor.Red, string.Format("fetching tokens from {0}", issuerAddress));

                // In the development setting (when talking to localhost), we need to override WCF security checks
                // with the DnsEndpointIdentity.
                //
                
                return GenericWorkerJobManagementClient.CreateSecureClient(
                    address: new EndpointAddress(new Uri(svcUrl), new DnsEndpointIdentity(dnsEnpointId)),
                    issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                    username: "Researcher",
                    password: "secret",
                    serviceCert: X509Helper.GetX509Certificate2(
                        StoreLocation.LocalMachine, StoreName.My,
                        authThumprint,
                        X509FindType.FindByThumbprint));
            };

            Func<GenericWorkerJobManagementClient> CreateUnprotectedClient = () =>
            {
                var svcUrl = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.GenericWorker.URL"];
                print(ConsoleColor.Red, string.Format("Submitting all jobs to {0}", svcUrl));

                return GenericWorkerJobManagementClient.CreateUnprotectedClient(svcUrl);
            };

            var submissionPortal = CreateSecureClient();

            // The order of job submissions does not matter, because the jobs are data driven
            // The BLAST only run when the splitter finished and created sequence files.
            // The assembler only runs when all BLASTs are finished. 
            //

            // submit Blast job
            foreach (var blastJob in allBLASTJobs)
            {
                submissionPortal.SubmitVENUSJob(blastJob);
                print(ConsoleColor.Red, "Blast job submitted...");
            }

            // submit Assembler job
            submissionPortal.SubmitVENUSJob(assemblerJob);
            print(ConsoleColor.Red, "Assembler job submitted...");

            Console.Write("Press enter to continue and submit a splitter job");
            Console.ReadLine();

            // submit Splitter job
            submissionPortal.SubmitVENUSJob(splitterJob);
            print(ConsoleColor.Red, "Splitter job submitted...");

            #endregion

            #region Poll for results

            Task job1ResultPoller = new Task(() => Poll(userDataContainer, computeName(resultFileName), resultFileName,
                ct, TimeSpan.FromSeconds(5)), ct, TaskCreationOptions.AttachedToParent);
            job1ResultPoller.Start();

            #endregion
        }

        enum Choice { No, Yes }

        static Choice AskUser(string question)
        {
            Console.Write(question);

            Predicate<char> isYes = c => c == 'y' || c == 'Y';
            Predicate<char> isNo = c => c == 'n' || c == 'N';
            Predicate<char> isValidChoice = c => isYes(c) || isNo(c);

            char choice = ' ';
            while (!isValidChoice(choice))
                choice = Console.ReadKey().KeyChar;

            Console.WriteLine();
            if (isYes(choice))
                return Choice.Yes;
            else
                return Choice.No;
        }

        private static void Poll(CloudBlobContainer container, string blobName, string filename, CancellationToken cts, TimeSpan interval)
        {
            // rechecks every interval seconds if the output file is created
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
}