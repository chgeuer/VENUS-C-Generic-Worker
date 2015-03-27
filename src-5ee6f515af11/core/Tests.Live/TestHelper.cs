//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Microsoft.EMIC.Cloud.GenericWorker.AzureProvider;
using Microsoft.EMIC.Cloud.GenericWorker;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

using OGF.BES;
using OGF.JDSL;
using Microsoft.EMIC.Cloud.GenericWorker.AzureAccounting;
using System.Collections.Generic;
using System.Xml;
using Microsoft.EMIC.Cloud.LiveDemo;
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
                    CloudStorageAccount cs = CloudStorageAccount.Parse(LiveDemoCloudSettings.LiveTestsCloudConnectionString);

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
            CloudStorageAccount cs = CloudStorageAccount.Parse(LiveDemoCloudSettings.LiveTestsCloudConnectionString);
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
            epr.Address = new AttributedURIType
            {
                Value = LiveDemoCloudSettings.GenericWorkerUrl
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