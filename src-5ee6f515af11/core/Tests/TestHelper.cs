//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using System.Xml;
using Ionic.Zip;
using Microsoft.EMIC.Cloud.ApplicationRepository;
using Microsoft.EMIC.Cloud.DataManagement;
using Microsoft.EMIC.Cloud.Demo;
using Microsoft.EMIC.Cloud.GenericWorker;
using Microsoft.EMIC.Cloud.GenericWorker.AzureAccounting;
using Microsoft.EMIC.Cloud.GenericWorker.AzureProvider;
using Microsoft.EMIC.Cloud.Storage.Azure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using OGF.BES;
using OGF.JDSL;
using System.Threading;
using Microsoft.EMIC.Cloud;

namespace Tests
{
    public class TestHelper
    {
        public static void FlushTable(CompositionContainer container)
        {
            #region Delete all table data
            for (var retries = 0; retries < 30; retries++)
            {
                try
                {
                    
                    CloudStorageAccount cs = CloudStorageAccount.Parse(CloudSettings.TestCloudConnectionString);

                    var c1 = new CurrentJobTableDataContext(cs.TableEndpoint.AbsoluteUri, cs.Credentials,
                        container.GetExportedValue<string>("Microsoft.EMIC.Cloud.Development.GenericWorker.IndexTableName"));
                    c1.CurrentJobs.ToList().ForEach(x => { c1.DeleteObject(x); c1.SaveChangesWithRetries(); });

                    var c2 = new JobTableDataContext(cs.TableEndpoint.AbsoluteUri, cs.Credentials,
                        container.GetExportedValue<string>("Microsoft.EMIC.Cloud.Development.GenericWorker.DetailsTableName"));
                    c2.AllJobs.ToList().ForEach(x => { c2.DeleteObject(x); c2.SaveChangesWithRetries(); });

                    var c3 = new AccountingTableDataContext(cs.TableEndpoint.AbsoluteUri, cs.Credentials,
                      container.GetExportedValue<string>("Microsoft.EMIC.Cloud.Development.GenericWorker.AccountingTableName"));
                    c3.AllAccountingInfo.ToList().ForEach(x => { c3.DeleteObject(x); c3.SaveChangesWithRetries(); });
                    break;
                }
                catch(Exception)
                {
                    System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(500));                    
                }
            }
            #endregion
        }

        public static void FlushBlobContainer(CompositionContainer container)
        {
            var devBlobContainerName = container.GetExportedValue<string>(CompositionIdentifiers.DevelopmentGenericWorkerBlobName);
            CloudStorageAccount cs = CloudStorageAccount.Parse(CloudSettings.TestCloudConnectionString);
            var blobClient = cs.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference(devBlobContainerName);
            var blobs = blobContainer.ListBlobs().Where(b => !b.Uri.LocalPath.Split('/')[2].StartsWith("http"));
            foreach (var blob in blobs)
            {
                blobClient.GetBlobReference(blob.Uri.AbsoluteUri).DeleteIfExists();
            }
        }

        public static VENUSJobDescription CreateJSDL
        {
            get
            {
                return new VENUSJobDescription()
                {
                    ApplicationIdentificationURI = "http://www.microsoft.com/someapp",
                    CustomerJobID = "Some customer-selected job ID 123",
                    JobName = "Invoke to show at Aachen meeting"
                };
            }
        }

        public static VENUSJobDescription CreateHierarchicalJSDL(String customerJobID)
        {
            return new VENUSJobDescription()
            {
                ApplicationIdentificationURI = "http://www.microsoft.com/someapp",
                CustomerJobID = customerJobID,
                JobName = "Invoke to show at Aachen meeting"
            };
        }

        public static VENUSJobDescription CreateGroupJSDL(string groupName, string jobName)
        {
            return new VENUSJobDescription()
            {
                ApplicationIdentificationURI = "http://www.microsoft.com/someapp",
                CustomerJobID = string.Format("GroupID://{0}/{1}",groupName,jobName),
                JobName = "Invoke to show at Aachen meeting"
            };
        }

        public static void UploadMathApplication(out string applicationIdentificationURI, out Reference appReference, out Reference descReference, out VENUSApplicationDescription appDesc, CloudBlobContainer userDataContainer)
        {
            applicationIdentificationURI = "http://www.microsoft.com/emic/cloud/demo/SimpleMathApp";
            appDesc = new VENUSApplicationDescription()
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

            #endregion

            appReference = new Reference(new AzureBlobReference(appBlob, CloudSettings.UserDataStoreConnectionString));
            descReference = new Reference(new AzureBlobReference(appDescBlob, CloudSettings.UserDataStoreConnectionString));
        }

        public static void PollForOutputFiles(VENUSApplicationDescription appDesc, List<VENUSJobDescription> jobs, TimeSpan pollDuration)
        {
            var OutRefs = jobs.SelectMany(j => j.JobArgs).OfType<SingleReference>().Where(sr => sr.IsOutput(appDesc));
            DateTime startTime = DateTime.Now;
            var pollIntervall = (int)Math.Min(pollDuration.TotalMilliseconds, 500);
            while (!OutRefs.All(or => or.ExistsDataItem()))
            {
                if (DateTime.Now - startTime > pollDuration)
                {
                    throw new TimeoutException("resultfile not seen");
                }
                Thread.Sleep(pollIntervall);
            }
        }

        public static VENUSJobDescription GetMathJob(Func<string, Reference> refBuilderForSomeStorage, string inputFileName, string resultFileName, string applicationIdentificationURI, Reference appReference, Reference descReference)
        {
            Func<Func<string, Reference>, string, string, VENUSJobDescription> GetMathJob_f = (refBuilder, inputfileName, resultfileName) =>
            {
                var job = new VENUSJobDescription()
                {
                    ApplicationIdentificationURI = applicationIdentificationURI,
                    CustomerJobID = "EndToEndTest job ID 124",
                    AppPkgReference = appReference,
                    AppDescReference = descReference,
                    JobName = "Invoke to show at Aachen meeting",
                    JobArgs = new ArgumentCollection()
                    {
                         new SingleReference{  
                             Name = "InputFile", 
                             Ref = refBuilder(inputfileName)
                         },
                         new SingleReference
                         { 
                             Name = "OutputFile", 
                             Ref = refBuilder(resultfileName)
                         },
                         new SwitchArgument 
                         { 
                             Name = "Operation", 
                             Value = true 
                         }
                    }
                };
                return job;
            };
            return GetMathJob_f(refBuilderForSomeStorage, inputFileName, resultFileName);
        }

        public static void WaitForBlob(string blobName, CloudBlobContainer userDataContainer)
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

        public static string upload(string blobName, string blobContent, CloudBlobContainer userDataContainer)
        {
            var blob = userDataContainer.GetBlobReference(blobName);
            blob.UploadText(blobContent);
            var blobAddress = blob.Uri.AbsoluteUri;
            return blobAddress;
        }


        public static string computeNameAndDeleteIfExist(string blobname, CloudBlobContainer userDataContainer)
        {
            var r = userDataContainer.GetBlobReference(blobname);
            r.DeleteIfExists();
            return r.Uri.AbsoluteUri;
        }
        public static void CompareWithSampleJob(JobDefinition_Type jobDefinition)
        {
            var sampleJob = CreateJSDL;
            Assert.AreEqual(sampleJob.ApplicationIdentificationURI, jobDefinition.JobDescription.Application.ApplicationName);
            Assert.AreEqual(sampleJob.CustomerJobID, jobDefinition.id);
            Assert.AreEqual(sampleJob.JobName, jobDefinition.JobDescription.JobIdentification.JobName);
        }

        public static Action<JobStatus> CreateAssertStatus(string id, CompositionContainer container)
        {
            var rt = new TwoRuntimes(container);

            return (status) =>
            {
                var j = rt.RT0.GetJobStatus(id);
                Assert.IsNotNull(j, "j");
                Assert.AreEqual<JobStatus>(status, j.Value);
                Console.WriteLine(status);
            };
        }

        public static bool ChangeToStatus(IGWRuntimeEnvironment runtime, IJob job, JobStatus finalStatus)
        {
             switch ( finalStatus)
             {
                case JobStatus.Submitted:
                     return runtime.MarkJobAsSubmittedBack(job, "");
                case JobStatus.CheckingInputData:
                    return runtime.MarkJobAsChekingInputData(job, runtime.GetHashCode().ToString());
                case JobStatus.Running:
                    return runtime.MarkJobAsRunning(job, "", "", "");
                case JobStatus.CancelRequested:
                    return runtime.MarkJobAsCancellationPending(job);
                case JobStatus.Failed:
                    return runtime.MarkJobAsFailed(job,"");
                case JobStatus.Finished:
                    return runtime.MarkJobAsFinished(job, "");
                case JobStatus.Cancelled:
                    return runtime.MarkJobAsCancelled(job, "");
            }
            return false;
        }

        public static bool AdvanceNewJobToStatus(IGWRuntimeEnvironment runtime, IJob job, JobStatus finalStatus)
        {
            if (finalStatus == JobStatus.Submitted)
            {
                return true;
            }
            var states = new List<JobStatus>[] {    new List<JobStatus>{JobStatus.CheckingInputData, JobStatus.Cancelled }, 
                                                    new List<JobStatus>{JobStatus.Running},
                                                    new List<JobStatus>{JobStatus.CancelRequested, JobStatus.Failed, JobStatus.Finished}
                                                };
            int level = 0;
            while (level<states.Length && !states[level].Contains(finalStatus))
            {
                ChangeToStatus(runtime,job,states[level][0]);
                level++;
            }
            return ChangeToStatus(runtime, job, finalStatus);
        }

        internal static string GetIdFromEndPointReference(EndpointReferenceType endpoint)
        {
            string result = "";

            try
            {
                result = endpoint.ReferenceParameters.Any[0].InnerText;
            }
            catch
            {
            }

            return result;
        }

        public static EndpointReferenceType GetEpr(IJob job)
        {
            var jobIdAsEpr = new JobIdAsEPR(job.InternalJobID);
            var epr = new EndpointReferenceType();
            epr.Address = new AttributedURIType() 
            { 
                Value = CloudSettings.GenericWorkerUrl 
            };
            epr.ReferenceParameters = new ReferenceParametersType
            {
                Any = new List<XmlElement> { jobIdAsEpr.AsXmlElement() }
            };
            return epr;
        }

        public static IJob GetIJob(EndpointReferenceType endPoint, IGWRuntimeEnvironment runtimeEnvironment)
        {
            if (endPoint.Address != null)
            {
                var jobIdAsEPR = JobIdAsEPR.Load(endPoint, endPoint.Address.Value, runtimeEnvironment);
                if (jobIdAsEPR == null) throw new ArgumentNullException();

                return runtimeEnvironment.GetJobByID(jobIdAsEPR.Value);
            }
            else
            {
                return null;
            }
        }

        public static void PollForCondition(Func<bool> pred, TimeSpan pollinterval, TimeSpan pollduration)
        {
            ActAndPollForCondition(() => { }, pred, pollinterval, pollduration);
        }

        public static void ActAndPollForCondition(Action act, Func<bool> pred, TimeSpan pollinterval, TimeSpan pollduration)
        {
            if (pollinterval > pollduration)
            {
                throw new ArgumentException("pollduration should be longer than the pollinterval");
            }
            if (pollinterval.Ticks <= 0)
            {
                throw new ArgumentException("pollinterval should be a postive value");
            }
            DateTime startTime = DateTime.Now;
            while (DateTime.Now - startTime < pollduration)
            {
                act();
                if (pred()) return;
                System.Threading.Thread.Sleep(pollinterval);
            }
        }

        public static IJob GetRoot(IGWRuntimeEnvironment worker1, string rootJobId, IJob rootJob)
        {
            var status = worker1.GetJobStatus(rootJob.InternalJobID);
            if (status != JobStatus.Submitted)
            {
                Assert.IsTrue(worker1.MarkJobAsSubmittedBack(rootJob, "re-submitted"));
            }
            IJob ijob;
            worker1.TryDequeueJob(out ijob);
            Assert.IsNotNull(ijob);
            Assert.AreEqual(ijob.InternalJobID, rootJobId);
            Assert.AreEqual(ijob.Status, JobStatus.Submitted);
            Assert.IsTrue(worker1.MarkJobAsChekingInputData(rootJob, "instance1"));
            Assert.IsTrue(worker1.MarkJobAsRunning(rootJob, "status", "stdout", "stderr"));
            return ijob;
        }

        public static IJob SubmitChildren(IGWRuntimeEnvironment worker1, string customerJobID)
        {
            IJob testChild = worker1.SubmitJob("John Doe", Guid.NewGuid().ToString(), TestHelper.CreateHierarchicalJSDL(customerJobID), true);
            string testid = testChild.InternalJobID;
            worker1.TryDequeueJob(out testChild);
            Assert.IsNotNull(testChild);
            Assert.AreEqual(testChild.InternalJobID, testid);
            Assert.IsTrue(worker1.MarkJobAsChekingInputData(testChild, "instance1"));
            Assert.IsTrue(worker1.MarkJobAsRunning(testChild, "status", "stdout", "stderr"));
            return testChild;
        }
    }

    public class TwoRuntimes
    {
        public TwoRuntimes(CompositionContainer container)
        {
            container.SatisfyImportsOnce(this);
        }

        [Import(RequiredCreationPolicy = CreationPolicy.NonShared)]
        public IGWRuntimeEnvironment RT0;

        [Import(RequiredCreationPolicy = CreationPolicy.NonShared)]
        public IGWRuntimeEnvironment RT1;

        [Import(RequiredCreationPolicy = CreationPolicy.NonShared)]
        public IGWRuntimeEnvironment RT2;
    }

    public static class StopWatchExtensions
    {
        public static StopWatch Stop(this string info)
        {
            return new StopWatch(info);
        }
    }

    public class StopWatch : IDisposable
    {
        DateTime start;
        string info;

        public StopWatch(string info)
        {
            this.start = DateTime.UtcNow;
            this.info = info;
        }

        #region IDisposable Members

        private bool alreadyDisposed = false;

        private object m_lock = new object();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (alreadyDisposed)
                return;

            Trace.TraceInformation(
                string.Format("StopWatch: {1} for \"{0:00000}\"",
                    this.info, (long)DateTime.UtcNow.Subtract(this.start).TotalMilliseconds));

            alreadyDisposed = true;
        }

        #endregion
    }
}
