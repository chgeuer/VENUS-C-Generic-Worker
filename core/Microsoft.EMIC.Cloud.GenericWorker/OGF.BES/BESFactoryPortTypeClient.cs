//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





using System.Collections.Generic;
using System.Xml;
using Microsoft.EMIC.Cloud.GenericWorker;
using OGF.BES.COMPSsExtensions;

namespace OGF.BES
{
    /// <summary>
    /// WCF created client class to interact with OGF BES based Programming Model Enactment service
    /// </summary>
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class BESFactoryPortTypeClient : System.ServiceModel.ClientBase<BESFactoryPortType>, BESFactoryPortType
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="BESFactoryPortTypeClient"/> class.
        /// </summary>
        public BESFactoryPortTypeClient()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BESFactoryPortTypeClient"/> class.
        /// </summary>
        /// <param name="endpointConfigurationName">Name of the endpoint configuration.</param>
        public BESFactoryPortTypeClient(string endpointConfigurationName) :
            base(endpointConfigurationName)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BESFactoryPortTypeClient"/> class.
        /// </summary>
        /// <param name="endpointConfigurationName">Name of the endpoint configuration.</param>
        /// <param name="remoteAddress">The remote address.</param>
        public BESFactoryPortTypeClient(string endpointConfigurationName, string remoteAddress) :
            base(endpointConfigurationName, remoteAddress)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BESFactoryPortTypeClient"/> class.
        /// </summary>
        /// <param name="endpointConfigurationName">Name of the endpoint configuration.</param>
        /// <param name="remoteAddress">The remote address.</param>
        public BESFactoryPortTypeClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) :
            base(endpointConfigurationName, remoteAddress)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BESFactoryPortTypeClient"/> class.
        /// </summary>
        /// <param name="binding">The binding.</param>
        /// <param name="remoteAddress">The remote address.</param>
        public BESFactoryPortTypeClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) :
            base(binding, remoteAddress)
        {
        }

        /// <summary>
        /// Creates the activity.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        public CreateActivityResponse CreateActivity(CreateActivityRequest request)
        {
            return base.Channel.CreateActivity(request);
        }

        /// <summary>
        /// Gets the activity statuses.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        public GetActivityStatusesResponse GetActivityStatuses(GetActivityStatusesRequest request)
        {
            return base.Channel.GetActivityStatuses(request);
        }

        /// <summary>
        /// Terminates the activities.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        public TerminateActivitiesResponse TerminateActivities(TerminateActivitiesRequest request)
        {
            return base.Channel.TerminateActivities(request);
        }

        /// <summary>
        /// Gets the activity documents.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        public GetActivityDocumentsResponse GetActivityDocuments(GetActivityDocumentsRequest request)
        {
            return base.Channel.GetActivityDocuments(request);
        }

        /// <summary>
        /// Gets the factory attributes document.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        public GetFactoryAttributesDocumentResponse GetFactoryAttributesDocument(GetFactoryAttributesDocumentRequest request)
        {
            return base.Channel.GetFactoryAttributesDocument(request);
        }

        /// <summary>
        /// Removes the terminated jobs.
        /// </summary>
        /// <param name="owner">The owner.</param>
        public void RemoveTerminatedJobs(string owner)
        {
            base.Channel.RemoveTerminatedJobs(owner);
        }

        /// <summary>
        /// Gets the number of jobs.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="statuses">The statuses.</param>
        /// <returns></returns>
        public int GetNumberOfJobs(string owner, List<JobStatus> statuses)
        {
            return base.Channel.GetNumberOfJobs(owner, statuses);
        }

        /// <summary>
        /// Gets the jobs.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="page">The page.</param>
        /// <returns></returns>
        public List<EndpointReferenceType> GetJobs(string owner, int page=-1)
        {
            return base.Channel.GetJobs(owner,page);
        }

        /// <summary>
        /// Gets all jobs.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <returns></returns>
        public List<EndpointReferenceType> GetAllJobs(int page=-1)
        {
            return base.Channel.GetAllJobs(page);
        }

        /// <summary>
        /// Gets the hierarchy.
        /// </summary>
        /// <param name="root">The root.</param>
        /// <param name="page">The page.</param>
        /// <returns></returns>
        public List<EndpointReferenceType> GetHierarchy(EndpointReferenceType root, int page = -1)
        {
            return base.Channel.GetHierarchy(root, page);
        }

        /// <summary>
        /// Gets the root.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <returns></returns>
        public EndpointReferenceType GetRoot(EndpointReferenceType job)
        {
            return base.Channel.GetRoot(job);
        }

        /// <summary>
        /// Gets the jobs by group.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <param name="page">The page.</param>
        /// <returns></returns>
        public List<EndpointReferenceType> GetJobsByGroup(string owner, string groupName, int page = -1)
        {
            return base.Channel.GetJobsByGroup(owner, groupName, page);
        }

        /// <summary>
        /// Cancels the group.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="groupName">Name of the group.</param>
        public void CancelGroup(string owner, string groupName)
        {
            base.Channel.CancelGroup(owner, groupName);
        }

        /// <summary>
        /// Cancels the hierarchy.
        /// </summary>
        /// <param name="root">The root.</param>
        public void CancelHierarchy(EndpointReferenceType root)
        {
            base.Channel.CancelHierarchy(root);
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