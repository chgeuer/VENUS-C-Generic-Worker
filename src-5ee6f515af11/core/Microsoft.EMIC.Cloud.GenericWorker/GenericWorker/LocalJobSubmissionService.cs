//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿
namespace Microsoft.EMIC.Cloud.GenericWorker
{
    using System;
    using System.Linq;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.ServiceModel;
    using System.ServiceModel.Activation;
    using System.ServiceModel.Channels;
    using Microsoft.EMIC.Cloud.DataManagement;
    using Microsoft.EMIC.Cloud.UserAdministration;
    using Microsoft.EMIC.Cloud.Utilities;
    using OGF.BES;
    using System.Xml;
    using OGF.JDSL;

    /// <summary>
    /// Local job submission service class, implements BESFactoryPortType and IPartImportsSatisfiedNotification
    /// </summary>
    [Export(typeof(LocalJobSubmissionService))]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceBehavior(AddressFilterMode = AddressFilterMode.Any)]
    public class LocalJobSubmissionService : BESFactoryPortTypeImpl, IPartImportsSatisfiedNotification
    {
        [Import(typeof(IGWRuntimeEnvironment))]
        IGWRuntimeEnvironment RuntimeEnvironment { get; set; }

        [Import]
        ArgumentRepository ArgumentRepository { get; set; }

        void IPartImportsSatisfiedNotification.OnImportsSatisfied()
        {
            Trace.TraceInformation(string.Format("{0} - OnImportsSatisfied", typeof(LocalJobSubmissionService).Name));
        }

        /// <summary>
        /// Creates the activity.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        public override CreateActivityResponse CreateActivity(CreateActivityRequest request)
        {
            //Here check whether it is a hierhical job and its parents were cancelled
            var venusDescription = new VENUSJobDescription(request, this.ArgumentRepository);
            JobID jobid = null;
            if (JobID.TryParse(venusDescription.CustomerJobID, out jobid))
            {
                try
                {
                    this.RuntimeEnvironment.AcceptLocalJob(venusDescription.CustomerJobID);
                }
                catch(Exception ex)
                {
                    throw new FaultException<OGF.BES.Faults.InvalidRequestMessageFaultType>
                        (new OGF.BES.Faults.InvalidRequestMessageFaultType()
                        {
                            Message = ex.ToString() 
                            
                        }, new FaultReason(ExceptionMessages.ChildrenJobNotAccepted));
                }
            }

            var messageProperties = OperationContext.Current.IncomingMessageProperties;
 
            var endpointProperty = messageProperties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
            if (endpointProperty == null)
            {
                throw new NotSupportedException(string.Format(ExceptionMessages.CanNotDetermineParameters));
            }
            var connection = IPHelperAPI.GetAllTcpConnections()
                .Where(cnt => (cnt.LocalIPAddress.ToString() == endpointProperty.Address && cnt.LocalPort == endpointProperty.Port))
                .FirstOrDefault();

            if (connection == null)
            {
                throw new NotSupportedException(string.Format(ExceptionMessages.CanNotDetermineProcess,
                    endpointProperty.Address, endpointProperty.Port));
            }

            var owner = CreateProfileClient.GetActualJobOwner(connection.Username);
            Trace.TraceInformation(string.Format(ExceptionMessages.LocalUserMap, connection.Username, owner));

            return CreateVenusActivity(venusDescription, owner);
        }

        /// <summary>
        /// Creates the venus activity.
        /// </summary>
        /// <param name="venusJobDescription">The venus job description.</param>
        /// <param name="ownerClaim">The owner claim.</param>
        /// <returns></returns>
        public CreateActivityResponse CreateVenusActivity(VENUSJobDescription venusJobDescription, String ownerClaim)
        {
            var internalJobID = string.Format("job-{0:yyyy.MM.dd-HHmmss}-guid-{1}", DateTime.UtcNow, Guid.NewGuid().ToString());

            Trace.TraceInformation(string.Format("Enqueueing {0}", internalJobID));

            // The owner of the new job must be the owner of the current running job.
            // Unfortunately it is not possible to find out this owner so we have to assume
            // that the RuntimeEnvironment knows the right owner.
            String owner = ownerClaim;
            if (String.IsNullOrEmpty(owner))
            {
                owner = "anonymous";
            }

            var retries=5;
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    var job = this.RuntimeEnvironment.SubmitJob(owner, internalJobID, venusJobDescription, isLocalJob: true);
                    break;
                }
                catch (TimeoutException)
                {
                    if (i < retries)
                    {
                        System.Threading.Thread.Sleep(300);
                    }
                    else throw;
                }
            }
            return new CreateActivityResponse
            {
                CreateActivityResponse1 = new CreateActivityResponseType
                {   
                    ActivityDocument = new ActivityDocumentType
                    {
                        JobDefinition = new JobDefinition_Type
                        {
                            id = "Greetings"
                        }
                    }
                }
            };
        }
    }
}