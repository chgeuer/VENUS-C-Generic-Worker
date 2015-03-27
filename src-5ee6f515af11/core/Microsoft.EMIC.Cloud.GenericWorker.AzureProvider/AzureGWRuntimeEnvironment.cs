//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.Services.Client;
using System.Diagnostics;
using System.Linq;
using Microsoft.EMIC.Cloud.DataManagement;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.Xml.Serialization;
using System.Net.NetworkInformation;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Threading;
using Microsoft.EMIC.Cloud.Notification;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Globalization;
using System.Text;
using Microsoft.EMIC.Cloud.Storage.Azure;

namespace Microsoft.EMIC.Cloud.GenericWorker.AzureProvider
{
    /// <summary>
    /// Enables the <see cref="GenericWorkerDriver"/> to use Windows Azure storage for coordination. 
    /// </summary>
    [Export(typeof(IGWRuntimeEnvironment))]
    public class AzureGWRuntimeEnvironment : IGWRuntimeEnvironment, IPartImportsSatisfiedNotification
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureGWRuntimeEnvironment"/> class.
        /// </summary>
        public AzureGWRuntimeEnvironment() 
        {
            //Mutex myMutex;
            try
            {
                // As the mutex might already be created by another thread / process,
                // try to open the existing mutex.
                _TableWriteLock = Mutex.OpenExisting(TABLEWRITELOCK_MUTEX_NAME);
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                // The Mutex doesn't exist

                // The mutex must be accessable from all processes on the system.
                // Therefore it is very important to set the right access level.
                SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                MutexSecurity mSec = new MutexSecurity();
                MutexAccessRule rule = new MutexAccessRule(sid, MutexRights.FullControl, AccessControlType.Allow);
                mSec.AddAccessRule(rule);
                bool mutexIsNew = false;

                // Now the mutex can be created with correct access rights.
                _TableWriteLock = new Mutex(false, TABLEWRITELOCK_MUTEX_NAME, out mutexIsNew, mSec);
            }
        }

        /// <summary>
        /// Gets or sets the generic worker cloud connection string.
        /// </summary>
        /// <value>
        /// The generic worker cloud connection string.
        /// </value>
        [Import(CompositionIdentifiers.GenericWorkerConnectionString)]
        public string GenericWorkerCloudConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the generic worker job BLOB store.
        /// </summary>
        /// <value>
        /// The generic worker job BLOB store.
        /// </value>
        [Import(CompositionIdentifiers.DevelopmentGenericWorkerBlobName)]
        public string GenericWorkerJobBlobStore { get; set; }
        
        /// <summary>
        /// Gets or sets the generic worker subscription blob storage.
        /// </summary>
        /// <value>
        /// The generic worker subscription BLOB store.
        /// </value>
        [Import(CompositionIdentifiers.DevelopmentGenericWorkerSubscriptionsBlobName)]
        public string GenericWorkerSubscriptionBlobStore { get; set; }

        /// <summary>
        /// Gets or sets the name of the generic worker details table.
        /// </summary>
        /// <value>
        /// The name of the generic worker details table.
        /// </value>
        [Import(CompositionIdentifiers.DevelopmentGenericWorkerDetailsTableName)]
        public string GenericWorkerDetailsTableName { get; set; }

        /// <summary>
        /// Gets or sets the name of the generic worker index table.
        /// </summary>
        /// <value>
        /// The name of the generic worker index table.
        /// </value>
        [Import(CompositionIdentifiers.DevelopmentGenericWorkerIndexTableName)]
        public string GenericWorkerIndexTableName { get; set; }

        /// <summary>
        /// Gets or sets the EventDispatcher of the Generic Worker
        /// </summary>
        /// <value>
        /// GW Event Dispatcher dispatches incoming usage events to the imported listeners
        /// </value>
        [Import]
        public GenericWorkerEventDispatcher GWEventDispatcher { get; set; }

        /// <summary>
        /// Gets or sets the name of the generic worker hygiene table.
        /// </summary>
        /// <value>
        /// The name of the generic worker hygiene table.
        /// </value>
        [Import(CompositionIdentifiers.DevelopmentGenericWorkerHygieneTableName)]
        public string GenericWorkerHygieneTableName { get; set; }

        [Import]
        ArgumentRepository ArgumentRepository { get; set; }

        private static Mutex _TableWriteLock;
        private const string TABLEWRITELOCK_MUTEX_NAME = "GW_TableWriteMutex";
        private const int LIMIT_SIZE_TABLE_COLUMN = 65000; //Todo: Rename this and make it configurable
        private const int MUTEX_TIMEOUT_MS = 50000;

        private CloudStorageAccount _account;
        
        private CloudBlobClient _blobClient;
        
        private CloudBlobContainer _blobContainer;

        private CloudBlobContainer _notificationSubscriptionsContainer;

        private JobTableDataContext GetAll() 
        {
            return new JobTableDataContext(
               this._account.TableEndpoint.AbsoluteUri,
               this._account.Credentials, this.GenericWorkerDetailsTableName);
        }
        
        private CurrentJobTableDataContext GetCurrent() 
        {
            return new CurrentJobTableDataContext(
                this._account.TableEndpoint.AbsoluteUri,
                this._account.Credentials, this.GenericWorkerIndexTableName);
        }

        private string lastDequeuedJobInternalID;
        

        void IPartImportsSatisfiedNotification.OnImportsSatisfied()
        {
            this._account = CloudStorageAccount.Parse(this.GenericWorkerCloudConnectionString);
            var tc = this._account.CreateCloudTableClient();

            #region Tables

            for (var retries = 0; retries < 3; retries++)
            {
                try
                {
                    if (!tc.DoesTableExist(this.GenericWorkerDetailsTableName))
                    {
                        tc.CreateTableIfNotExist(this.GenericWorkerDetailsTableName);
                    }

                    if (!tc.DoesTableExist(this.GenericWorkerIndexTableName))
                    {
                        tc.CreateTableIfNotExist(this.GenericWorkerIndexTableName);
                    }

                    if (!tc.DoesTableExist(this.GenericWorkerHygieneTableName))
                    {
                        tc.CreateTableIfNotExist(this.GenericWorkerHygieneTableName);
                    }
                    break;
                }
                catch (StorageClientException)
                {
                    System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(200));
                }
            }

            #endregion

            this._blobClient = _account.CreateCloudBlobClient();
            this._blobContainer = _blobClient.GetContainerReference(this.GenericWorkerJobBlobStore);
            this._blobContainer.CreateIfNotExist();
            BlobContainerPermissions resultPermissions = _blobContainer.GetPermissions();
            resultPermissions.PublicAccess = BlobContainerPublicAccessType.Off;
            this._blobContainer.SetPermissions(resultPermissions);

            this._notificationSubscriptionsContainer = _blobClient.GetContainerReference(this.GenericWorkerSubscriptionBlobStore); 
            this._notificationSubscriptionsContainer.CreateIfNotExist();
        }

        #region IGWRuntimeEnvironment Members
        
        private static Func<CurrentJobTableEntity, bool> ExactlyIndex2(string owner, string internalJobID)
        {
            return e => (e.PartitionKey == owner && e.RowKey == internalJobID);
        }
        
        IJob IGWRuntimeEnvironment.SubmitJob(string owner, string internalJobID, VENUSJobDescription venusJobDescription, bool isLocalJob)
        {
            var cj = this.GetCurrent();
            var aj = this.GetAll();

            if (aj.AllJobs.Where(e => e.PartitionKey == owner && e.RowKey == internalJobID).FirstOrDefault() != null)
            {
                throw new ArgumentException(ExceptionMessages.JobExists);
            }

            var jte = new JobTableEntity(owner, internalJobID)
            {
                CustomerJobID = venusJobDescription.CustomerJobID,
                ApplicationIdentificationURI = venusJobDescription.ApplicationIdentificationURI,
                Submission = DateTime.UtcNow,
                LastChange = DateTime.UtcNow
            };

            if (isLocalJob)
            {
                if (AcceptLocalJob(jte.CustomerJobID))
                {
                    jte.ParentGUID = lastDequeuedJobInternalID;

                    // The LocalJobSubmissionEndpoint is not able to find out the owner of the
                    // current running job. But the owner of the new job has to be the same owner as of the
                    // current running job. So the parameter "owner" has to be replaced.
                    AzureJob parentJob = this.GetJobByID(lastDequeuedJobInternalID) as AzureJob;
                    owner = parentJob.Owner;
                    jte.PartitionKey = owner;

                    var parentJobEntity = aj.AllJobs.Where(e => e.PartitionKey == owner && e.RowKey == lastDequeuedJobInternalID).FirstOrDefault();
                    if (parentJobEntity == null)
                    {
                        throw new ArgumentException(ExceptionMessages.ParentJobNotFound);
                    }
                    var parentAzureJob = this.GetJobByID(parentJobEntity.RowKey) as AzureJob;
                    parentAzureJob.AddChild(internalJobID);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                string groupName, jobName;
                if (venusJobDescription.CustomerJobID != null && JobID.TryParseGroup(venusJobDescription.CustomerJobID, out groupName, out jobName))
                {
                    var groupEntity = aj.AllJobs.Where(e => e.PartitionKey == owner && e.RowKey == groupName).FirstOrDefault();
                    if (groupEntity == null)
                    {
                        //this is the first job of this group, so we have to create the groupentity (owner,groupNmae,childs(blobURI))
                        var groupjte = new JobTableEntity(owner, groupName)
                        {
                            CustomerJobID = string.Format("GroupID://{0}", groupName),
                            Status = ""
                        };
                        jte.ParentGUID = groupName;
                        aj.AddJob(groupjte);
                        aj.SaveChangesWithRetries();
                        var parentAzureJob = this.GetJobByID(groupjte.RowKey) as AzureJob;
                        parentAzureJob.AddChild(internalJobID);
                    }
                    else
                    {
                        //groupNode just needs to store an additional child
                        var parentAzureJob = this.GetJobByID(groupEntity.RowKey) as AzureJob;
                        parentAzureJob.AddChild(internalJobID);

                        jte.ParentGUID = groupName;
                    }
                }
            }

            // The constructor also uploads the VENUSJobDescription into blob store...
            var job = AzureJob.Create(this._blobContainer, this.ArgumentRepository,
                jte, venusJobDescription);

            aj.AddJob(jte);
            aj.SaveChangesWithRetries();

            var cjte = new CurrentJobTableEntity(owner, internalJobID);
            cjte.SetPrio(venusJobDescription.JobPrio);
            cj.AddJobIndex(cjte);
            cj.SaveChangesWithRetries();

            return job;

        }

        /// <summary>
        /// Retrieves a job by ID.
        /// </summary>
        /// <param name="internalJobID">The internal job ID.</param>
        /// <returns></returns>
        public IJob GetJobByID(string internalJobID)
        {
            var ajs = this.GetAll();
            var aj = ajs.AllJobs
                .Where(e => e.RowKey == internalJobID)
                .FirstOrDefault();

            if (aj == null)
                return null;

            return new AzureJob(this._blobContainer, this.ArgumentRepository, aj);
        }

        /// <summary>
        /// Gets the list of jobs for a given owner and groupname.
        /// </summary>
        /// <param name="owner">Owner of the jobs</param>
        /// <param name="groupName">Name of the group</param>
        /// <returns></returns>
        public List<IJob> GetJobGroupByOwnerGroupName(string owner, string groupName)
        {           
            var jobs = new List<IJob>();
            var aj = this.GetAll();
            var groupEntity = aj.AllJobs.Where(e => e.PartitionKey == owner && e.RowKey == groupName).FirstOrDefault();
            if (groupEntity != null)
            {
                var parentAzureJob = this.GetJobByID(groupEntity.RowKey) as AzureJob;
                parentAzureJob.GetChildren().ForEach(child => jobs.Add(this.GetJobByID(child)));
            }
            return jobs;
        }
        
        /// <summary>
        /// Get the list of the jobs which are in the same group with the given job
        /// </summary>
        /// <param name="job">One of the jobs which is in the group</param>
        /// <returns></returns>
        public List<IJob> GetJobGroupByJob(IJob job)
        {
            var jobs = new List<IJob>();
            string groupName,jobName;
            if (JobID.TryParseGroup(job.CustomerJobID, out groupName, out jobName))
            {
                jobs = GetJobGroupByOwnerGroupName(job.Owner, groupName);
            }
            return jobs;
        }

        /// <summary>
        /// Get the dummy header job of the group 
        /// </summary>
        /// <param name="owner">Owner of the group</param>
        /// <param name="groupName">Name of the group</param>
        /// <returns></returns>
        public IJob GetGroupHeadByGroupName(string owner, string groupName)
        {
            var ajs = this.GetAll();
            var groupHead = ajs.AllJobs.Where(e => e.PartitionKey == owner && e.RowKey == groupName).FirstOrDefault();
            if (groupHead == null)
            {
                throw new ArgumentException(ExceptionMessages.GroupHeadNotFound);
            }
            return this.GetJobByID(groupHead.RowKey);
        }

        /// <summary>
        /// Get the dummy header job of the group 
        /// </summary>
        /// <param name="job">A job which is in the targeted group</param>
        /// <returns></returns>
        public IJob GetGroupHeadByJob(IJob job)
        {
            string groupName, jobName;
            if (!JobID.TryParseGroup(job.CustomerJobID, out groupName, out jobName))
            {
                throw new ArgumentException(ExceptionMessages.JobNotPartOfAGroup);
            }
            return this.GetGroupHeadByGroupName(job.Owner, groupName);
        }

       /// <summary>
       /// Retrieves all jobs under the root job
       /// </summary>
        /// <param name="internalJobID">The internal job ID of the root element.</param>
        /// <returns></returns>
       /// <returns></returns>
        List<IJob> IGWRuntimeEnvironment.GetJobHierarchyByRoot(string internalJobID)
        {
            var ajs = this.GetAll();
            var aj = ajs.AllJobs
                .Where(e => e.RowKey == internalJobID)
                .FirstOrDefault();

            if (aj == null)
                return new List<IJob>();

            List<IJob> resultSet = new List<IJob>();
            JobID jobId=null;
            if(!JobID.TryParse(aj.CustomerJobID, out jobId))
            {
                resultSet.Add(this.GetJobByID(aj.RowKey));
                return resultSet;
            }

            
            List<string> unprocessedNodes = new List<string>();
            unprocessedNodes.Add(internalJobID);

            while (unprocessedNodes.Count > 0)
            {
                AzureJob azureJob = this.GetJobByID(unprocessedNodes[0]) as AzureJob;

                if (azureJob != null)
                {
                    resultSet.Add(azureJob);
                    foreach (var child in azureJob.GetChildren())
                    {
                        unprocessedNodes.Add(child);
                    }
                }

                unprocessedNodes.RemoveAt(0);
            }

            return resultSet;
        }

        /// <summary>
        /// Returns the root of the hierarchy
        /// </summary>
        /// <param name="internalJobID">Internal unique id of the root job</param>
        /// <returns></returns>
        IJob IGWRuntimeEnvironment.GetRootofTheHierarchy(string internalJobID)
        {
            AzureJob job = GetJobByID(internalJobID) as AzureJob;
            if (job == null)
            {
                return null;
            }

            //if submitted job is a hierarchical job we can go one step more
            JobID jobId;
            if (!JobID.TryParse(job.CustomerJobID, out jobId))
            {
                return null;
            }
            else
            {
                while (!String.IsNullOrEmpty(job.ParentGUID))
                {
                    job = this.GetJobByID(job.ParentGUID) as AzureJob;
                    if (job == null)
                    {
                        return null;
                    }
                }
            }

            return job;
        }

        /// <summary>
        /// Persists the StatusMessage of a job.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="output">The output.</param>
        private void WriteStatus(IJob job, string output)
        {
            var statusBlob = this._blobContainer.GetBlockBlobReference(AzureJob.GetStatusTextBlobName(job.Owner, job.InternalJobID));
            statusBlob.AppendText(output, LIMIT_SIZE_TABLE_COLUMN);
        }


        private void UpdateJobOutputs(IJob iJob, string status, string stdout, string stderr)
        {
            //Todo: check/retry //blob timeout increase //notfication/message output truncation
            if (!String.IsNullOrEmpty(status))
            {
                this.WriteStatus(iJob, status);
            }
            if (!String.IsNullOrEmpty(stdout))
            {
                this.WriteStdOut(iJob, stdout);
            }
            if (!String.IsNullOrEmpty(stderr))
            {
                this.WriteStdErr(iJob, stderr);
            }
        }

        /// <summary>
        /// Updates the outputs of a job.
        /// </summary>
        /// <param name="iJob">The job.</param>
        /// <param name="stdout">The stdout.</param>
        /// <param name="stderr">The stderr.</param>
        /// <param name="statusText">The status text.</param>
        public void UpdateJobOutputs(IJob iJob, StringBuilder stdout, StringBuilder stderr, StringBuilder statusText)
        {
            string output;
            output = ReadAndClear(stdout);
            WriteStdOut(iJob, output); 

            output = ReadAndClear(stderr);
            WriteStdErr(iJob, output); 

            output = ReadAndClear(statusText);
            WriteStatus(iJob, output); 
        }

        /// <summary>
        /// Reads the content of the provided StringBuilder instance and clears it.
        /// </summary>
        /// <param name="sb">The StringBuilder instance.</param>
        /// <returns>The content of the StringBuilder instance.</returns>
        private static string ReadAndClear(StringBuilder sb)
        {
            string output="";
            if (sb != null)
            {
                lock (sb)
                {
                    output = sb.ToString();
                    sb.Clear();
                }
            }
            return output;
        }

        /// <summary>
        /// Writes the StdOut of a job to a blob.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="output">The output.</param>
        private void WriteStdOut(IJob job, string output)
        {
            var stdoutBlob = this._blobContainer.GetBlockBlobReference(AzureJob.GetStdOutBlobName(job.Owner, job.InternalJobID));
            stdoutBlob.AppendText(output, LIMIT_SIZE_TABLE_COLUMN);
        }

        /// <summary>
        /// Writes the StdErr of a job to a blob.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="output">The output.</param>
        private void WriteStdErr(IJob job, string output)
        {
            var stderrBlob = this._blobContainer.GetBlockBlobReference(AzureJob.GetStdErrBlobName(job.Owner, job.InternalJobID));
            stderrBlob.AppendText(output, LIMIT_SIZE_TABLE_COLUMN);
        }

        /// <summary>
        /// Gets the job status.
        /// </summary>
        /// <param name="internalJobID">The internal job ID.</param>
        /// <returns></returns>
        public JobStatus? GetJobStatus(string internalJobID)
        {
            var ajs = this.GetAll();

            var j = ajs.AllJobs.Where(aj =>
                    aj.RowKey == internalJobID).FirstOrDefault();

            if (j == null) return null;

            return (JobStatus)Enum.Parse(typeof(JobStatus), j.Status);
        }

        //make maxRetries configurable - exponential backoff
        private bool UpdateAndHandleExceptionWithReries(Func<bool> act)
        {
            Func<Func<bool>, bool> updateAndHandleExceptionWithReries = action =>
            {
                var maxRetries = 5;
                var rnd = new Random();
                for (int i = 0; i < maxRetries; i++)
                {
                    try
                    {
                        return action();
                    }
                    catch (StorageClientException sce)
                    {
                        if (sce.ErrorCode == StorageErrorCode.ConditionFailed) return false;
                        throw;
                    }
                    catch (DataServiceRequestException dsre)
                    {
                        var inner = dsre.InnerException as DataServiceClientException;
                        if (inner == null) throw;
                        if (inner.StatusCode != 412) throw;
                        if (i < maxRetries)
                        {
                            System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(rnd.Next(100, 500)));
                        }
                        else
                        {
                            Trace.TraceWarning("reached maximum number of retries for update operation on azure table");
                            if (inner.Message.Contains("ConditionNotMet")) return false;
                            if (inner.Message.Contains("UpdateConditionNotSatisfied")) return false;
                            throw;
                        }
                    }
                }
                return false;
            };
            return updateAndHandleExceptionWithReries(act);
        }

        bool IGWRuntimeEnvironment.TryDequeueJob(out IJob job)
        {
            try
            {
                job = ((IGWRuntimeEnvironment)this).CurrentJobs
                .Where(j => j.Status == JobStatus.Submitted)
                .FirstOrDefault() as AzureJob;
                if (job != null)
                {
                    lastDequeuedJobInternalID = job.InternalJobID;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                job = null;
                lastDequeuedJobInternalID = null;
            }
            return (job != null);
        }

        private bool ProcessJob(string owner, string internalJobID, JobStatus newStatus, 
            Func<JobTableEntity, bool> customAction, bool deleteCurrent)
        {
            // This method must be synchronized in order to avoid table write conflicts
            // BE CAREFUL: Regardless of how this method is left, the mutex has to be released!
            if (!_TableWriteLock.WaitOne(MUTEX_TIMEOUT_MS))
            {
                throw new TimeoutException(ExceptionMessages.CanNotClaimMutex);
            }
            try
            {
                var ajs = this.GetAll();
                var aj = ajs.AllJobs.Where(e => e.PartitionKey == owner && e.RowKey == internalJobID).FirstOrDefault();

                if (aj == null)
                {
                    return false;
                }
                var oldStatus = aj.GetStatus();

                Func<bool> updateDetails = () =>
                {
                    ajs = this.GetAll();
                    aj = ajs.AllJobs.Where(e => e.PartitionKey == owner && e.RowKey == internalJobID).FirstOrDefault();

                    if (aj == null)
                    {
                        return false;
                    }
                    if (!TransitionTable.IsValidTransition(
                        oldStatus: aj.GetStatus(),
                        newStatus: newStatus))
                    {
                        return false;
                    }
                    if (customAction != null)
                    {
                        if (!customAction(aj))
                            return false;
                    }

                    aj.SetStatus(newStatus);
                    aj.LastChange = DateTime.UtcNow;
                    ajs.UpdateObject(aj);
                    ajs.SaveChangesWithRetries();
                    return true;
                };

                Func<bool> updateIndex = () =>
                {
                    if (deleteCurrent)
                    {
                        var cjs = this.GetCurrent();
                        var cj = cjs.CurrentJobs.Where(e => e.PartitionKey == owner && e.RowKey == internalJobID).FirstOrDefault();
                        if (cj != null)
                        {
                            cjs.DeleteObject(cj);
                            cjs.SaveChangesWithRetries();
                        }
                    }
                    else if (newStatus == JobStatus.CheckingInputData)
                    {
                        var cjs = this.GetCurrent();
                        var cj = cjs.CurrentJobs.Where(e => e.PartitionKey == owner && e.RowKey == internalJobID).FirstOrDefault();
                        if (cj != null)
                        {
                            cj.LastChange = DateTime.UtcNow;
                            cjs.UpdateObject(cj);
                            cjs.SaveChangesWithRetries();
                        }
                    }

                    return true;
                };

                if (!UpdateAndHandleExceptionWithReries(updateDetails))
                {
                    return false;
                }
                if (!UpdateAndHandleExceptionWithReries(updateIndex))
                {
                    return false;
                }

                IJob job = new AzureJob(_blobContainer, ArgumentRepository, aj);
                GWEventDispatcher.Notify(new StatusEvent(oldStatus, newStatus, job, DateTime.Now.ToUniversalTime()));
                GWEventDispatcher.Notify(GetCurrentNetworkCountersForVM(oldStatus, newStatus, job));

                return true;
            }
            finally
            {
                // Release mutex, otherwise danger of deadlock
                _TableWriteLock.ReleaseMutex();
            }
        }

        private NetworkEvent GetCurrentNetworkCountersForVM(JobStatus oldStatus, JobStatus newStatus, IJob job)
        {
            var networkEvent = new NetworkEvent();
            networkEvent.Job = job;
            networkEvent.OldStatus = oldStatus;
            networkEvent.NewStatus = newStatus;
            networkEvent.BytesReceived = 0;
            networkEvent.BytesSent = 0;
            networkEvent.NotificationTime = DateTime.Now.ToUniversalTime();
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var networkInterface in interfaces)
            {
               if (networkInterface.OperationalStatus== OperationalStatus.Up && 
                    (networkInterface.NetworkInterfaceType== NetworkInterfaceType.Ethernet || networkInterface.NetworkInterfaceType== NetworkInterfaceType.Ethernet3Megabit
                    || networkInterface.NetworkInterfaceType== NetworkInterfaceType.FastEthernetFx || networkInterface.NetworkInterfaceType== NetworkInterfaceType.FastEthernetT
                    || networkInterface.NetworkInterfaceType== NetworkInterfaceType.GigabitEthernet || networkInterface.NetworkInterfaceType== NetworkInterfaceType.GenericModem
                    || networkInterface.NetworkInterfaceType== NetworkInterfaceType.Wireless80211))
                {
                    networkEvent.BytesReceived += networkInterface.GetIPv4Statistics().BytesReceived;    //bytes received
                    networkEvent.BytesSent += networkInterface.GetIPv4Statistics().BytesSent;
                }
            }

            return networkEvent;
        }

        bool IGWRuntimeEnvironment.MarkJobAsChekingInputData(IJob iJob, string instanceID)
        {
            Func<JobTableEntity, bool> a = aj =>
            {
                if (aj.GetStatus() != JobStatus.Submitted)
                    return false;

                aj.InstanceID = instanceID;
                aj.Start = DateTime.UtcNow;

                return true;
            };

            return ProcessJob(iJob.Owner, iJob.InternalJobID, JobStatus.CheckingInputData, a, deleteCurrent: false);
        }

        bool IGWRuntimeEnvironment.MarkJobAsSubmittedBack(IJob iJob, string status, bool increaseResetCounter)
        {
            UpdateJobOutputs(iJob, status, null, null);

            Func<JobTableEntity, bool> a = aj =>
            {
                aj.InstanceID = null;
                aj.Start = null;
                aj.DataChecked = DateTime.UtcNow;
                if (increaseResetCounter)
                {
                    aj.ResetCounter = iJob.ResetCounter + 1;
                }
                return true;
            };

            return ProcessJob(iJob.Owner, iJob.InternalJobID, JobStatus.Submitted, a, deleteCurrent: false);
        }


        bool IGWRuntimeEnvironment.MarkJobAsRunning(IJob iJob, string status, string stdoutUpToNow, string stderrUpToNow)
        {
            UpdateJobOutputs(iJob, status, stdoutUpToNow, stderrUpToNow);
            Func<JobTableEntity, bool> a = aj =>
            {
                return true;
            };

            return ProcessJob(iJob.Owner, iJob.InternalJobID, JobStatus.Running, a, deleteCurrent: false);
        }


        bool IGWRuntimeEnvironment.MarkJobAsFinished(IJob iJob, string status, string stdout, string stderr)
        {
            UpdateJobOutputs(iJob, status, stdout, stderr);
            //Todo: Transaction mechanism?
            Func<JobTableEntity, bool> a = aj =>
            {                              
                aj.End = DateTime.UtcNow;
                return true;
            };

            return ProcessJob(iJob.Owner, iJob.InternalJobID, JobStatus.Finished, a, deleteCurrent: true);
        }

        bool IGWRuntimeEnvironment.MarkJobAsFailed(IJob iJob, string status, string stdout, string stderr)
        {
            UpdateJobOutputs(iJob, status, stdout, stderr);

            Func<JobTableEntity, bool> a = aj =>
            {
                aj.End = DateTime.UtcNow;
                return true;
            };

            JobID jobID = null;
            if (!JobID.TryParse(iJob.CustomerJobID, out jobID))
            {
                return ProcessJob(iJob.Owner, iJob.InternalJobID, JobStatus.Failed, a, deleteCurrent: true);
            }
            else
            {
                //automatically cancel all successors.
                return MarkJobHierarchyCancellationPending(iJob);
            }
        }

        bool IGWRuntimeEnvironment.MarkJobAsCancellationPending(IJob iJob)
        {
            // If the job was submitted when we cancel it, there will be no worker feeling responsible for 
            // transitioning into the Cancelled state, so we need to do it ourselves. 

            JobStatus oldStatus = JobStatus.Failed;

            Func<JobTableEntity, bool> a = aj =>
            {
                oldStatus = aj.GetStatus();

                return true;
            };

            JobID jobID = null;
            if (!JobID.TryParse(iJob.CustomerJobID, out jobID))
            {

                bool transitionedToCancelRequested = ProcessJob(iJob.Owner, iJob.InternalJobID, JobStatus.CancelRequested, a, deleteCurrent: true);
                if (!transitionedToCancelRequested)
                    return false;

                if (oldStatus == JobStatus.Submitted)
                    return ProcessJob(iJob.Owner, iJob.InternalJobID, JobStatus.Cancelled, a, deleteCurrent: false);

                return true;
            }
            else
            {
                return MarkJobHierarchyCancellationPending(iJob);
            }
        }

        private bool MarkJobHierarchyCancellationPending(IJob iJob)
        {
            AzureJob job = iJob as AzureJob;
            if (job == null)
            {
                throw new NotSupportedException();
            }

            bool returnValue = false;

           
            JobID parentJobID=null;
            if (!JobID.TryParse(job.CustomerJobID, out parentJobID))
            {
                return false;
            }

            //iterate through the hierarchy
            List<String> children = new List<string>();
            children.Add(job.InternalJobID);
            
            while (children.Count>0)
            {
                var actualJob = this.GetAll().AllJobs.Where(j => j.PartitionKey == job.Owner && j.RowKey == children[0]).FirstOrDefault();

                JobID jobID = null;
                if (JobID.TryParse(actualJob.CustomerJobID, out jobID))
                {
                    if (jobID.IsPartOf(parentJobID))
                    {
                        JobStatus oldStatus = JobStatus.Failed;

                        Func<JobTableEntity, bool> a = aj =>
                        {
                            oldStatus = aj.GetStatus();

                            return true;
                        };

                        bool transitionedToCancelRequested = ProcessJob(actualJob.PartitionKey, actualJob.RowKey, JobStatus.CancelRequested, a, deleteCurrent: true);
                        if (transitionedToCancelRequested)
                        {
                            if (oldStatus == JobStatus.Submitted || oldStatus == JobStatus.Finished)
                            {
                                returnValue = returnValue || ProcessJob(actualJob.PartitionKey, actualJob.RowKey, JobStatus.Cancelled, a, deleteCurrent: false);
                            }
                            else
                            {
                                returnValue = true;
                            }
                        }
                    }
                }
                var parentAzureJob = this.GetJobByID(actualJob.RowKey) as AzureJob;
                if (parentAzureJob.HasChildren())
                {
                    var childrenOftheJob = parentAzureJob.GetChildren();
                    children.AddRange(childrenOftheJob);
                }

                children.RemoveAt(0);

            }

            return returnValue;
        }

        bool IGWRuntimeEnvironment.MarkJobAsCancelled(IJob iJob, string status, string stdout, string stderr)
        {
            UpdateJobOutputs(iJob, status, stdout, stderr);

            Func<JobTableEntity, bool> a = aj =>
            {
                aj.End = DateTime.UtcNow;
                return true;
            };

            return ProcessJob(iJob.Owner, iJob.InternalJobID, JobStatus.Cancelled, a, deleteCurrent: true);
        }


        bool IGWRuntimeEnvironment.MarkHygieneAsRunning(TimeSpan intervallToLastRun)
        {   
            var htdc = new HygieneTableDataContext(this._account.TableEndpoint.AbsoluteUri, this._account.Credentials, this.GenericWorkerHygieneTableName);
            var hte = htdc.HygieneStatus.Where(result => result.PartitionKey == HygienePartitionKey && result.RowKey == HygieneRowKey).FirstOrDefault();
            
            if (hte == null)
            {
                hte = new HygieneTableSchedulingEntity
                          {
                              PartitionKey = HygienePartitionKey,
                              RowKey = HygieneRowKey,
                              IsRunning = false,
                              LastRun = DateTime.UtcNow.Subtract(intervallToLastRun.Add(intervallToLastRun))
                          };
                try
                {
                    htdc.AddObject(htdc.TableName, hte);
                    htdc.SaveChangesWithRetries();
                }
                catch (Exception)
                {
                    ; // suppress exception
                }
                
                return false;
            }

            if (hte.IsRunning && hte.LastRun < DateTime.UtcNow.AddSeconds((int)intervallToLastRun.TotalSeconds * -3))
            {
                hte.IsRunning = false;
                hte.LastRun = DateTime.UtcNow.Subtract(intervallToLastRun).AddSeconds(-1);
                try
                {
                    htdc.UpdateObject(hte);
                    htdc.SaveChangesWithRetries();
                }
                catch
                {
                    return false;
                }
            }

            if (hte.IsRunning || hte.LastRun > DateTime.UtcNow.Subtract(intervallToLastRun))
            {
                    return false;
            }

            hte.IsRunning = true;
            try
            {
                htdc.UpdateObject(hte);
                htdc.SaveChangesWithRetries();
            }
            catch (StorageClientException sce)
            {
                if (sce.ErrorCode == StorageErrorCode.ConditionFailed)
                {
                    return false;
                }

                throw;
            }
            catch (DataServiceRequestException dsre)
            {
                var inner = dsre.InnerException as DataServiceClientException;
                if (inner != null && inner.StatusCode == 412 &&
                    inner.Message.Contains("ConditionNotMet"))
                {
                    return false;
                }

                throw;
            }

            return true;
        }

        const string HygienePartitionKey = "HygienePartition";
        const string HygieneRowKey = "1";

        bool IGWRuntimeEnvironment.MarkHygieneAsFinished()
        {
            var htdc = new HygieneTableDataContext(this._account.TableEndpoint.AbsoluteUri, this._account.Credentials, this.GenericWorkerHygieneTableName);
            var hte = htdc.HygieneStatus.Where(result => result.PartitionKey == HygienePartitionKey && result.RowKey == HygieneRowKey).FirstOrDefault();

            if (hte == null || !hte.IsRunning)
            {
                return false;
            }

            hte.IsRunning = false;
            hte.LastRun = DateTime.UtcNow;
            try
            {
                htdc.UpdateObject(hte);
                htdc.SaveChangesWithRetries();
            }
            catch (StorageClientException sce)
            {
                if (sce.ErrorCode == StorageErrorCode.ConditionFailed)
                {
                    return false;
                }

                throw;
            }
            catch (DataServiceRequestException dsre)
            {
                var inner = dsre.InnerException as DataServiceClientException;
                if (inner != null && inner.StatusCode == 412 &&
                    inner.Message.Contains("ConditionNotMet"))
                {
                    return false;
                }

                throw;
            }

            return true;
        }

        IEnumerable<IJob> IGWRuntimeEnvironment.CurrentJobs
        {
            get
            {
                var cjs = this.GetCurrent();
                foreach (var cj in cjs.CurrentJobs.ToList().OrderBy(j => j.Timestamp))
                {
                    var ajs = this.GetAll().AllJobs;
                    var j = ajs.Where(e => e.PartitionKey == cj.PartitionKey && e.RowKey == cj.RowKey).FirstOrDefault();

                    if (j == null)
                    {
                        // An entry in the CurrentJobs table that does not have
                        // a corresponding one in AllJobs indicates an inconsistency. 
                        cjs.DeleteObject(cj);
                        cjs.SaveChangesWithRetries();
                        continue;
                    }

                    yield return new AzureJob(this._blobContainer, this.ArgumentRepository, j);
                }
            }
        }

        IEnumerable<IJob> IGWRuntimeEnvironment.AllJobs
        {
            get 
            { 
                return this.GetAll().AllJobs.AsEnumerable()
                    .Select(j => new AzureJob(this._blobContainer, this.ArgumentRepository, j)); 
            }
        }

        IEnumerable<IJob> IGWRuntimeEnvironment.AllJobsOfOwner(string owner)
        {
            return this.GetAll().AllJobsOfOwner(owner).AsEnumerable().Where(j => !JobID.IsGroupHead(j.CustomerJobID)).Select(j => new AzureJob(this._blobContainer, this.ArgumentRepository, j));
        }

        bool IGWRuntimeEnvironment.AcceptLocalJob(string customerJobId)
        {
            return AcceptLocalJob(customerJobId);
        }

        private bool AcceptLocalJob(string customerJobId)
        {
            //return false, if there is no parent job known by runtime
            Trace.WriteLine(string.Format("Runtime: {0}, parent: {1}, cust job: {2}", this.GetHashCode(), lastDequeuedJobInternalID, customerJobId));
            if(String.IsNullOrEmpty(lastDequeuedJobInternalID))
            {
                throw new Exception(ExceptionMessages.ParentJobIDNotFound);
            }

            //Return false => parent job cannot be found
            AzureJob parentJob = this.GetJobByID(lastDequeuedJobInternalID) as AzureJob;
            if (parentJob == null)
            {
                throw new Exception(ExceptionMessages.ParentJobNotFound);
            }

            //if submitted job is not a hierarchical job it can be accepted
            JobID jobId;
            if (!JobID.TryParse(customerJobId, out jobId))
            { 
                return true;
            }

            //We have to check the naming convention if the job is an hieararhical job
            JobID jobIDLastDequeued;
            if (JobID.TryParse(parentJob.CustomerJobID, out jobIDLastDequeued))
            {
                //Parent and submitted job are both hierarhical
                //But they are irrelevant which means parent may create another job group
                if(!jobId.IsPartOf(jobIDLastDequeued))
                {
                    return true;
                }
                else if (jobId.IsDirectChildOf(jobIDLastDequeued)) //parent jobs can only submit direct children
                {
                    //its name should be unique 
                    var children = parentJob.GetChildren();
                    foreach (var child in children)
                    {
                        var childJob = this.GetJobByID(child);
                        if (childJob.CustomerJobID == customerJobId)
                        {
                            throw new Exception(ExceptionMessages.UniqueHierarchicalNameError);
                        }
                    }

                    //now all ancestors should be checked
                    //if one of the ancestors is failed, cancelled or cancelrequested 
                    //return false
                    while (true)
                    {

                        var status = parentJob.Status;

                        if (status == JobStatus.Failed || status == JobStatus.Cancelled || status == JobStatus.CancelRequested)
                        {
                            throw new Exception(ExceptionMessages.AncestorIsFailedorCancelled);
                        }

                        if (String.IsNullOrEmpty(parentJob.ParentGUID))
                        {
                            return true;
                        }
                        else
                        {
                            parentJob = this.GetJobByID(parentJob.ParentGUID) as AzureJob;
                            if (parentJob == null)
                            {
                                throw new Exception(ExceptionMessages.AncestorCannotFound);
                            }
                        }
                    }
                }
                else //if the submitted job is not a direct child, return false
                {
                    throw new Exception(ExceptionMessages.LevelHierarchyError);
                }
            }
           

            return true;
        }

        //consider extending IJob interface with HasChildren, HasParent
        void IGWRuntimeEnvironment.DeleteTerminatedJobs(string owner)            
        {
            DeleteTerminatedSingleJobs(owner);

            Func<bool> DeleteTerminatedHierarchiesAndGroupMembers = () =>
            {
                #region load the complete list of the users jobs
                var tc = this.GetAll();
                var usersJobs = tc.AllJobs.Where(j => j.PartitionKey == owner).ToList();
                #endregion
                var terminatedRoots = usersJobs.Where(j => j.Status == "Finished" || j.Status == "Failed" || j.Status == "Cancelled");
                foreach (var job in terminatedRoots)
                {
                    var parent = job.ParentGUID;
                    var parentAzureJob = this.GetJobByID(job.RowKey) as AzureJob;
                    var childs = parentAzureJob.GetChildren();
                    #region DeleteTerminatedHierarchies
                    if (parent == null && childs.Count>0) //terminated root of a job hierarchy, check if all jobs in the hierarchy have terminated
                    {
                        var childGuids = childs;

                        var numProcessedItems = 0;
                        while (childGuids.Count > numProcessedItems)
                        {
                            var azureJob = this.GetJobByID(childGuids[numProcessedItems]) as AzureJob;

                            if (!azureJob.IsTerminated())
                            {
                                break;
                            }

                            childGuids.AddRange(azureJob.GetChildren());
                            numProcessedItems++;
                        }

                        //if the job hierarchy has unterminated nodes skip it
                        if (childGuids.Count > numProcessedItems)
                        {
                            continue;
                        }
                        //Delete the job hierarchy                        
                        foreach (var childGuid in childGuids)
                        {
                            var azureJob = this.GetJobByID(childGuid) as AzureJob;
                            azureJob.RemoveChildren();
                            var je = usersJobs.Where(j => j.PartitionKey == owner && j.RowKey == childGuid).FirstOrDefault();
                            if (je != null)
                            {
                                BatchDelete(tc, je);
                            }
                        }
                        var rootAzureJob = this.GetJobByID(job.RowKey) as AzureJob;
                        rootAzureJob.RemoveChildren();
                        BatchDelete(tc,job);                        
                    }
                    #endregion
                    #region DeleteTerminatedGroupMembers                    
                    else
                    {
                        string groupName, jobName;
                        if (JobID.TryParseGroup(job.CustomerJobID, out groupName, out jobName))
                        {
                            DeleteGroupMember(tc, usersJobs, job, groupName);
                        }
                    }
                    #endregion
                }
                tc.SaveChangesWithRetries(SaveChangesOptions.Batch );
                return true;
            };
            this.UpdateAndHandleExceptionWithReries(DeleteTerminatedHierarchiesAndGroupMembers);
        }

        private void DeleteGroupMember(JobTableDataContext tc, List<JobTableEntity> usersJobs, JobTableEntity job, string groupName)
        {
            BatchDelete(tc, job);
            var groupHead = usersJobs.Where(j => j.RowKey == groupName).FirstOrDefault();
            if (groupHead != null)
            {
                //remove jobGUID from the groupheads list of childs
                var parentAzureJob = this.GetJobByID(groupHead.RowKey) as AzureJob;
                var childrenList = parentAzureJob.GetChildren();
                childrenList.Remove(job.RowKey);
                parentAzureJob.RemoveChild(job.RowKey);
                if (childrenList.Count == 0)
                {
                    BatchDelete(tc, groupHead);
                }
            }
        }

        /// <summary>
        /// Executes a batch operation as soon as 100 entities are marked for deletion
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="jobEntity">The job entity.</param>
        private void BatchDelete(JobTableDataContext context, JobTableEntity jobEntity)
        {
            AzureJob.DeleteAssociatedBlobs(this._blobContainer, jobEntity.PartitionKey, jobEntity.RowKey);

            var numDirtyEntities = context.Entities.Where(e => e.State == EntityStates.Deleted).Count();
            if (numDirtyEntities == 100)
            {
                context.SaveChangesWithRetries(SaveChangesOptions.Batch);
            }
            context.DeleteObject(jobEntity);
        }
        /// <summary>
        /// Delete all terminated jobs that do not belong to a group or hierarchy.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <returns></returns>
        private void DeleteTerminatedSingleJobs(string owner)
        {
            Func<bool> _DeleteTerminatedSingleJobs = () =>
            {
                var tc = this.GetAll();

                var singleTerminatedJobs = tc.AllJobsOfOwner(owner).Where(j => j.Status == "Finished" || j.Status == "Failed" || j.Status == "Cancelled"); 
                if (singleTerminatedJobs != null)
                {
                    singleTerminatedJobs.ToList().Where(j => j.ParentGUID == null).ToList().ForEach(j =>
                    {
                        var azureJob = this.GetJobByID(j.RowKey) as AzureJob;
                        if (!azureJob.HasChildren())
                        {
                            BatchDelete(tc, j);
                        }
                    });
                    tc.SaveChangesWithRetries(SaveChangesOptions.Batch);                   
                }
                return true;
            };
            this.UpdateAndHandleExceptionWithReries(_DeleteTerminatedSingleJobs);
        }

        #endregion

        /// <summary>
        /// Subscription Method for the Notifications
        /// </summary>
        /// <param name="iJob">The job which is been subscribed to</param>
        /// <param name="subs">Details of the subscription</param>
        /// <returns></returns>
        public bool Subscribe(IJob iJob, Subscription subs)
        {
            var ajs = this.GetAll();
            var aj = ajs.AllJobs.Where(e => e.PartitionKey == iJob.Owner && e.RowKey == iJob.InternalJobID).FirstOrDefault();

            if (aj == null)
            {
                return false;
            }

            var azj = new AzureJob(this._blobContainer, this.ArgumentRepository, aj);

            var subsconfig = azj.GetNotificationSubscriptions(this._notificationSubscriptionsContainer);

            var newSubscriptions = new List<Subscription>();

            var xs = new XmlSerializer(typeof(List<Subscription>));

            if (subsconfig.Count > 0)
            {
                newSubscriptions.AddRange(subsconfig);
            }
            newSubscriptions.Add(subs);

            azj.SetNotificationSubscriptions(newSubscriptions, this._notificationSubscriptionsContainer);
            return true;
        }

        /// <summary>
        /// Subscribe to the events in the given group
        /// </summary>
        /// <param name="owner">Owner of the group</param>
        /// <param name="groupName">Name of the group</param>
        /// <param name="subs">The details of the subscription</param>
        /// <returns></returns>
        public bool Subscribe(string owner, string groupName, Subscription subs)
        {
            var groupmembers = this.GetJobGroupByOwnerGroupName(owner, groupName);
            if (groupmembers.Count == 0)
                throw new Exception(ExceptionMessages.NoGroupWithName);
            var ajs = this.GetAll();
            var groupEntity = ajs.AllJobs.Where(e => e.PartitionKey == owner && e.RowKey == groupName).FirstOrDefault();
            var azj = new AzureJob(this._blobContainer, this.ArgumentRepository, groupEntity);
            var subsconfig = azj.GetNotificationSubscriptions(this._notificationSubscriptionsContainer);

            var newSubscriptions = new List<Subscription>();

            var xs = new XmlSerializer(typeof(List<Subscription>));

            if (subsconfig.Count > 0)
            {
                newSubscriptions.AddRange(subsconfig);
            }
            newSubscriptions.Add(subs);

            azj.SetNotificationSubscriptions(newSubscriptions, this._notificationSubscriptionsContainer);
            return true;
        }

        /// <summary>
        /// Unsubscribe from the given job
        /// </summary>
        /// <param name="iJob">The job which was previously been subscribed to</param>
        /// <returns></returns>
        public bool UnSubscribe(IJob iJob)
        {
            var ajs = this.GetAll();
            var aj = ajs.AllJobs.Where(e => e.PartitionKey == iJob.Owner && e.RowKey == iJob.InternalJobID).FirstOrDefault();

            if (aj == null)
            {
                return false;
            }

            this._notificationSubscriptionsContainer.GetBlobReference(aj.RowKey).DeleteIfExists();
            return true;
        }

        /// <summary>
        /// This Method is used to cache the last 10 subscriptions retrieved from the runtime.
        /// Caching is applied to reduce the number of transactions (azure table accesses)
        /// </summary>
        /// <param name="job">The job.</param>
        /// <returns></returns>
        public List<Subscription> GetSubscriptions(IJob job) 
        {
            var ajs = this.GetAll();
            var aj = ajs.AllJobs.Where(e => e.PartitionKey == job.Owner && e.RowKey == job.InternalJobID).FirstOrDefault();

            if (aj == null)
            {
                return new List<Subscription>();
            }
            var azj = new AzureJob(this._blobContainer, this.ArgumentRepository, aj);
            return azj.GetNotificationSubscriptions(this._notificationSubscriptionsContainer);
        }

        /// <summary>
        /// Cancel all of the jobs in the given group
        /// </summary>
        /// <param name="owner">Owner of the group</param>
        /// <param name="groupName">Name of the group</param>
        public void CancelGroup(string owner, string groupName)
        {
            var aj = this.GetAll();
            var groupEntity = aj.AllJobs.Where(e => e.PartitionKey == owner && e.RowKey == groupName).FirstOrDefault();
            if (groupEntity != null)
            {
                var azureJob = this.GetJobByID(groupEntity.RowKey) as AzureJob;
                var childJobIds = azureJob.GetChildren();
                foreach (var childJobId in childJobIds)
                {
                    var childJobEntity = aj.AllJobs.Where(j => j.RowKey == childJobId).FirstOrDefault();
                    if (childJobEntity != null)
                    {
                        if (childJobEntity.GetStatus() == JobStatus.Running)
                        {
                            ProcessJob(childJobEntity.PartitionKey,childJobEntity.RowKey, JobStatus.CancelRequested,null,true);
                        }
                        else
                        {
                            UpdateJobOutputs(this.GetJobByID(childJobEntity.RowKey), string.Format("/r/n{0}", "the job's group was canceled"), null, null);
                            Func<JobTableEntity, bool> jobCancelAction = job =>
                            {
                                job.End = DateTime.UtcNow;
                                return true;
                            };
                            ProcessJob(childJobEntity.PartitionKey, childJobEntity.RowKey, JobStatus.Cancelled, jobCancelAction, true);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Cancel all jobs in the job given hierarcht
        /// </summary>
        /// <param name="internalRootJobID">Internal Job ID of the root item</param>
        public void CancelHierarchy(string internalRootJobID)
        {
            this.MarkJobHierarchyCancellationPending(this.GetJobByID(internalRootJobID));
        }
    }


    internal static class TransitionTable
    {
        private class JobTransition
        {
            internal JobTransition(JobStatus from, JobStatus to)
            {
                this.From = from;
                this.To = to;
            }

            internal JobStatus From { get; private set; }

            internal JobStatus To { get; private set; }
        }

        private static List<JobTransition> CreateTransitions()
        {
            return new List<JobTransition>
            {
                // Submitted CheckingInputData Running Running Running Running Finished
                // Submitted CheckingInputData Submitted CheckingInputData Running Running Running Running Finished
                // Submitted CheckingInputData Running Running Running Running Failed
                // Submitted CheckingInputData Running Running Running Running CancelRequested Stopped
                // Submitted                                                   CancelRequested Stopped

                new JobTransition(JobStatus.CheckingInputData, JobStatus.Failed),               //MarkJobAsFailed (Application is not installed)
                new JobTransition(JobStatus.Submitted, JobStatus.CheckingInputData),  // MarkJobOwnershipAsClaimed
                new JobTransition(JobStatus.CheckingInputData, JobStatus.Submitted),  // MarkJobOwnershipAsSubmitted
                new JobTransition(JobStatus.CheckingInputData, JobStatus.Running),    // MarkJobAsRunning
                new JobTransition(JobStatus.Running, JobStatus.Running),              // MarkJobAsRunning
                new JobTransition(JobStatus.Submitted, JobStatus.Cancelled),    // RequestCancellation
                new JobTransition(JobStatus.CheckingInputData, JobStatus.Cancelled),    // RequestCancellation
                new JobTransition(JobStatus.Running, JobStatus.CancelRequested),      // RequestCancellation
                new JobTransition(JobStatus.Finished, JobStatus.Cancelled),      // RequestCancellation
                new JobTransition(JobStatus.Failed, JobStatus.Cancelled),      // RequestCancellation
                new JobTransition(JobStatus.CancelRequested, JobStatus.Cancelled),    // RequestAsCancelled
                new JobTransition(JobStatus.CancelRequested, JobStatus.Finished),    // RequestAsCancelled
                new JobTransition(JobStatus.CancelRequested, JobStatus.Failed),    // RequestAsCancelled
                new JobTransition(JobStatus.Running, JobStatus.Finished),
                new JobTransition(JobStatus.Running, JobStatus.Failed),
                new JobTransition(JobStatus.Running, JobStatus.Submitted)             // Reset after watchdog failure
            };
        }

        static readonly List<JobTransition> Transitions = CreateTransitions();

        internal static bool IsValidTransition(JobStatus oldStatus, JobStatus newStatus)
        {
            var stateChange = Transitions
                .Where(t => t.From == oldStatus && t.To == newStatus)
                .FirstOrDefault();
            return stateChange != null;
        }
    }
}