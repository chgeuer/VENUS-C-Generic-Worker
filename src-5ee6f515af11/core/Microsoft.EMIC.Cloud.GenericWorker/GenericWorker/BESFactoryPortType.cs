//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OGF.BES;
using OGF.BES.Faults;
using OGF.BES.Managememt;
using OGF.JSDL_HPCPA;
using OGF.Soap;
using OGF.BES.COMPSsExtensions;

namespace Microsoft.EMIC.Cloud.GenericWorker
{
    /// <summary>
    /// Main OGF BES JSDL Interface that includes the methods provided by the GW Programming Model Enactment Service
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(Namespace = "http://schemas.ggf.org/bes/2006/08/bes-factory", ConfigurationName = "BESFactoryPortType")]
    public interface BESFactoryPortType
    {
        ///<summary>
        /// Not implemented method by the GW. It is only added to be compatible with COMPSs
        ///</summary>
        // CODEGEN: Generating message contract since the operation UpdateInstances is neither RPC nor document wrapped.
        [System.ServiceModel.OperationContractAttribute(Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/UpdateInstances" +
            "Request", ReplyAction = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/UpdateInstances" +
            "Response")]
        [System.ServiceModel.FaultContractAttribute(typeof(NotAuthorizedFaultType), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/UpdateInstances" +
            "/Fault/NotAuthorizedFault", Name = "NotAuthorizedFault")]
        [System.ServiceModel.FaultContractAttribute(typeof(UnknownActivityIdentifierFaultType), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/UpdateInstances" +
            "/Fault/UnknownActivityIdentifierFault", Name = "UnknownActivityIdentifierFault")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(Envelope))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ProblemActionType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CantApplyOperationToCurrentStateFault))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(NotAuthorizedFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(InvalidRequestMessageFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(UnknownActivityIdentifierFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(NotAcceptingNewActivitiesFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(UnsupportedFeatureFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(OperationWillBeAppliedEventuallyFaultType))]
        UpdateInstancesResponse UpdateInstances(UpdateInstancesRequest request);


        /// <summary>
        /// Removes the terminated jobs of the owner.
        /// </summary>
        /// <param name="owner">The owner.</param>
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(NotAuthorizedFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(Fault))]
        [System.ServiceModel.FaultContractAttribute(typeof(NotAuthorizedFaultType), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/RemoveTerminatedJobs/" +
            "Fault/NotAuthorizedFault", Name = "NotAuthorizedFault")]
        [System.ServiceModel.FaultContractAttribute(typeof(Fault), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/RemoveTerminatedJobs/" +
            "Fault/Fault", Name = "Fault")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        [System.ServiceModel.OperationContractAttribute(Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/RemoveTerminatedJobs", ReplyAction = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/RemoveTerminatedJobsResponse")]
        void RemoveTerminatedJobs(string owner);

        /// <summary>
        /// Gets the number of jobs of the owner with the specified statuses.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="statuses">The statuses.</param>
        /// <returns></returns>
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(NotAuthorizedFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(Fault))]
        [System.ServiceModel.FaultContractAttribute(typeof(NotAuthorizedFaultType), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetNumberOfJobs/" +
            "Fault/NotAuthorizedFault", Name = "NotAuthorizedFault")]
        [System.ServiceModel.FaultContractAttribute(typeof(Fault), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetNumberOfJobs/" +
            "Fault/Fault", Name = "Fault")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        [System.ServiceModel.OperationContractAttribute(Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetNumberOfJobs", ReplyAction = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetNumberOfJobsResponse")]
        int GetNumberOfJobs(string owner, List<JobStatus> statuses);

        /// <summary>
        /// Gets the jobs of the owner with paging.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="page">The page.</param>
        /// <returns></returns>
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(NotAuthorizedFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(Fault))]
        [System.ServiceModel.FaultContractAttribute(typeof(NotAuthorizedFaultType), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetJobs/" +
            "Fault/NotAuthorizedFault", Name = "NotAuthorizedFault")]
        [System.ServiceModel.FaultContractAttribute(typeof(Fault), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetJobs/" +
            "Fault/Fault", Name = "Fault")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        [System.ServiceModel.OperationContractAttribute(Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetJobs", ReplyAction = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetJobsResponse")]
        List<EndpointReferenceType> GetJobs(string owner, int page);

        /// <summary>
        /// Gets all jobs with paging.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <returns></returns>
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(Fault))]
        [System.ServiceModel.FaultContractAttribute(typeof(Fault), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetAllJobs/" +
            "Fault/Fault", Name = "Fault")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        [System.ServiceModel.OperationContractAttribute(Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetAllJobs", ReplyAction = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetAllJobsResponse")]
        List<EndpointReferenceType> GetAllJobs(int page);

        /// <summary>
        /// Gets all the job hierarchy.
        /// </summary>
        /// <param name="root">The root.</param>
        /// <param name="page">The page.</param>
        /// <returns></returns>
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(NotAuthorizedFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(UnknownActivityIdentifierFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(Fault))]
        [System.ServiceModel.FaultContractAttribute(typeof(NotAuthorizedFaultType), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetHierarchy/" +
            "Fault/NotAuthorizedFault", Name = "NotAuthorizedFault")]
        [System.ServiceModel.FaultContractAttribute(typeof(UnknownActivityIdentifierFaultType), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetHierarchy/" +
            "Fault/UnknownActivityIdentifierFaultType", Name = "UnknownActivityIdentifierFaultType")]
        [System.ServiceModel.FaultContractAttribute(typeof(Fault), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetHierarchy/" +
            "Fault/Fault", Name = "Fault")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        [System.ServiceModel.OperationContractAttribute(Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetHierarchy", ReplyAction = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetHierarchyResponse")]
        List<EndpointReferenceType> GetHierarchy(EndpointReferenceType root, int page);

        /// <summary>
        /// Gets the root job for the specified job.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <returns></returns>
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(NotAuthorizedFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(UnknownActivityIdentifierFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(Fault))]
        [System.ServiceModel.FaultContractAttribute(typeof(NotAuthorizedFaultType), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetRoot/" +
            "Fault/NotAuthorizedFault", Name = "NotAuthorizedFault")]
        [System.ServiceModel.FaultContractAttribute(typeof(UnknownActivityIdentifierFaultType), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetRoot/" +
            "Fault/UnknownActivityIdentifierFaultType", Name = "UnknownActivityIdentifierFaultType")]
        [System.ServiceModel.FaultContractAttribute(typeof(Fault), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetRoot/" +
            "Fault/Fault", Name = "Fault")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        [System.ServiceModel.OperationContractAttribute(Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetRoot", ReplyAction = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetRootResponse")]
        EndpointReferenceType GetRoot(EndpointReferenceType job);

        /// <summary>
        /// Gets the jobs in the specified group.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <param name="page">The page.</param>
        /// <returns></returns>
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(NotAuthorizedFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(Fault))]
        [System.ServiceModel.FaultContractAttribute(typeof(NotAuthorizedFaultType), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetJobsByGroup/" +
            "Fault/NotAuthorizedFault", Name = "NotAuthorizedFault")]
        [System.ServiceModel.FaultContractAttribute(typeof(Fault), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetJobsByGroup/" +
            "Fault/Fault", Name = "Fault")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        [System.ServiceModel.OperationContractAttribute(Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetJobsByGroup", ReplyAction = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetJobsByGroupResponse")]
        List<EndpointReferenceType> GetJobsByGroup(string owner, string groupName, int page);

        /// <summary>
        /// Cancels all the jobs of the owner in the specified group.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="groupName">Name of the group.</param>
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(NotAuthorizedFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(Fault))]
        [System.ServiceModel.FaultContractAttribute(typeof(NotAuthorizedFaultType), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/CancelGroup/" +
            "Fault/NotAuthorizedFault", Name = "NotAuthorizedFault")]
        [System.ServiceModel.FaultContractAttribute(typeof(Fault), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/CancelGroup/" +
            "Fault/Fault", Name = "Fault")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        [System.ServiceModel.OperationContractAttribute(Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/CancelGroup", ReplyAction = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/CancelGroupResponse")]
        void CancelGroup(string owner, string groupName);

        /// <summary>
        /// Cancels all the jobs in the job hierarchy.
        /// </summary>
        /// <param name="root">The root.</param>
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(NotAuthorizedFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(UnknownActivityIdentifierFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(Fault))]
        [System.ServiceModel.FaultContractAttribute(typeof(NotAuthorizedFaultType), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/CancelHierarchy/" +
            "Fault/NotAuthorizedFault", Name = "NotAuthorizedFault")]
        [System.ServiceModel.FaultContractAttribute(typeof(UnknownActivityIdentifierFaultType), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/CancelHierarchy/" +
            "Fault/UnknownActivityIdentifierFaultType", Name = "UnknownActivityIdentifierFaultType")]
        [System.ServiceModel.FaultContractAttribute(typeof(Fault), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/CancelHierarchy/" +
            "Fault/Fault", Name = "Fault")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        [System.ServiceModel.OperationContractAttribute(Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/CancelHierarchy", ReplyAction = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/CancelHierarchyResponse")]
        void CancelHierarchy(EndpointReferenceType root);


        /// <summary>
        /// Creates the activity for the specified CreateActivityRequest
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        [System.ServiceModel.OperationContractAttribute(Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/CreateActivity", ReplyAction = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/CreateActivityR" +
            "esponse")]
        [System.ServiceModel.FaultContractAttribute(typeof(InvalidRequestMessageFaultType), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/CreateActivity/" +
            "Fault/InvalidRequestMessageFault", Name = "InvalidRequestMessageFault")]
        [System.ServiceModel.FaultContractAttribute(typeof(NotAcceptingNewActivitiesFaultType), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/CreateActivity/" +
            "Fault/NotAcceptingNewActivitiesFault", Name = "NotAcceptingNewActivitiesFault")]
        [System.ServiceModel.FaultContractAttribute(typeof(NotAuthorizedFaultType), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/CreateActivity/" +
            "Fault/NotAuthorizedFault", Name = "NotAuthorizedFault")]
        [System.ServiceModel.FaultContractAttribute(typeof(UnsupportedFeatureFaultType), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/CreateActivity/" +
            "Fault/UnsupportedFeatureFault", Name = "UnsupportedFeatureFault")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(StartAcceptingNewActivitiesResponseType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(StartAcceptingNewActivitiesType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(StopAcceptingNewActivitiesType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(StopAcceptingNewActivitiesResponseType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(HPCProfileApplication_Type))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(Envelope))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ProblemActionType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CantApplyOperationToCurrentStateFault))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(NotAuthorizedFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(InvalidRequestMessageFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(UnknownActivityIdentifierFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(NotAcceptingNewActivitiesFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(UnsupportedFeatureFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(OperationWillBeAppliedEventuallyFaultType))]
        CreateActivityResponse CreateActivity(CreateActivityRequest request);

        /// <summary>
        /// Returns the ActivityStatusResponse for the specified ActivityStatusRequest
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        [System.ServiceModel.OperationContractAttribute(Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetActivityStat" +
            "uses", ReplyAction = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetActivityStat" +
            "usesResponse")]
        [System.ServiceModel.FaultContractAttribute(typeof(NotAuthorizedFaultType), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetActivityStat" +
            "uses/Fault/NotAuthorizedFault", Name = "NotAuthorizedFault")]
        [System.ServiceModel.FaultContractAttribute(typeof(UnknownActivityIdentifierFaultType), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetActivityStat" +
            "uses/Fault/UnknownActivityIdentifierFault", Name = "UnknownActivityIdentifierFault")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(StartAcceptingNewActivitiesResponseType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(StartAcceptingNewActivitiesType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(StopAcceptingNewActivitiesType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(StopAcceptingNewActivitiesResponseType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(HPCProfileApplication_Type))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(Envelope))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ProblemActionType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CantApplyOperationToCurrentStateFault))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(NotAuthorizedFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(InvalidRequestMessageFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(UnknownActivityIdentifierFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(NotAcceptingNewActivitiesFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(UnsupportedFeatureFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(OperationWillBeAppliedEventuallyFaultType))]
        GetActivityStatusesResponse GetActivityStatuses(GetActivityStatusesRequest request);

        /// <summary>
        /// Terminates the activities specified in the TerminateActivitiesRequest
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        [System.ServiceModel.OperationContractAttribute(Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/TerminateActivi" +
            "ties", ReplyAction = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/TerminateActivi" +
            "tiesResponse")]
        [System.ServiceModel.FaultContractAttribute(typeof(object), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/TerminateActivi" +
            "ties/Fault/CantApplyOperationToCurrentStateFault", Name = "CantApplyOperationToCurrentStateFault")]
        [System.ServiceModel.FaultContractAttribute(typeof(NotAuthorizedFaultType), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/TerminateActivi" +
            "ties/Fault/NotAuthorizedFault", Name = "NotAuthorizedFault")]
        [System.ServiceModel.FaultContractAttribute(typeof(UnknownActivityIdentifierFaultType), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/TerminateActivi" +
            "ties/Fault/UnknownActivityIdentifierFault", Name = "UnknownActivityIdentifierFault")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(StartAcceptingNewActivitiesResponseType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(StartAcceptingNewActivitiesType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(StopAcceptingNewActivitiesType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(StopAcceptingNewActivitiesResponseType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(HPCProfileApplication_Type))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(Envelope))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ProblemActionType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CantApplyOperationToCurrentStateFault))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(NotAuthorizedFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(InvalidRequestMessageFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(UnknownActivityIdentifierFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(NotAcceptingNewActivitiesFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(UnsupportedFeatureFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(OperationWillBeAppliedEventuallyFaultType))]
        TerminateActivitiesResponse TerminateActivities(TerminateActivitiesRequest request);

        /// <summary>
        /// Gets the activity documents specified in the GetActivityDocumentsRequest
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        [System.ServiceModel.OperationContractAttribute(Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetActivityDocu" +
            "ments", ReplyAction = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetActivityDocu" +
            "mentsResponse")]
        [System.ServiceModel.FaultContractAttribute(typeof(NotAuthorizedFaultType), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetActivityDocu" +
            "ments/Fault/NotAuthorizedFault", Name = "NotAuthorizedFault")]
        [System.ServiceModel.FaultContractAttribute(typeof(UnknownActivityIdentifierFaultType), Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetActivityDocu" +
            "ments/Fault/UnknownActivityIdentifierFault", Name = "UnknownActivityIdentifierFault")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(StartAcceptingNewActivitiesResponseType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(StartAcceptingNewActivitiesType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(StopAcceptingNewActivitiesType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(StopAcceptingNewActivitiesResponseType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(HPCProfileApplication_Type))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(Envelope))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ProblemActionType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CantApplyOperationToCurrentStateFault))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(NotAuthorizedFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(InvalidRequestMessageFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(UnknownActivityIdentifierFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(NotAcceptingNewActivitiesFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(UnsupportedFeatureFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(OperationWillBeAppliedEventuallyFaultType))]
        GetActivityDocumentsResponse GetActivityDocuments(GetActivityDocumentsRequest request);

        /// <summary>
        /// Gets the factory attributes document specified in the GetFactoryAttributesDocumentRequest
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        [System.ServiceModel.OperationContractAttribute(Action = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetFactoryAttri" +
            "butesDocument", ReplyAction = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/GetFactoryAttri" +
            "butesDocumentResponse")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(StartAcceptingNewActivitiesResponseType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(StartAcceptingNewActivitiesType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(StopAcceptingNewActivitiesType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(StopAcceptingNewActivitiesResponseType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(HPCProfileApplication_Type))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(Envelope))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(ProblemActionType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CantApplyOperationToCurrentStateFault))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(NotAuthorizedFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(InvalidRequestMessageFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(UnknownActivityIdentifierFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(NotAcceptingNewActivitiesFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(UnsupportedFeatureFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(OperationWillBeAppliedEventuallyFaultType))]
        GetFactoryAttributesDocumentResponse GetFactoryAttributesDocument(GetFactoryAttributesDocumentRequest request);
    }
}
