//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using KTH.GenericWorker.CDMI;
using Microsoft.EMIC.Cloud.GenericWorker;
using Microsoft.EMIC.Cloud.DataManagement;
using Microsoft.EMIC.Cloud.Storage.Azure;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.Web;

namespace UPVBioClient
{
    public class UPVBioClientCDMIProgram
    {
        static void Main(string[] Args)
        {
            Console.Title = "UPVBio in Generic Worker and CDMI End-to-End Client";

            const string inputFile = "input_file.fasta";
            const string resultFileName = "assembledResult";
            const int numFragments = 3;

            var userDataStoreConnectionString = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.Demo.User.ConnectionString"];
            var account = CloudStorageAccount.Parse(userDataStoreConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer userDataContainer = blobClient.GetContainerReference("userdatacontainer" + DateTime.Now.ToString("yyyyMMddhhmm"));
            userDataContainer.CreateIfNotExist();
            CloudBlobContainer appDataContainer = blobClient.GetContainerReference("applicationcontainer");
            appDataContainer.CreateIfNotExist();

            try
            {
                var cts = new CancellationTokenSource();
                RunJobAndPollForResult(inputFile, resultFileName, numFragments, cts,
                    userDataContainer, appDataContainer, userDataStoreConnectionString);

                var JobManagementUrl = ConfigurationManager.AppSettings["JobManagementUrl"];
                // Process.Start(JobManagementUrl);

                if (AskUser("Polling for job result now, should we stop? ") == Choice.Yes)
                {
                    cts.Cancel();
                }
            }
            finally
            {
                Console.WriteLine("Deleting container " + userDataContainer.Uri.AbsoluteUri);
                userDataContainer.Delete();
            }
        }

        private static void RunJobAndPollForResult(string inputFile, string resultFileName, int numFragments, CancellationTokenSource cts, CloudBlobContainer userDataContainer, CloudBlobContainer appDataContainer, string UserDataStoreConnectionString)
        {
            Action<ConsoleColor, string> print = (color, str) =>
            {
                Console.ForegroundColor = color;
                Console.Out.WriteLine(str);
                Console.ResetColor();
            };

            var GenericWorkerUrl = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.GenericWorker.URL"];

            string upvBioURIPrefix = "http://www.upvbio.eu/cloud/demo/gw/UPVBIOApp/";
            string splitterAppIdentificationURI = upvBioURIPrefix + "Splitter";
            string blastAppIdentificationURI = upvBioURIPrefix + "Blast";
            string assemblerAppIdentificationURI = upvBioURIPrefix + "Assembler";

            #region Upload user data to CDMI service

            bool useSecureBinding = false;
            //var cdmiAddress = useSecureBinding ? "https://cdmi.pdc2.pdc.kth.se:8080" : "http://cdmi.pdc2.pdc.kth.se:2364";
            //var username = "christian";
            //var password = "venusc";

            var cdmiAddress = useSecureBinding ? "https://emicloudbuild:8080" : "http://emicloudbuild:2365";
            var username = "user";
            var password = "cdmipass";

            var jobId = DateTime.UtcNow.ToString("yyyy-MM-dd--hh-mm-ss-fff");
            var inputUri = string.Format("{0}/{1}-seqfileAnewOne.sqf", cdmiAddress, jobId);
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


            var inputReference = new Reference(inputFile, cdmiRef);
            var dbPhrReference = new Reference("db.phr", cdmiRef);
            var dbPinReference = new Reference("db.pin", cdmiRef);
            var dbPsqReference = new Reference("db.psq", cdmiRef);
            
            AskUser(string.Format("Upload user data to {0}? ", inputUri));
            inputReference.Upload(".");
            dbPhrReference.Upload(".");
            dbPinReference.Upload(".");
            dbPsqReference.Upload(".");

            Console.WriteLine("Uploaded input file to {0}", ((CDMIBlobReference)inputReference.ProviderSpecificReference).URI);
            var cdmiRefResult= new CDMIBlobReference()
            {
                URI = string.Format("{0}/{1}-result.txt", cdmiAddress, jobId),
                Credentials = new NetworkCredential(username, password),
                RequestFactory = (url) =>
                {
                    var request = (HttpWebRequest)WebRequest.Create(url);
                    return request;
                }
            };
            var resultReference = new Reference(resultFileName,cdmiRefResult);

            Func<string, string> computeTempFileName = (name) => userDataContainer.GetBlobReference(name).Uri.AbsoluteUri;
            Func<string, Reference> AzureRef = (filename) => new Reference(filename, new AzureBlobReference(computeTempFileName(filename), UserDataStoreConnectionString));
            Func<int, Reference> TemporarySequenceFile = (i) => AzureRef("seqfile" + i + ".sqf");
            Func<int, Reference> ResultFile = (i) => AzureRef("result" + i + ".sqf");

            #endregion

            #region Define jobs

            var splitterJob = new VENUSJobDescription()
            {
                ApplicationIdentificationURI = splitterAppIdentificationURI,
                CustomerJobID = "UPVBIO Splitter Job " + DateTime.Now.ToLocalTime().ToString(),
                JobName = "some job name",
                AppPkgReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(splitterAppIdentificationURI) + "_App"), UserDataStoreConnectionString)),
                AppDescReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(splitterAppIdentificationURI) + "_Desc"), UserDataStoreConnectionString)),
                JobArgs = new ArgumentCollection()
                {
                        new SingleReference
                        {
                            Name="inputfile",
                            Ref = inputReference
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
                        },
                        //new ReferenceArray
                        //{
                        //    Name = "fragmentfiles",
                        //    References = new ReferenceCollection(Enumerable.Range(0, numFragments).Select(i => TemporarySequenceFile(i)))
                        //}
                }
                ,Uploads = new ReferenceCollection(Enumerable.Range(0, numFragments).Select(i => TemporarySequenceFile(i)))
            };

            var allAlignerJobs = Enumerable.Range(0, numFragments).Select(index => new VENUSJobDescription()
            {
                    ApplicationIdentificationURI = blastAppIdentificationURI,
                    CustomerJobID = "UPVBIO BLAST Job-" + index + "-" + DateTime.Now.ToLocalTime().ToString(),
                    JobName = "some job name",
                    AppPkgReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(blastAppIdentificationURI) + "_App"), UserDataStoreConnectionString)),
                    AppDescReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(blastAppIdentificationURI) + "_Desc"), UserDataStoreConnectionString)),

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
                                Ref= TemporarySequenceFile(index)
                            },
                            new LiteralArgument
                            {
                                Name = "expectationValue",
                                LiteralValue = "1e-10"
                            },
                            new SingleReference
                            {
                                Name="outputfile",
                                Ref= ResultFile(index)
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
                        new Reference(new AzureBlobReference(computeTempFileName("db.pin"), UserDataStoreConnectionString)),
                        new Reference(new AzureBlobReference(computeTempFileName("db.phr"), UserDataStoreConnectionString)),
                        new Reference(new AzureBlobReference(computeTempFileName("db.psq"), UserDataStoreConnectionString))
                    }
            });

            var assemblerJob = new VENUSJobDescription()
            {
                ApplicationIdentificationURI = assemblerAppIdentificationURI,
                CustomerJobID = "UPVBIO Assembler Job " + DateTime.Now.ToLocalTime().ToString(),
                JobName = "some job name",
                AppPkgReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(assemblerAppIdentificationURI) + "_App"), UserDataStoreConnectionString)),
                AppDescReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(assemblerAppIdentificationURI) + "_Desc"), UserDataStoreConnectionString)),
                JobArgs = new ArgumentCollection()
                {
                        new SingleReference
                        {
                            Name="resultFileName",
                            Ref = resultReference,
                        },
                        new ReferenceArray
                        {
                            Name = "fragmentfiles",
                            References = new ReferenceCollection(Enumerable.Range(0, numFragments).Select(i => ResultFile(i)))
                        }
                }
            };

            #endregion

            #region Submit jobs

            AskUser("Create jobs");

            print(ConsoleColor.Red, string.Format("Submitting all jobs to {0}", GenericWorkerUrl));
            print(ConsoleColor.White, splitterJob.Printable());
            print(ConsoleColor.Yellow, allAlignerJobs.First().Printable());
            print(ConsoleColor.White, assemblerJob.Printable());

            var submissionPortal = GenericWorkerJobManagementClient.CreateUnprotectedClient(GenericWorkerUrl);

            // The order of job submissions does not matter, because the jobs are data driven
            // The aligners only run when the splitter finished and created sequence files.
            // The assembler only runs when all aligners finished. 

            foreach (var alignerJob in allAlignerJobs)
            {
                AskUser("Submit aligner? ");
                submissionPortal.SubmitVENUSJob(alignerJob);
                print(ConsoleColor.Red, "Aligner job submitted...");
            }

            AskUser("Submit assembler? ");
            submissionPortal.SubmitVENUSJob(assemblerJob);
            print(ConsoleColor.Red, "Assembler job submitted...");

            AskUser("Submit splitter? ");
            submissionPortal.SubmitVENUSJob(splitterJob);
            print(ConsoleColor.Red, "Splitter job submitted...");

            #endregion

            #region Poll for results

            Task job1ResultPoller = new Task(() => Poll(resultReference, cts, TimeSpan.FromSeconds(5)), cts.Token, TaskCreationOptions.AttachedToParent);
            job1ResultPoller.Start();

            #endregion
        }

        enum Choice { No, Yes }

        static Choice AskUser(string question)
        {
            Console.Write(question + "(y/n) ");

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

        private static void Poll(Reference reference, CancellationTokenSource cts, TimeSpan interval)
        {
            while (!cts.IsCancellationRequested)
            {
                if (!reference.ExistsDataItem())
                {
                    Thread.Sleep(interval);
                    continue;
                }
                
                reference.Download(".", cts);
                Console.WriteLine("Downloaded result from {0}", ((CDMIBlobReference)reference.ProviderSpecificReference).URI);
                Console.WriteLine("Successfully downloaded result of job {0}", new FileInfo(reference.LocalFileName).FullName);
    
                return;
            }
        }
    }

    public static class LocalExtensions
    {
        public static string Printable(this Reference reference)
        {
            return reference.Serialize(new XmlDocument()).OuterXml;
        }

        public static string Printable(this VENUSJobDescription job)
        {
            return System.Text.Encoding.UTF8.GetString(job.AsMemoryStream().ToArray());
        }
    }
}