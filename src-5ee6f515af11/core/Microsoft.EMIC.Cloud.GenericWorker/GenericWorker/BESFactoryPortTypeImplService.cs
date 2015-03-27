//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using Microsoft.IdentityModel.Tokens;

namespace Microsoft.EMIC.Cloud.GenericWorker
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Linq;
    using System.ServiceModel;
    using System.ServiceModel.Activation;
    using System.Threading;
    using System.Xml;
    using Microsoft.EMIC.Cloud.DataManagement;
    using Microsoft.IdentityModel.Claims;
    using OGF.BES;
    using Microsoft.EMIC.Cloud.Security.AuthorizationPolicy;
    using Microsoft.EMIC.Cloud.Security;
    using System.Security;
    using OGF.BES.Faults;
    using System.Threading.Tasks;
    using System.Reflection;

    /// <summary>
    /// The core job submission service business logic for the generic worker. 
    /// </summary>
    [Export(typeof(BESFactoryPortTypeImplService))]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceBehavior(AddressFilterMode = AddressFilterMode.Any, Namespace = CompositionIdentifiers.OGFTargetNamespace)]
    public class BESFactoryPortTypeImplService : BESFactoryPortTypeImpl, IPartImportsSatisfiedNotification
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BESFactoryPortTypeImplService"/> class.
        /// </summary>
        public BESFactoryPortTypeImplService() { }

        /// <summary>
        /// The runtime environment of the underlying compute infrastructure. 
        /// </summary>
        [Import(typeof(IGWRuntimeEnvironment))]
        IGWRuntimeEnvironment RuntimeEnvironment { get; set; }

        /// <summary>
        /// The <see cref="ArgumentRepository"/> to de-serialize <see cref="IArgument"/> objects.
        /// </summary>
        [Import]
        ArgumentRepository ArgumentRepository { get; set; }

        /// <summary>
        /// Set or get whether the insecure access will be allowed
        /// </summary>
        [Import(CompositionIdentifiers.SecurityAllowInsecureAccess)]
        public bool SecurityAllowInsecureAccess { get; set; }

        /// <summary>
        /// Set or get the default number of jobs used by the paging mechanism
        /// </summary>
        [Import(CompositionIdentifiers.JobEntriesPerPage)]
        public int JobEntriesPerPage { get; set; }


        /// <summary>
        /// Called when a part's imports have been satisfied and it is safe to use.
        /// </summary>
        void IPartImportsSatisfiedNotification.OnImportsSatisfied()
        {
            Trace.TraceInformation("GenericWorkerSubmissionService - OnImportsSatisfied");
        }

        /// <summary>
        /// Creates the activity.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        public override CreateActivityResponse CreateActivity(CreateActivityRequest request)
        {
            try
            {
                return this.CreateVenusActivity(new VENUSJobDescription(request, this.ArgumentRepository));

            }
            catch (TimeoutException tex)
            {
                throw new FaultException<OGF.BES.Faults.NotAcceptingNewActivitiesFaultType>
                    (new OGF.BES.Faults.NotAcceptingNewActivitiesFaultType()
                    {
                        
                    }, new FaultReason(String.Format(ExceptionMessages.ServiceBusy, tex.ToString())));
            }
            catch (InvalidSecurityException iex)
            {
                throw new FaultException<OGF.BES.Faults.NotAuthorizedFaultType>
                       (new OGF.BES.Faults.NotAuthorizedFaultType()
                       {

                       }, new FaultReason(String.Format(ExceptionMessages.UnauthenticatedCall, iex.ToString())));
            }
            catch (Exception ex)
            {
                throw new FaultException<OGF.BES.Faults.InvalidRequestMessageFaultType>
                       (new OGF.BES.Faults.InvalidRequestMessageFaultType()
                       {
                           Message = ex.ToString()
                       }, new FaultReason(ExceptionMessages.JobNotAccepted));
            }
        }

        /// <summary>
        /// Creates the venus activity.
        /// </summary>
        /// <param name="venusJobDescription">The venus job description.</param>
        /// <returns></returns>
        private CreateActivityResponse CreateVenusActivity(VENUSJobDescription venusJobDescription)
        {
            string internalJobID = string.Format("job-{0:yyyy.MM.dd-HHmmss}-guid-{1}", DateTime.UtcNow, Guid.NewGuid().ToString());

            Trace.TraceInformation(string.Format("Enqueueing {0}", internalJobID));

            var ici = Thread.CurrentPrincipal.Identity as IClaimsIdentity;
            if ((ici == null && !this.SecurityAllowInsecureAccess) || (ici != null && String.IsNullOrEmpty(ici.Name) && !this.SecurityAllowInsecureAccess))
            {
                throw new InvalidSecurityException(String.Format(ExceptionMessages.UnauthenticatedCall, "Thread.CurrentPrincipal.Identity is either null or you are not allowed for this operation"));
            }

            var assem = Assembly.GetExecutingAssembly();
            var versionService = assem.GetName().Version.ToString();

            if (venusJobDescription.ClientVersion == null ||
                !venusJobDescription.ClientVersion.Equals(versionService))
            {
                if (venusJobDescription.ClientVersion == null)
                {
                    venusJobDescription.ClientVersion = "older";
                }
                var message=string.Format("!!! Only job management clients with version {0} are supported. Your's is {1}. For your client application you should use the sdk assemblies that come together with your deployment package.", versionService, venusJobDescription.ClientVersion);
                Trace.TraceError(message); //Todo: Later we can throw a NotSupported Exception here.
            }
            string owner = ici != null && !String.IsNullOrEmpty(ici.Name) ? ici.Name : "anonymous";

            Trace.TraceInformation(string.Format("Received job from owner \"{0}\"", owner));

            IJob job = this.RuntimeEnvironment.SubmitJob(owner, internalJobID, venusJobDescription);

            var jobIdAsEpr = new JobIdAsEPR(internalJobID);

            return new CreateActivityResponse
            {
                CreateActivityResponse1 = new CreateActivityResponseType
                {
                    ActivityIdentifier = new EndpointReferenceType
                    {
                        Address = new AttributedURIType
                        {
                            Value = OperationContext.Current.IncomingMessageHeaders.To.AbsoluteUri
                        },
                        ReferenceParameters = new ReferenceParametersType
                        {
                            Any = new List<XmlElement> { jobIdAsEpr.AsXmlElement() }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Gets the activity statuses.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        public override GetActivityStatusesResponse GetActivityStatuses(GetActivityStatusesRequest request)
        {
            var response = new GetActivityStatusesResponse
            {
                GetActivityStatusesResponse1 = new GetActivityStatusesResponseType
                {
                    Response = new List<GetActivityStatusResponseType>()
                }
            };


            if (request != null && request.GetActivityStatuses != null && request.GetActivityStatuses.ActivityIdentifier != null)
            {
                foreach (var endPointReference in request.GetActivityStatuses.ActivityIdentifier)
                {
                    response.GetActivityStatusesResponse1.Response.Add(GetActivityStatus(endPointReference));
                }
            }

            return response;

        }

        /// <summary>
        /// Gets the activity status.
        /// </summary>
        /// <param name="endPoint">The end point.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        private GetActivityStatusResponseType GetActivityStatus(EndpointReferenceType endPoint)
        {
            var response = new GetActivityStatusResponseType { ActivityIdentifier = endPoint };
            try
            {
                var jobIdAsEPR = JobIdAsEPR.Load(endPoint, OperationContext.Current.IncomingMessageHeaders.To.AbsoluteUri, this.RuntimeEnvironment);
                if (jobIdAsEPR == null) throw new ArgumentNullException();

                var job = this.RuntimeEnvironment.GetJobByID(jobIdAsEPR.Value);
                var owner = job.Owner;
                throwOnInvalidOwner(owner);
                response.ActivityStatus = new ActivityStatusType();

                switch (job.Status)
                {
                    case JobStatus.Cancelled:
                        response.ActivityStatus.state = ActivityStateEnumeration.Cancelled;
                        break;
                    case JobStatus.CancelRequested:
                        response.ActivityStatus.state = job.GetPreviousStatusForCancelRequestedJob() == JobStatus.Submitted ? ActivityStateEnumeration.Pending : ActivityStateEnumeration.Running;
                        break;
                    case JobStatus.CheckingInputData:
                        response.ActivityStatus.state = ActivityStateEnumeration.Pending;
                        break;
                    case JobStatus.Failed:
                        response.ActivityStatus.state = ActivityStateEnumeration.Failed;
                        break;
                    case JobStatus.Finished:
                        response.ActivityStatus.state = ActivityStateEnumeration.Finished;
                        break;
                    case JobStatus.Running:
                        response.ActivityStatus.state = ActivityStateEnumeration.Running;
                        break;
                    case JobStatus.Submitted:
                        response.ActivityStatus.state = ActivityStateEnumeration.Pending;
                        break;
                }
            }
            catch (ArgumentNullException)
            {
                response.Fault = new Fault
                {
                    faultstring = String.Format(ExceptionMessages.JobDoesNotExistError, endPoint.Address)
                };
            }
            catch (InvalidSecurityException iex)
            {
                response.Fault = new Fault
                {
                    faultstring = String.Format(ExceptionMessages.UnauthenticatedCall, iex.ToString())
                };
            }
            catch (Exception ex)
            {
                response.Fault = new Fault
                {
                    faultstring = String.Format(ExceptionMessages.GeneralRequestError, "GetActivityStatus", ex.ToString())
                };
            }

            return response;
        }


        /// <summary>
        /// Terminates the activities.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        public override TerminateActivitiesResponse TerminateActivities(TerminateActivitiesRequest request)
        {
            var response = new TerminateActivitiesResponse
            {
                TerminateActivitiesResponse1 = new TerminateActivitiesResponseType
                {
                    Response = new List<TerminateActivityResponseType>()
                }
            };
            if (request != null && request.TerminateActivities != null && request.TerminateActivities.ActivityIdentifier != null)
            {
                foreach (var endPointReference in request.TerminateActivities.ActivityIdentifier)
                {
                    response.TerminateActivitiesResponse1.Response.Add(TerminateActivity(endPointReference));
                }
            }

            return response;
        }

        /// <summary>
        /// Terminates the activity.
        /// </summary>
        /// <param name="endPoint">The end point.</param>
        /// <returns></returns>
        private TerminateActivityResponseType TerminateActivity(EndpointReferenceType endPoint)
        {
            var response = new TerminateActivityResponseType
                               {
                                   ActivityIdentifier = endPoint
                               };
            try
            {
                JobIdAsEPR jobIdAsEPR = JobIdAsEPR.Load(endPoint, OperationContext.Current.IncomingMessageHeaders.To.AbsoluteUri, this.RuntimeEnvironment);
                if (jobIdAsEPR == null) throw new ArgumentNullException();

                var job = this.RuntimeEnvironment.GetJobByID(jobIdAsEPR.Value);
                throwOnInvalidOwner(job.Owner); //security exceptions generated here are catched and rethrown by first catch clause
                if (!((job.Status == JobStatus.Running && this.RuntimeEnvironment.MarkJobAsCancellationPending(job))
                    ||
                   (job.Status != JobStatus.Running && job.Status != JobStatus.CancelRequested && this.RuntimeEnvironment.MarkJobAsCancelled(job, "job canceled"))))
                {
                    throw new InvalidOperationException(String.Format(ExceptionMessages.TerminateActivityStateError, job.Status));
                }

                var job2 = this.RuntimeEnvironment.GetJobByID(jobIdAsEPR.Value);

                switch (job2.Status)
                {
                    case JobStatus.Cancelled:
                        response.Terminated = true;
                        break;
                    default:
                        response.Terminated = false;
                        break;
                }
            }
            catch (ArgumentNullException)
            {
                response.Fault = new Fault
                {
                    faultstring = String.Format(ExceptionMessages.JobDoesNotExistError, endPoint.Address)
                };
            }
            catch (InvalidOperationException ioex)
            {
                throw new FaultException<OGF.BES.Faults.CantApplyOperationToCurrentStateFaultType>
                         (new OGF.BES.Faults.CantApplyOperationToCurrentStateFaultType()
                         {

                         }, new FaultReason(ioex.ToString()));
            }
            catch (InvalidSecurityException iex)
            {
                response.Fault = new Fault
                {
                    faultstring = String.Format(ExceptionMessages.UnauthenticatedCall, iex.ToString())
                };
            }
            catch (Exception ex)
            {
                response.Fault = new Fault
                {
                    faultstring = String.Format(ExceptionMessages.GeneralRequestError, "TerminateActivity", ex.ToString())
                };
            }

            return response;
        }

        /// <summary>
        /// Gets the activity documents.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        public override GetActivityDocumentsResponse GetActivityDocuments(GetActivityDocumentsRequest request)
        {
            var response = new GetActivityDocumentsResponse
            {
                GetActivityDocumentsResponse1 = new GetActivityDocumentsResponseType
                {
                    Response = new List<GetActivityDocumentResponseType>()
                }
            };
            if (request != null && request.GetActivityDocuments != null && request.GetActivityDocuments.ActivityIdentifier != null)
            {
                foreach (var endPointReference in request.GetActivityDocuments.ActivityIdentifier)
                {
                    response.GetActivityDocumentsResponse1.Response.Add(GetActivityDocument(endPointReference));
                }
            }

            return response;
        }

        /// <summary>
        /// Gets the activity document.
        /// </summary>
        /// <param name="endPoint">The end point.</param>
        /// <returns></returns>
        private GetActivityDocumentResponseType GetActivityDocument(EndpointReferenceType endPoint)
        {
            var response = new GetActivityDocumentResponseType
                               {
                                   ActivityIdentifier = endPoint
                               };
            try
            {
                JobIdAsEPR jobIdAsEPR = JobIdAsEPR.Load(endPoint, OperationContext.Current.IncomingMessageHeaders.To.AbsoluteUri, this.RuntimeEnvironment);
                if (jobIdAsEPR == null) throw new ArgumentNullException();

                var job = this.RuntimeEnvironment.GetJobByID(jobIdAsEPR.Value);
                var owner = job.Owner;
                throwOnInvalidOwner(owner);

                response.JobDefinition = job.GetVENUSJobDescription().GetCreateActivity().CreateActivity.ActivityDocument.JobDefinition;
            }
            catch (ArgumentNullException)
            {
                response.Fault = new Fault
                {
                    faultstring = String.Format(ExceptionMessages.JobDoesNotExistError, endPoint.Address)
                };
            }
            catch (InvalidSecurityException iex)
            {
                response.Fault = new Fault
                {
                    faultstring = String.Format(ExceptionMessages.UnauthenticatedCall, iex.ToString())
                };
            }
            catch (Exception ex)
            {
                response.Fault = new Fault
                {
                    faultstring = String.Format(ExceptionMessages.GeneralRequestError, "GetActivityDocument", ex.ToString())
                };
            }

            return response;
        }

        private List<EndpointReferenceType> GetEprList(IEnumerable<IJob> jobs)
        {
            var eprs = new List<EndpointReferenceType>();

            foreach (var job in jobs)
            {
                var jobIdAsEpr = new JobIdAsEPR(job.InternalJobID);
                var epr = new EndpointReferenceType();
                epr.Address = new AttributedURIType
                {
                    Value = OperationContext.Current.IncomingMessageHeaders.To.AbsoluteUri
                };
                epr.ReferenceParameters = new ReferenceParametersType
                {
                    Any = new List<XmlElement> { jobIdAsEpr.AsXmlElement() }
                };
                eprs.Add(epr);
            }
            return eprs;
        }

        private void throwOnInvalidOwner(string owner)
        {
            var ici = Thread.CurrentPrincipal.Identity as IClaimsIdentity;

            // If security is enabled, the name claims has to be prooved against the owner
            // If security is disabled, but a secure client is used, then the proove has to be made too
            if (!this.SecurityAllowInsecureAccess || (ici != null && !String.IsNullOrEmpty(ici.Name)))
            {
                if (ici == null)
                    throw new InvalidSecurityException(String.Format(ExceptionMessages.UnauthenticatedCall, "Thread.CurrentPrincipal.Identity is null"));

                var adminRole = new WrappedClaim(VenusClaimsAuthorizationManager.Roles.ComputeAdministrator) as IClaimRequirement;
                var isAdmin = adminRole.IsSatisfiedBy(ici.Claims);

                if (!isAdmin && ici.Name != owner)
                    throw new InvalidSecurityException(String.Format(ExceptionMessages.UnauthenticatedCall, "You are not allowed for this operation"));
            }
        }

        /// <summary>
        /// Get all jobs for the given owner
        /// </summary>
        /// <param name="owner">Owner of the of jobs</param>
        /// <param name="page">Paging mechanism is implemented to split the return size. The number of page is needed.</param>
        /// <returns>List of unique identifiers of the jobs</returns>
        public override List<EndpointReferenceType> GetJobs(string owner, int page)
        {
            try
            {
                throwOnInvalidOwner(owner);
                var jobs = this.RuntimeEnvironment.AllJobs.Where(j => j.Owner == owner && !JobID.IsGroupHead(j.CustomerJobID));
                return GetJobListPage(page, ref jobs);
            }
            catch (InvalidSecurityException iex)
            {
                throw new FaultException<OGF.BES.Faults.NotAuthorizedFaultType>
                       (new OGF.BES.Faults.NotAuthorizedFaultType()
                       {

                       }, new FaultReason(ExceptionMessages.UnauthenticatedCall + " " + iex.ToString()));
            }
            catch (Exception ex)
            {
                throw new FaultException<OGF.BES.Faults.Fault>(new OGF.BES.Faults.Fault()
                {
                },new FaultReason(String.Format(ExceptionMessages.GeneralRequestError, "GetJobs", ex.ToString())));
            }
        }

        /// <summary>
        /// Get all the jobs in the system
        /// </summary>
        /// <param name="page">Paging mechanism is implemented to split the return size. The number of page is needed.</param>
        /// <returns>List of unique identifiers of the jobs</returns>
        public override List<EndpointReferenceType> GetAllJobs(int page)
        {
            try
            {
                var jobs = this.RuntimeEnvironment.AllJobs.Where(j => !JobID.IsGroupHead(j.CustomerJobID));
                return GetJobListPage(page, ref jobs);
            }
            catch (Exception ex)
            {
                throw new FaultException<OGF.BES.Faults.Fault>(new OGF.BES.Faults.Fault()
                {
                }, new FaultReason(String.Format(ExceptionMessages.GeneralRequestError, "GetAllJobs", ex.ToString())));
            }
        }

        private List<EndpointReferenceType> GetJobListPage(int page, ref IEnumerable<IJob> jobs)
        {
            if (page >= 0)
            {
                if (page * JobEntriesPerPage > jobs.Count())
                {
                    return new List<EndpointReferenceType>();
                }
                jobs = jobs.Skip(page * JobEntriesPerPage).Take(JobEntriesPerPage);
            }
            return GetEprList(jobs);
        }

        /// <summary>
        /// Get the list of the jobs (hieararchy) under the root
        /// </summary>
        /// <param name="root">Unique identifier of the root job</param>
        /// <param name="page">Paging mechanism is implemented to split the return size. The number of page is needed.</param>
        /// <returns>List of unique identifiers of the jobs in the hieararchy</returns>
        public override List<EndpointReferenceType> GetHierarchy(EndpointReferenceType root, int page)
        {
            try
            {
                var jobIdAsEPR = JobIdAsEPR.Load(root, OperationContext.Current.IncomingMessageHeaders.To.AbsoluteUri, this.RuntimeEnvironment);
                if (jobIdAsEPR == null) throw new ArgumentNullException();

                var job = this.RuntimeEnvironment.GetJobByID(jobIdAsEPR.Value);
                var owner = job.Owner;
                throwOnInvalidOwner(owner);
                var jobs = this.RuntimeEnvironment.GetJobHierarchyByRoot(job.InternalJobID).AsEnumerable();
                return GetJobListPage(page, ref jobs);
            }
            catch (ArgumentNullException)
            {
                throw new FaultException<OGF.BES.Faults.UnknownActivityIdentifierFaultType>
                      (new OGF.BES.Faults.UnknownActivityIdentifierFaultType()
                      {

                      }, new FaultReason(String.Format(ExceptionMessages.JobDoesNotExistError, root.Address)));
            }
            catch (InvalidSecurityException iex)
            {
                throw new FaultException<OGF.BES.Faults.NotAuthorizedFaultType>
                       (new OGF.BES.Faults.NotAuthorizedFaultType()
                       {

                       }, new FaultReason(String.Format(ExceptionMessages.UnauthenticatedCall, iex.ToString())));
            }
            catch (Exception ex)
            {
                throw new FaultException<OGF.BES.Faults.Fault>(new OGF.BES.Faults.Fault()
                {
                }, new FaultReason(String.Format(ExceptionMessages.GeneralRequestError, "GetHierarchy", ex.ToString())));
            }
        }

        /// <summary>
        /// Get the root job of the given job 
        /// </summary>
        /// <param name="leaf">Unique identifier of the job in the hieararchy</param>
        /// <returns>Unique identifier of the root job</returns>
        public override EndpointReferenceType GetRoot(EndpointReferenceType leaf)
        {
            try
            {
                var jobIdAsEPR = JobIdAsEPR.Load(leaf, OperationContext.Current.IncomingMessageHeaders.To.AbsoluteUri, this.RuntimeEnvironment);
                if (jobIdAsEPR == null) throw new ArgumentNullException();

                var rootJob = this.RuntimeEnvironment.GetRootofTheHierarchy(jobIdAsEPR.Value);
                var owner = rootJob.Owner;
                throwOnInvalidOwner(owner);

                var jobIdAsEpr = new JobIdAsEPR(rootJob.InternalJobID);

                return new EndpointReferenceType
                {
                    Address = new AttributedURIType
                    {
                        Value = OperationContext.Current.IncomingMessageHeaders.To.AbsoluteUri
                    },
                    ReferenceParameters = new ReferenceParametersType
                    {
                        Any = new List<XmlElement> { jobIdAsEpr.AsXmlElement() }
                    }
                };
            }
            catch (ArgumentNullException)
            {
                throw new FaultException<OGF.BES.Faults.UnknownActivityIdentifierFaultType>
                      (new OGF.BES.Faults.UnknownActivityIdentifierFaultType()
                      {

                      }, new FaultReason(String.Format(ExceptionMessages.JobDoesNotExistError, leaf.Address)));
            }
            catch (InvalidSecurityException iex)
            {
                throw new FaultException<OGF.BES.Faults.NotAuthorizedFaultType>
                      (new OGF.BES.Faults.NotAuthorizedFaultType()
                      {

                      }, new FaultReason(String.Format(ExceptionMessages.UnauthenticatedCall, iex.ToString())));
            }
            catch (Exception ex)
            {
                throw new FaultException<OGF.BES.Faults.Fault>(new OGF.BES.Faults.Fault()
                {
                }, new FaultReason(String.Format(ExceptionMessages.GeneralRequestError, "GetRoot", ex.ToString())));
            }
        }

        /// <summary>
        /// Gets all the jobs in the given group
        /// </summary>
        /// <param name="owner">Owner of the group</param>
        /// <param name="groupName">Name of the group</param>
        /// <param name="page">Paging mechanism is implemented to split the return size. The number of page is needed.</param>
        /// <returns>List of unique identifiers of the jobs in the group</returns>
        public override List<EndpointReferenceType> GetJobsByGroup(string owner, string groupName, int page)
        {
            try
            {
                throwOnInvalidOwner(owner);
                var jobs = this.RuntimeEnvironment.GetJobGroupByOwnerGroupName(owner, groupName).AsEnumerable();
                return GetJobListPage(page, ref jobs);
            }
            catch (InvalidSecurityException iex)
            {
                throw new FaultException<OGF.BES.Faults.NotAuthorizedFaultType>
                     (new OGF.BES.Faults.NotAuthorizedFaultType()
                     {

                     }, new FaultReason(String.Format(ExceptionMessages.UnauthenticatedCall, iex.ToString())));
            }
            catch (Exception ex)
            {
                throw new FaultException<OGF.BES.Faults.Fault>(new OGF.BES.Faults.Fault()
                {
                }, new FaultReason(String.Format(ExceptionMessages.GeneralRequestError, "GetJobsByGroup", ex.ToString())));
            }
        }

        /// <summary>
        /// Cancel all the jobs in the group
        /// </summary>
        /// <param name="owner">Owner of the group</param>
        /// <param name="groupName">Name of the group</param>
        public override void CancelGroup(string owner, string groupName)
        {
            try
            {
                throwOnInvalidOwner(owner);
                if (!ThreadPool.QueueUserWorkItem(delegate { this.RuntimeEnvironment.CancelGroup(owner, groupName); }))
                {
                    throw new FaultException<OGF.BES.Faults.Fault>(new OGF.BES.Faults.Fault()
                    {
                    }, new FaultReason(ExceptionMessages.CancelGroupQueue));
                }
            }
            catch (InvalidSecurityException iex)
            {
                throw new FaultException<OGF.BES.Faults.NotAuthorizedFaultType>
                    (new OGF.BES.Faults.NotAuthorizedFaultType()
                    {

                    }, new FaultReason(String.Format(ExceptionMessages.UnauthenticatedCall, iex.ToString())));
            }
        }

        /// <summary>
        /// Cancel all the jobs in the hieararchy under the given root
        /// </summary>
        /// <param name="root">Unique identifier of the root job</param>
        public override void CancelHierarchy(EndpointReferenceType root)
        {
            try
            {
                var jobIdAsEPR = JobIdAsEPR.Load(root, OperationContext.Current.IncomingMessageHeaders.To.AbsoluteUri, this.RuntimeEnvironment);
                if (jobIdAsEPR == null) throw new ArgumentNullException();

                var job = this.RuntimeEnvironment.GetJobByID(jobIdAsEPR.Value);
                var owner = job.Owner;
                throwOnInvalidOwner(owner);

                if (!ThreadPool.QueueUserWorkItem(delegate { this.RuntimeEnvironment.CancelHierarchy(job.InternalJobID); }))
                {
                    throw new FaultException(ExceptionMessages.CancelHierarchyQueue);
                }
            }
            catch (ArgumentNullException)
            {
                throw new FaultException<OGF.BES.Faults.UnknownActivityIdentifierFaultType>
                     (new OGF.BES.Faults.UnknownActivityIdentifierFaultType()
                     {

                     }, new FaultReason(String.Format(ExceptionMessages.JobDoesNotExistError, root.Address)));
            }
            catch (InvalidSecurityException iex)
            {
                throw new FaultException<OGF.BES.Faults.NotAuthorizedFaultType>
                    (new OGF.BES.Faults.NotAuthorizedFaultType()
                    {

                    }, new FaultReason(String.Format(ExceptionMessages.UnauthenticatedCall, iex.ToString())));
            }
            catch (Exception ex)
            {
                throw new FaultException<OGF.BES.Faults.Fault>(new OGF.BES.Faults.Fault()
                {
                }, new FaultReason(String.Format(ExceptionMessages.GeneralRequestError, "CancelHierarchy", ex.ToString())));
            }

        }

        /// <summary>
        /// Remove the terminated jobs from the tables
        /// </summary>
        /// <param name="owner">Owner of the group</param>
        public override void RemoveTerminatedJobs(string owner)
        {
            try
            {
                throwOnInvalidOwner(owner);
                if (!ThreadPool.QueueUserWorkItem(delegate { this.RuntimeEnvironment.DeleteTerminatedJobs(owner); }))
                {
                    throw new FaultException<OGF.BES.Faults.Fault>(new OGF.BES.Faults.Fault()
                    {
                    }, new FaultReason(ExceptionMessages.RemoveTerminatedJobsQueue));
                }
            }
            catch (InvalidSecurityException iex)
            {
                throw new FaultException<OGF.BES.Faults.NotAuthorizedFaultType>
                       (new OGF.BES.Faults.NotAuthorizedFaultType()
                       {

                       }, new FaultReason(String.Format(ExceptionMessages.UnauthenticatedCall, iex.ToString())));
            }
        }

        /// <summary>
        /// Get the number of the jobs of the given owner and statuses
        /// </summary>
        /// <param name="owner">Owner of the jobs</param>
        /// <param name="statuses">Statuses asked for</param>
        /// <returns>Number of the jobs</returns>
        public override int GetNumberOfJobs(string owner, List<JobStatus> statuses)
        {
            try
            {
                throwOnInvalidOwner(owner);
                var numJobs = this.RuntimeEnvironment.AllJobsOfOwner(owner).Where(j => statuses.Contains(j.Status)).Select(j => j.InternalJobID).Count();
                return numJobs;
            }
            catch (InvalidSecurityException iex)
            {
                throw new FaultException<OGF.BES.Faults.NotAuthorizedFaultType>
                       (new OGF.BES.Faults.NotAuthorizedFaultType()
                       {

                       }, new FaultReason(String.Format(ExceptionMessages.UnauthenticatedCall, iex.ToString())));
            }
            catch (Exception ex)
            {
                throw new FaultException<OGF.BES.Faults.Fault>(new OGF.BES.Faults.Fault()
                {
                }, new FaultReason(String.Format(ExceptionMessages.GeneralRequestError, "GetNumberOfJobs", ex.ToString())));

            }
        }
    }
}