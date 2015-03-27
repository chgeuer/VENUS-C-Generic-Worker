//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.EMIC.Cloud.GenericWorker
{
    /// <summary>
    /// Serves as isolation layer between the underlying cloud platform and the generic worker. 
    /// </summary>
    public interface IGWRuntimeEnvironment
    {

        /// <summary>
        /// Submits a job.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="internalJobID">The internal job ID.</param>
        /// <param name="venusJobDescription">The venus job description.</param>
        /// <param name="isLocalJob">Boolean value states whether the job is a local one</param>
        /// <returns></returns>
        IJob SubmitJob(string owner, string internalJobID, VENUSJobDescription venusJobDescription, bool isLocalJob = false);

        /// <summary>
        /// Retrieves a job by ID.
        /// </summary>
        /// <param name="internalJobID">The internal job ID.</param>
        /// <returns></returns>
        IJob GetJobByID(string internalJobID);

        /// <summary>
        /// Gets the job status.
        /// </summary>
        /// <param name="internalJobID">The internal job ID.</param>
        /// <returns></returns>
        JobStatus? GetJobStatus(string internalJobID);

        /// <summary>
        /// Attempts to claim ownership of a job. 
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="instanceID">The instance ID.</param>
        /// <returns></returns>
        bool MarkJobAsChekingInputData(IJob job, string instanceID);

        /// <summary>
        /// Resubmitting a job after data check
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="statusText">The status text.</param>
        /// <param name="increaseResetCounter">Boolean to show whether the function has been called by the hygiene</param>
        /// <returns></returns>
        bool MarkJobAsSubmittedBack(IJob job, string statusText, bool increaseResetCounter = false);

        /// <summary>
        /// Marks the job as running.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="statusText">The status text.</param>
        /// <param name="stdoutUpToNow">The stdout up to now.</param>
        /// <param name="stderrUpToNow">The stderr up to now.</param>
        /// <returns></returns>
        bool MarkJobAsRunning(IJob job, string statusText, string stdoutUpToNow, string stderrUpToNow);

        /// <summary>
        /// Marks the job as finished.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="statusText">The status text.</param>
        /// <param name="stdout">The stdout.</param>
        /// <param name="stderr">The stderr.</param>
        /// <returns></returns>
        bool MarkJobAsFinished(IJob job, string statusText, string stdout = "", string stderr = "");

        /// <summary>
        /// Marks the job as failed.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="failureText">The failure text.</param>
        /// <param name="stdout">The stdout.</param>
        /// <param name="stderr">The stderr.</param>
        /// <returns></returns>
        bool MarkJobAsFailed(IJob job, string failureText, string stdout = "", string stderr = "");

        /// <summary>
        /// Marks the job as cancelled.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="failureText">The failure text.</param>
        /// <param name="stdout">The stdout.</param>
        /// <param name="stderr">The stderr.</param>
        /// <returns></returns>
        bool MarkJobAsCancelled(IJob job, string failureText, string stdout = "" , string stderr = "");

        /// <summary>
        /// Marks the job as cancellation pending.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <returns></returns>
        bool MarkJobAsCancellationPending(IJob job);


        /// <summary>
        ///     Try to mark Hygiene as running. If marking has been successfull, true is returned, otherwise false.
        /// </summary>
        /// <param name="intervallToLastRun">
        ///     The intervall between two hygiene runs. If the last hygiene run has been done within the specified intervall, function returns false.
        /// </param>
        /// <returns></returns>
        bool MarkHygieneAsRunning(TimeSpan intervallToLastRun);

        /// <summary>
        /// Marks the hygiene as finished.
        /// </summary>
        /// <returns></returns>
        bool MarkHygieneAsFinished();

        /// <summary>
        /// Gets a list of all current (running or to be run) jobs.
        /// </summary>
        IEnumerable<IJob> CurrentJobs { get; }

        /// <summary>
        /// Gets a list of all jobs.
        /// </summary>
        IEnumerable<IJob> AllJobs { get; }


        /// <summary>
        /// Tries the dequeue job.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <returns>true if job could be dequeued</returns>
        bool TryDequeueJob(out IJob job);

        /// <summary>
        /// Decides whether to accept job depending on the ancestors' situation.
        /// </summary>
        /// <param name="customerJobID">Customer Job Id</param>
        /// <returns>true if job can be accpeted.</returns>
        bool AcceptLocalJob(string customerJobID);

        /// <summary>
        /// Returns all jobs under the root job
        /// </summary>
        /// <param name="internalJobID">Internal unique id of the root job</param>
        /// <returns></returns>
        List<IJob> GetJobHierarchyByRoot(string internalJobID);

 	    /// <summary>
        /// Returns the root of the hierarchy
        /// </summary>
        /// <param name="internalJobID">Internal unique id of the root job</param>
        /// <returns></returns>
        IJob GetRootofTheHierarchy(string internalJobID);

 	    /// <summary>
        /// Gets all the jobs belonging to the same group as the given job.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <returns></returns>
        List<IJob> GetJobGroupByJob(IJob job);

        /// <summary>
        /// Gets the name of the job group by owner and groupname.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <returns></returns>
        List<IJob> GetJobGroupByOwnerGroupName(string owner, string groupName);

        /// <summary>
        /// Gets the name of the group head by groupname.
        /// </summary>
        /// <param name="owner">Owner of the group</param>
        /// <param name="groupName">Name of the group.</param>
        /// <returns></returns>
        IJob GetGroupHeadByGroupName(string owner, string groupName);

        /// <summary>
        /// Gets the group head by job.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <returns></returns>
        IJob GetGroupHeadByJob(IJob job);

        /// <summary>
        /// Alls the jobs of owner.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <returns></returns>
        IEnumerable<IJob> AllJobsOfOwner(string owner);

        /// <summary>
        /// Cancels the group.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="groupName">Name of the group.</param>
        void CancelGroup(string owner, string groupName);

        /// <summary>
        /// Cancels the hierarchy.
        /// </summary>
        /// <param name="internalRootJobID">The internal root job ID.</param>
        void CancelHierarchy(string internalRootJobID);

        /// <summary>
        /// Deletes the terminated (finished and failed) root jobs (jobs with no parent) which also have no children.
        /// TODO: we should also delete also all cancelled job and and job hierarchies which are completely terminated
        /// </summary>
        void DeleteTerminatedJobs(string owner);

        /// <summary>
        /// Subscription Method for the Notifications
        /// </summary>
        /// <param name="iJob">The job which is been subscribed to</param>
        /// <param name="subs">Details of the subscription</param>
        /// <returns></returns>
        bool Subscribe(IJob iJob, Subscription subs);

        /// <summary>
        /// Subscribe to the events in the given group
        /// </summary>
        /// <param name="owner">Owner of the group</param>
        /// <param name="groupName">Name of the group</param>
        /// <param name="subs">The details of the subscription</param>
        /// <returns></returns>
        bool Subscribe(string owner, string groupName, Subscription subs);

        /// <summary>
        /// Unsubscribe from the given job
        /// </summary>
        /// <param name="iJob">The job which was previously been subscribed to</param>
        /// <returns></returns>
        bool UnSubscribe(IJob iJob);

        /// <summary>
        /// This Method is used to cache the last 10 subscriptions retrieved from the runtime.
        /// Caching is applied to reduce the number of transactions (azure table accesses)
        /// </summary>
        /// <param name="iJob">The job.</param>
        /// <returns></returns>
        List<Subscription> GetSubscriptions(IJob iJob);

        /// <summary>
        /// Updates the outputs of a job.
        /// </summary>
        /// <param name="iJob">The job.</param>
        /// <param name="stdout">The stdout.</param>
        /// <param name="stderr">The stderr.</param>
        /// <param name="statusText">The status text.</param>
        void UpdateJobOutputs(IJob iJob, StringBuilder stdout, StringBuilder stderr, StringBuilder statusText);
    }
}
