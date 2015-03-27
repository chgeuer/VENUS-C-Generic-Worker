//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Microsoft.EMIC.Cloud.Utilities
{
    using System;
    using System.ComponentModel.Composition.Hosting;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public static class AddressUtils
    {
        private static string JobManagementWebSiteSuffix { get { return "/JobManagement/"; } }
        private static string JobSubmissionProtectedSuffix { get { return "/JobSubmission/SecureService.svc"; } }
        private static string JobSubmissionUnprotectedSuffix { get { return "/JobSubmission/Service.svc"; } }
        private static string ScalingProtectedSuffix { get { return "/ScalingService/SecureService.svc"; } }
        private static string ScalingUnprotectedSuffix { get { return "/ScalingService/Service.svc"; } }
        private static string NotificationProtectedSuffix { get { return "/NotificationService/SecureService.svc"; } }
        private static string NotificationUnprotectedSuffix { get { return "/NotificationService/Service.svc"; } }
        private static string SecurityTokenServiceFederationMetadataSuffix { get { return "/STS/FederationMetadata/2007-06/FederationMetadata.xml"; } }
        private static string SecurityTokenServiceUsernamePasswordSuffix { get { return "/STS/UsernamePassword.svc"; } }

        const string ApplicationrepositoryFolder = "/AppStore/";
        const string JobSubmissionFolder = "/JobSubmission/";
        const string ScalingServiceFolder = "/ScalingService/";
        const string NotificationServiceFolder = "/NotificationService/";
        const string SecurityTokenServiceFolder = "/STS/";

        /// <summary>
        /// Gets the suffix collection.
        /// </summary>
        public static string[] SuffixCollection
        {
            get
            {
                return new string[]
                           {
                               AddressUtils.JobManagementWebSiteSuffix,
                               AddressUtils.JobSubmissionProtectedSuffix,
                               AddressUtils.JobSubmissionUnprotectedSuffix,
                               AddressUtils.ScalingProtectedSuffix,
                               AddressUtils.ScalingUnprotectedSuffix,
                               AddressUtils.NotificationProtectedSuffix,
                               AddressUtils.NotificationUnprotectedSuffix,
                               AddressUtils.SecurityTokenServiceFederationMetadataSuffix,
                               AddressUtils.SecurityTokenServiceUsernamePasswordSuffix
                           };
            }
        }

        internal static string GetSecurityTokenServiceMetadataAddress(this CompositionContainer container)
        {
            return ComputeLocation(container, CompositionIdentifiers.STSURL, SecurityTokenServiceFolder, SecurityTokenServiceFederationMetadataSuffix);
        }

        internal static string GetSecureJobSubmissionAddress(this CompositionContainer container)
        {
            return ComputeLocation(container, CompositionIdentifiers.GenericWorkerURL, JobSubmissionFolder, JobSubmissionProtectedSuffix);
        }

        internal static string GetSecureScalingServiceAddress(this CompositionContainer container)
        {
            return ComputeLocation(container, CompositionIdentifiers.ScalingServiceURL, ScalingServiceFolder, ScalingProtectedSuffix);
        }

        internal static string GetSecureNotificationServiceAddress(this CompositionContainer container)
        {
            return ComputeLocation(container, CompositionIdentifiers.NotificationServiceURL, NotificationServiceFolder, NotificationProtectedSuffix);
        }

        private static string ComputeLocation(this CompositionContainer container, string mefKey, string folder, string newSuffix)
        {
            var url = container.GetExportedValue<string>(mefKey);
            if (url.IndexOf(folder) == -1)
            {
                throw new NotSupportedException(string.Format(ExceptionMessages.CanNotFindFolder, folder, url));
            }

            var result = string.Format("{0}{1}", url.Substring(0, url.IndexOf(folder)), newSuffix);
            return result;
        }
    }
}
