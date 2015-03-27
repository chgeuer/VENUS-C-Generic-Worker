//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.ServiceModel.Activation;
using System.ServiceModel;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Threading;
using Microsoft.EMIC.Cloud.Security.AuthorizationPolicy;
using Microsoft.EMIC.Cloud.Security;
using Microsoft.IdentityModel.Claims;
using System.Security;
using Microsoft.EMIC.Cloud.Notification;
using OGF.BES;


namespace Microsoft.EMIC.Cloud.GenericWorker
{
    /// <summary>
    /// Represents a subscription ( a list of job statuses and a pluginconfig that has to be provided to the corresponding notification plugin)
    /// </summary>
    public class Subscription
    {
        /// <summary>
        /// Gets or sets the statuses.
        /// </summary>
        /// <value>
        /// The statuses.
        /// </value>
        public List<JobStatus> Statuses { get; set; }
        /// <summary>
        /// Gets or sets the plugin config (a collection of key/value pairs representing the configuration of an arbitary notification plugin).
        /// </summary>
        /// <value>
        /// The plugin config.
        /// </value>
        public List<SerializableKeyValuePair<string, string>> PluginConfig { get; set; }
    }


    /// <summary>
    /// The implementation of the notification service.
    /// </summary>
    [Export(typeof(NotificationService))]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceBehavior(AddressFilterMode = AddressFilterMode.Any)]
    public class NotificationService : INotificationService
    {

        /// <summary>
        /// Gets or sets the runtime environment.
        /// </summary>
        [Import(typeof(IGWRuntimeEnvironment), RequiredCreationPolicy = CreationPolicy.NonShared)]
        public IGWRuntimeEnvironment RuntimeEnvironment { get; set; }

        /// <summary>
        /// Property to allow/deny access through an unprotected client
        /// </summary>
        [Import(CompositionIdentifiers.SecurityAllowInsecureAccess)]
        public bool SecurityAllowInsecureAccess { get; set; }

        /// <summary>
        /// Creates the subscription on a job for a given set of statuses.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="statuses">The statuses.</param>
        /// <param name="pluginConfig">The plugin config.</param>
        public void CreateSubscriptionForStatuses(EndpointReferenceType job, List<JobStatus> statuses, List<SerializableKeyValuePair<string, string>> pluginConfig)
        {
            if (job == null)
            {
                throw new FaultException<OGF.BES.Faults.InvalidRequestMessageFaultType>
                          (new OGF.BES.Faults.InvalidRequestMessageFaultType()
                          {

                          }, new FaultReason(ExceptionMessages.ProvideValidJobReference));
            }
            if (statuses == null || statuses.Count==0)
            {
                throw new FaultException<OGF.BES.Faults.InvalidRequestMessageFaultType>
                          (new OGF.BES.Faults.InvalidRequestMessageFaultType()
                          {

                          }, new FaultReason(ExceptionMessages.DefineStatuses));
            }
            if (pluginConfig == null || pluginConfig.Count == 0)
            {
                throw new FaultException<OGF.BES.Faults.InvalidRequestMessageFaultType>
                          (new OGF.BES.Faults.InvalidRequestMessageFaultType()
                          {

                          }, new FaultReason(ExceptionMessages.ProvidePluginConfig));
            }

            //add subscription owner to xml
            var newSubscription = new Subscription() { Statuses = statuses, PluginConfig = pluginConfig };

            var internalJobId = job.ReferenceParameters.Any[0].InnerText;
            var rtJob = this.RuntimeEnvironment.GetJobByID(internalJobId);
            if (rtJob == null)
            {
                throw new FaultException<OGF.BES.Faults.InvalidRequestMessageFaultType>
                          (new OGF.BES.Faults.InvalidRequestMessageFaultType()
                          {

                          }, new FaultReason(ExceptionMessages.JobNotFound));
            }

            if (!this.SecurityAllowInsecureAccess)
            {
                var ici = Thread.CurrentPrincipal.Identity as IClaimsIdentity;
                if (ici == null)
                {
                    throw new FaultException<OGF.BES.Faults.NotAuthorizedFaultType>
                          (new OGF.BES.Faults.NotAuthorizedFaultType()
                          {

                          }, new FaultReason(ExceptionMessages.IdentityNotRetrieved));
                }

                if (ici.Name != rtJob.Owner)
                {
                    throw new FaultException<OGF.BES.Faults.NotAuthorizedFaultType>
                          (new OGF.BES.Faults.NotAuthorizedFaultType()
                          {

                          }, new FaultReason(ExceptionMessages.NameClaimNotMatching));
                }
            }
            if (rtJob.Status.HasEnded())
            {
                throw new FaultException<OGF.BES.Faults.CantApplyOperationToCurrentStateFaultType>
                          (new OGF.BES.Faults.CantApplyOperationToCurrentStateFaultType()
                          {

                          }, new FaultReason(ExceptionMessages.JobAlreadyTerminated));
            }
           
            try
            {
                 RuntimeEnvironment.Subscribe(rtJob, newSubscription);
            }
            catch(Exception ex)
            {
                throw new FaultException<OGF.BES.Faults.Fault>
                          (new OGF.BES.Faults.Fault()
                          {

                          }, new FaultReason(String.Format(ExceptionMessages.GeneralRequestError, "CreateSubscriptionForStatuses", ex.ToString())));
            }
        }

        /// <summary>
        /// Creates the subscription on a job for all statuses.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="pluginConfig">The plugin config.</param>
        public void CreateSubscription(EndpointReferenceType job, List<SerializableKeyValuePair<string, string>> pluginConfig)
        {
            var statuses = new List<JobStatus>();
            foreach (var e in Enum.GetValues(typeof(JobStatus)))
            {
                statuses.Add((JobStatus)e);
            }
            CreateSubscriptionForStatuses(job, statuses, pluginConfig);
        }

        /// <summary>
        /// Unsubscribes notification for the specified job.
        /// </summary>
        /// <param name="job">The job.</param>
        public void Unsubscribe(EndpointReferenceType job)
        {
            if (job == null)
            {
                throw new FaultException<OGF.BES.Faults.InvalidRequestMessageFaultType>
                          (new OGF.BES.Faults.InvalidRequestMessageFaultType()
                          {

                          }, new FaultReason(ExceptionMessages.ProvideValidJobReference));
            }
            var internalJobId = job.ReferenceParameters.Any[0].InnerText;
            var rtJob = this.RuntimeEnvironment.GetJobByID(internalJobId);
            if (rtJob == null)
            {
                throw new FaultException<OGF.BES.Faults.InvalidRequestMessageFaultType>
                          (new OGF.BES.Faults.InvalidRequestMessageFaultType()
                          {

                          }, new FaultReason(ExceptionMessages.JobNotFound));
            }
            if (!this.SecurityAllowInsecureAccess)
            {
                var ici = Thread.CurrentPrincipal.Identity as IClaimsIdentity;
                if (ici == null)
                {
                    throw new FaultException<OGF.BES.Faults.NotAuthorizedFaultType>
                          (new OGF.BES.Faults.NotAuthorizedFaultType()
                          {

                          }, new FaultReason(ExceptionMessages.IdentityNotRetrieved));
                }

                var adminRole = new WrappedClaim(VenusClaimsAuthorizationManager.Roles.ComputeAdministrator) as IClaimRequirement;
                var isAdmin = adminRole.IsSatisfiedBy(ici.Claims);

                if (!isAdmin && ici.Name != rtJob.Owner)
                {
                    throw new FaultException<OGF.BES.Faults.NotAuthorizedFaultType>
                          (new OGF.BES.Faults.NotAuthorizedFaultType()
                          {

                          }, new FaultReason(ExceptionMessages.NameClaimNotMatching));
                }
            }
            try
            {
                this.RuntimeEnvironment.UnSubscribe(rtJob);
            }
            catch (Exception ex)
            {
                throw new FaultException<OGF.BES.Faults.Fault>
                          (new OGF.BES.Faults.Fault()
                          {

                          }, new FaultReason(String.Format(ExceptionMessages.GeneralRequestError, "Unsubscribe", ex.ToString())));
            }

        }

        /// <summary>
        /// Creates a notification subscription for a given group of jobs on a given set of statuses.
        /// </summary>
        /// <param name="groupName">Name of the group.</param>
        /// <param name="statuses">The statuses.</param>
        /// <param name="pluginConfig">The plugin config.</param>
        public void CreateSubscriptionForGroupStatuses(string groupName, List<JobStatus> statuses, List<SerializableKeyValuePair<string, string>> pluginConfig)
        {
            if (statuses == null || statuses.Count == 0)
            {
                throw new FaultException<OGF.BES.Faults.InvalidRequestMessageFaultType>
                          (new OGF.BES.Faults.InvalidRequestMessageFaultType()
                          {

                          }, new FaultReason(ExceptionMessages.DefineStatuses));
            }
            if (pluginConfig == null || pluginConfig.Count == 0)
            {
                throw new FaultException<OGF.BES.Faults.InvalidRequestMessageFaultType>
                          (new OGF.BES.Faults.InvalidRequestMessageFaultType()
                          {

                          }, new FaultReason(ExceptionMessages.ProvidePluginConfig));
            }
            if (statuses.Distinct().ToList().Count < statuses.Count)
            {
                throw new FaultException<OGF.BES.Faults.InvalidRequestMessageFaultType>
                          (new OGF.BES.Faults.InvalidRequestMessageFaultType()
                          {

                          }, new FaultReason(ExceptionMessages.JobStatusMoreThanOnce));
            }
            if (statuses.Where(s => s != JobStatus.Finished && s != JobStatus.Failed).Count() > 0)
            {
                throw new FaultException<OGF.BES.Faults.InvalidRequestMessageFaultType>
                          (new OGF.BES.Faults.InvalidRequestMessageFaultType()
                          {

                          }, new FaultReason(ExceptionMessages.NotificationOnlyFinishedFailed));
            }

            var newSubscription = new Subscription() { Statuses = statuses, PluginConfig = pluginConfig };
            string currentUser;
            if (!this.SecurityAllowInsecureAccess)
            {
                var ici = Thread.CurrentPrincipal.Identity as IClaimsIdentity;
                if (ici == null)
                {
                    throw new FaultException<OGF.BES.Faults.NotAuthorizedFaultType>
                          (new OGF.BES.Faults.NotAuthorizedFaultType()
                          {

                          }, new FaultReason(ExceptionMessages.IdentityNotRetrieved));
                }
                else
                {
                    currentUser = ici.Name;
                }
            }
            else
            {
                currentUser = "anonymous";
            }
            try
            {
                RuntimeEnvironment.Subscribe(currentUser, groupName, newSubscription);
            }
            catch(Exception ex)
            {
                throw new FaultException<OGF.BES.Faults.Fault>
                          (new OGF.BES.Faults.Fault()
                          {

                          }, new FaultReason(String.Format(ExceptionMessages.GeneralRequestError, "CreateSubscriptionForGroupStatuses", ex.ToString())));
            }
        }
    }
}
