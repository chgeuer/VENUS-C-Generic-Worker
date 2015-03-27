//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





using System.Collections.Generic;
using OGF.BES.Faults;
using Microsoft.EMIC.Cloud.GenericWorker;
using OGF.BES.COMPSsExtensions;

namespace OGF.BES
{
    /// <summary>
    /// The class which implements the interface BESFactoryPortType by OGF.
    /// </summary>
    [System.ServiceModel.ServiceBehaviorAttribute(InstanceContextMode = System.ServiceModel.InstanceContextMode.PerCall, ConcurrencyMode = System.ServiceModel.ConcurrencyMode.Multiple)]
    public class BESFactoryPortTypeImpl : BESFactoryPortType
    {
        /// <summary>
        /// Creates a job in the target in the GW
        /// </summary>
        /// <param name="request">CreateActivityRequest having the details of the jobs</param>
        /// <returns>CreateActivityResponse which returns unique identifier of the created job</returns>
        public virtual CreateActivityResponse CreateActivity(CreateActivityRequest request)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Begin method of the CreateActivity Async Call
        /// </summary>
        /// <param name="request">CreateActivityRequest having the details of the jobs</param>
        /// <param name="callback">Asynccallback method reference</param>
        /// <param name="asyncState">AsyncState</param>
        /// <returns>AsyncResult used by the system to end the async call</returns>
        public virtual System.IAsyncResult BeginCreateActivity(CreateActivityRequest request, System.AsyncCallback callback, object asyncState)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// End method of the CreateActivity Async Call
        /// </summary>
        /// <param name="result">AsyncResult used by the system to end the async call</param>
        /// <returns>CreateActivityResponse which returns unique identifier of the created job</returns>
        public virtual CreateActivityResponse EndCreateActivity(System.IAsyncResult result)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Get the statuses of the given jobs
        /// </summary>
        /// <param name="request">GetActivityStatusesRequest having the details of the jobs</param>
        /// <returns>GetActivityStatusesResponse which returns the statuses of the jobs</returns>
        public virtual GetActivityStatusesResponse GetActivityStatuses(GetActivityStatusesRequest request)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Begin method of the GetActivityStatuses Async Call
        /// </summary>
        /// <param name="request">GetActivityStatusesRequest having the details of the jobs</param>
        /// <param name="callback">Asynccallback method reference</param>
        /// <param name="asyncState">AsyncState</param>
        /// <returns>AsyncResult used by the system to end the async call</returns>
        public virtual System.IAsyncResult BeginGetActivityStatuses(GetActivityStatusesRequest request, System.AsyncCallback callback, object asyncState)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// End method of the GetActivityStatuses Async Call
        /// </summary>
        /// <param name="result">AsyncResult used by the system to end the async call</param>
        /// <returns>GetActivityStatusesResponse which returns the statuses of the jobs</returns>
        public virtual GetActivityStatusesResponse EndGetActivityStatuses(System.IAsyncResult result)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Terminate the jobs which are submitted to GW
        /// </summary>
        /// <param name="request">TerminateActivitiesRequest having the details of the jobs</param>
        /// <returns>TerminateActivitiesResponse returning whether the desired operation is successful</returns>
        public virtual TerminateActivitiesResponse TerminateActivities(TerminateActivitiesRequest request)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Begin method of the TerminateActivities Async Call
        /// </summary>
        /// <param name="request">TerminateActivitiesRequest having the details of the jobs</param>
        /// <param name="callback">Asynccallback method reference</param>
        /// <param name="asyncState">AsyncState</param>
        /// <returns>AsyncResult used by the system to end the async call</returns>
        public virtual System.IAsyncResult BeginTerminateActivities(TerminateActivitiesRequest request, System.AsyncCallback callback, object asyncState)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// End method of the TerminateActivities Async Call
        /// </summary>
        /// <param name="result">AsyncResult used by the system to end the async call</param>
        /// <returns>TerminateActivitiesResponse returning whether the desired operation is successful</returns>
        public virtual TerminateActivitiesResponse EndTerminateActivities(System.IAsyncResult result)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Get the activity documents of the jobs
        /// </summary>
        /// <param name="request">GetActivityDocumentsRequest having the details of the jobs</param>
        /// <returns>GetActivityDocumentsResponse returns the activity documents of the jobs</returns>
        public virtual GetActivityDocumentsResponse GetActivityDocuments(GetActivityDocumentsRequest request)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Begin method of the GetActivityDocuments Async Call
        /// </summary>
        /// <param name="request">GetActivityDocumentsRequest having the details of the jobs</param>
        /// <param name="callback">Asynccallback method reference</param>
        /// <param name="asyncState">AsyncState</param>
        /// <returns>AsyncResult used by the system to end the async call</returns>
        public virtual System.IAsyncResult BeginGetActivityDocuments(GetActivityDocumentsRequest request, System.AsyncCallback callback, object asyncState)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// End method of the GetActivityDocuments Async Call
        /// </summary>
        /// <param name="result">AsyncResult used by the system to end the async call</param>
        /// <returns>GetActivityDocumentsResponse returns the activity documents of the jobs</returns>
        public virtual GetActivityDocumentsResponse EndGetActivityDocuments(System.IAsyncResult result)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Not used in GW
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public virtual GetFactoryAttributesDocumentResponse GetFactoryAttributesDocument(GetFactoryAttributesDocumentRequest request)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Not used in GW
        /// </summary>
        /// <param name="request"></param>
        /// <param name="callback"></param>
        /// <param name="asyncState"></param>
        /// <returns></returns>
        public virtual System.IAsyncResult BeginGetFactoryAttributesDocument(GetFactoryAttributesDocumentRequest request, System.AsyncCallback callback, object asyncState)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Not used in GW
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public virtual GetFactoryAttributesDocumentResponse EndGetFactoryAttributesDocument(System.IAsyncResult result)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Get all jobs for the given owner
        /// </summary>
        /// <param name="owner">Owner of the of jobs</param>
        /// <param name="page">Paging mechanism is implemented to split the return size. The number of page is needed.</param>
        /// <returns>List of unique identifiers of the jobs</returns>
        public virtual System.Collections.Generic.List<EndpointReferenceType> GetJobs(string owner, int page)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Get all the jobs in the system
        /// </summary>
        /// <param name="page">Paging mechanism is implemented to split the return size. The number of page is needed.</param>
        /// <returns>List of unique identifiers of the jobs</returns>
        public virtual System.Collections.Generic.List<EndpointReferenceType> GetAllJobs(int page)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Get the list of the jobs (hieararchy) under the root
        /// </summary>
        /// <param name="root">Unique identifier of the root job</param>
        /// <param name="page">Paging mechanism is implemented to split the return size. The number of page is needed.</param>
        /// <returns>List of unique identifiers of the jobs in the hieararchy</returns>
        public virtual System.Collections.Generic.List<EndpointReferenceType> GetHierarchy(EndpointReferenceType root, int page)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Begin method of the GetHierarchy Async Call
        /// </summary>
        /// <param name="root">Unique identifier of the root job</param>
        /// <param name="page">Paging mechanism is implemented to split the return size. The number of page is needed.</param>
        /// <param name="callback">Asynccallback method reference</param>
        /// <param name="asyncState">AsyncState</param>
        /// <returns>AsyncResult used by the system to end the async call</returns>
        public virtual System.IAsyncResult BeginGetHierarchy(EndpointReferenceType root, int page, System.AsyncCallback callback, object asyncState)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// End method of the GetHierarchy Async Call
        /// </summary>
        /// <param name="result">AsyncResult used by the system to end the async call</param>
        /// <returns>List of unique identifiers of the jobs in the hieararchy</returns>
        public virtual System.Collections.Generic.List<EndpointReferenceType> EndGetHierarchy(System.IAsyncResult result)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Get the root job of the given job 
        /// </summary>
        /// <param name="job">Unique identifier of the job in the hieararchy</param>
        /// <returns>Unique identifier of the root job</returns>
        public virtual EndpointReferenceType GetRoot(EndpointReferenceType job)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Begin method of the GetRoot Async Call
        /// </summary>
        /// <param name="job">Unique identifier of the job in the hieararchy</param>
        /// <param name="callback">Asynccallback method reference</param>
        /// <param name="asyncState">AsyncState</param>
        /// <returns>AsyncResult used by the system to end the async call</returns>
        public virtual System.IAsyncResult BeginGetRoot(EndpointReferenceType job, System.AsyncCallback callback, object asyncState)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Get the root job of the given job 
        /// </summary>
        /// <param name="result">AsyncResult used by the system to end the async call</param>
        /// <returns>Unique identifier of the root job</returns>
        public virtual EndpointReferenceType EndGetRoot(System.IAsyncResult result)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Gets all the jobs in the given group
        /// </summary>
        /// <param name="owner">Owner of the group</param>
        /// <param name="groupName">Name of the group</param>
        /// <param name="page">Paging mechanism is implemented to split the return size. The number of page is needed.</param>
        /// <returns>List of unique identifiers of the jobs in the group</returns>
        public virtual System.Collections.Generic.List<EndpointReferenceType> GetJobsByGroup(string owner, string groupName, int page)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Cancel all the jobs in the group
        /// </summary>
        /// <param name="owner">Owner of the group</param>
        /// <param name="groupName">Name of the group</param>
        public virtual void CancelGroup(string owner, string groupName)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Cancel all the jobs in the hieararchy under the given root
        /// </summary>
        /// <param name="root">Unique identifier of the root job</param>
        public virtual void CancelHierarchy(EndpointReferenceType root)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Remove the terminated jobs from the tables
        /// </summary>
        /// <param name="owner">Owner of the group</param>
        public virtual void RemoveTerminatedJobs(string owner)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Get the number of the jobs of the given owner and statuses
        /// </summary>
        /// <param name="owner">Owner of the jobs</param>
        /// <param name="statuses">Statuses asked for</param>
        /// <returns>Number of the jobs</returns>
        public virtual int GetNumberOfJobs(string owner, List<JobStatus> statuses)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// COMPSs extension method, not implemented, just added for interop reasons
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public UpdateInstancesResponse UpdateInstances(UpdateInstancesRequest request)
        {
            throw new System.NotImplementedException();
        }
    }
}

