//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Microsoft.EMIC.Cloud.Security
{
	using System;
	using System.Linq;
	using System.Diagnostics;
	using Microsoft.IdentityModel.Claims;
	using Microsoft.EMIC.Cloud.ApplicationRepository;
	using AuthorizationPolicy;
	
	/// <summary>
	/// A <see cref="ClaimsAuthorizationManager"/> to check access against the VENUS-C services. 
	/// </summary>
	public class VenusClaimsAuthorizationManager : ClaimsAuthorizationManager
	{
        /// <summary>
        /// Static class that defines the roles used in the system
        /// </summary>
		public static class Roles
		{
            /// <summary>
            /// Researcher Role
            /// </summary>
			public static readonly Claim Researcher = new Claim(ClaimTypes.Role, "VENUS-C Researcher");
            /// <summary>
            /// Administrator Role
            /// </summary>
			public static readonly Claim ComputeAdministrator = new Claim(ClaimTypes.Role, "VENUS-C Compute Administrator");
		}

		internal static class OGFActions
		{
			public const string ogfPrefix = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/";
			public const string CreateActivity = ogfPrefix + "CreateActivity";
			public const string GetActivityStatuses = ogfPrefix + "GetActivityStatuses";
			public const string TerminateActivities = ogfPrefix + "TerminateActivities";
			public const string GetActivityDocuments = ogfPrefix + "GetActivityDocuments";
			public const string GetFactoryAttributesDocument = ogfPrefix + "GetFactoryAttributesDocument";
		}

        internal static class ScalingActions
        {
            public const string prefix = "http://tempuri.org/IScalingService/";
            public const string List = prefix + "ListDeployments";
            public const string Update = prefix + "UpdateDeployment";
        }

        internal static class JobListingActions
        {
            public const string prefix = "http://schemas.ggf.org/bes/2006/08/bes-factory/BESFactoryPortType/";
            public const string GetJobs = prefix + "GetJobs";
            public const string GetAllJobs = prefix + "GetAllJobs";
            public const string GetHierarchy = prefix + "GetHierarchy";
            public const string GetRoot = prefix + "GetRoot";
            public const string GetJobsByGroup = prefix + "GetJobsByGroup";
        }

        internal static class NotificationActions
        {
            public const string prefix = "http://tempuri.org/INotificationService/";
            public const string CreateSubscriptionForStatuses = prefix + "CreateSubscriptionForStatuses";
            public const string CreateSubscription = prefix + "CreateSubscription";
            public const string CreateSubscriptionForStatusesOfGroup = prefix + "CreateSubscriptionForGroupStatuses";
            public const string Unsubscribe = prefix + "Unsubscribe";
        }

        internal static class JobManagementComfortActions
        {
            public const string prefix = JobListingActions.prefix;
            public const string CancelGroup = prefix + "CancelGroup";
            public const string CancelHierarchy = prefix + "CancelHierarchy";
            public const string RemoveTerminatedJobs = prefix + "RemoveTerminatedJobs";
            public const string GetNumberOfJobs = prefix + "GetNumberOfJobs";
        }

		private readonly ClaimRequirementPolicy policy;
		private readonly string _prefix = typeof(VenusClaimsAuthorizationManager).Name;
		private readonly Action<string> log;

        /// <summary>
        /// Gets the default policy.
        /// </summary>
		public static ClaimRequirementPolicy DefaultPolicy
		{
			get
            {
                #region PermissionsAlsoUsedInDocumentation
                var researcher = Roles.Researcher.AsRequirement();
				var computeAdmin = Roles.ComputeAdministrator.AsRequirement();
				var researcherOrComputeAdmin = researcher.Or(computeAdmin);

                return new ClaimRequirementPolicy
						 {
							 { OGFActions.CreateActivity, researcher },
							 { OGFActions.GetActivityDocuments, researcherOrComputeAdmin },
							 { OGFActions.GetActivityStatuses, researcherOrComputeAdmin },
							 { OGFActions.TerminateActivities, researcherOrComputeAdmin },
							 { OGFActions.GetFactoryAttributesDocument, computeAdmin},
                             { ScalingActions.List, researcherOrComputeAdmin},
                             { ScalingActions.Update, computeAdmin},
                             { JobListingActions.GetJobs, researcherOrComputeAdmin},
                             { JobListingActions.GetAllJobs, computeAdmin},
                             { JobListingActions.GetHierarchy, researcherOrComputeAdmin},
                             { JobListingActions.GetRoot, researcherOrComputeAdmin},
                             { JobListingActions.GetJobsByGroup, researcherOrComputeAdmin},
                             { NotificationActions.CreateSubscription, researcher},
                             { NotificationActions.CreateSubscriptionForStatuses, researcher},
                             { NotificationActions.CreateSubscriptionForStatusesOfGroup, researcher },
                             { NotificationActions.Unsubscribe, researcherOrComputeAdmin},
                             { JobManagementComfortActions.CancelGroup, researcherOrComputeAdmin},
                             { JobManagementComfortActions.CancelHierarchy, researcherOrComputeAdmin},
                             { JobManagementComfortActions.RemoveTerminatedJobs, researcherOrComputeAdmin},
                             { JobManagementComfortActions.GetNumberOfJobs, researcherOrComputeAdmin}
						 };
                #endregion
            }
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="VenusClaimsAuthorizationManager"/> class.
        /// </summary>
		public VenusClaimsAuthorizationManager()
		{
			this.policy = DefaultPolicy;

			log = str => Trace.TraceInformation(string.Format("{0}: {1}", _prefix, str));
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="VenusClaimsAuthorizationManager"/> class.
        /// </summary>
        /// <param name="claimRequirementPolicy">The claim requirement policy.</param>
		public VenusClaimsAuthorizationManager(ClaimRequirementPolicy claimRequirementPolicy)
		{
			this.policy = claimRequirementPolicy;

			log = str => Trace.TraceInformation(string.Format("{0}: {1}", _prefix, str));
		}

        /// <summary>
        /// Checks the specified action in AuthorizationContext is satisfied by the claims
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
		public override bool CheckAccess(AuthorizationContext context)
		{
			var action = context.Action.Where(x => x.ClaimType.Equals(ClaimTypes.Name)).Select(x => x.Value).FirstOrDefault();
			var service = context.Resource.Where(x => x.ClaimType.Equals(ClaimTypes.Name)).Select(x => x.Value).FirstOrDefault();

			if (!policy.ContainsKey(action))
			{
				log(string.Format("Action {0} is not configured in policy", action));
				return false;
			}
			var resourcePolicy = policy[action];

			var clientClaims = context.Principal.Identities.SelectMany(id => id.Claims);
			bool policyIsSatisfied = resourcePolicy.IsSatisfiedBy(clientClaims);

			log(string.Format("Resource policy is satisfied: {0}", policyIsSatisfied));
			if (!policyIsSatisfied)
			{
				Trace.TraceInformation("Access is denied. ");
				Trace.TraceInformation("Policy is {0}. ", resourcePolicy.ToString());
				clientClaims.ToList().ForEach(claim => Trace.TraceInformation("Input claim {0}. ", claim.AsRequirement().ToString()));
			}

            return policyIsSatisfied;
		}
	}
}
