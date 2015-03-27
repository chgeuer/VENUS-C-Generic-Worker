//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using Microsoft.EMIC.Cloud;
using System.ComponentModel.Composition;
using System.IO;

namespace OnPremisesSettings
{
    public static class OnPremisesSettings
    {
        #region Logic

        private static string GetRegistryValue(string key)
        {
            RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft EMIC\Cloud\VENUS-C");
            if (regKey != null)
            {
                var regVal = regKey.GetValue(key) as string;
                if (!string.IsNullOrEmpty(regVal))
                    return regVal;
            }

            return null;
        }

        private static string GetSettingValue(string key)
        {
            var registryData = GetRegistryValue(key);
            if (registryData != null)
            {
                return registryData;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private static string GetLocalDirectory(string azureKey)
        {
            string path = string.Format(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) + "\\{0}", azureKey);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        #region Scaling

        [Export(CompositionIdentifiers.AzureServiceName)]
        public static string AzureServiceName { get { return GetSettingValue(CompositionIdentifiers.AzureServiceName); } }

        [Export(CompositionIdentifiers.AzureSubscriptionId)]
        public static string AzureSubscriptionId { get { return GetSettingValue(CompositionIdentifiers.AzureSubscriptionId); } }

        [Export(CompositionIdentifiers.AzureMngmtCertThumbprint)]
        public static string AzureMngmtCertThumbprint { get { return GetSettingValue(CompositionIdentifiers.AzureMngmtCertThumbprint); } }

        #endregion

        #region STS

        /// <summary>
        /// Gets the corporate STS URL.
        /// </summary>
        [Export(CompositionIdentifiers.STSURL)]
        public static string CorporateSTSUrl { get { return GetSettingValue(CompositionIdentifiers.STSURL); } }

        /// <summary>
        /// Gets the corporate Passive STS URL.
        /// </summary>
        [Export(CompositionIdentifiers.PassiveSTSURL)]
        public static string CorporatePassiveSTSUrl { get { return GetSettingValue(CompositionIdentifiers.PassiveSTSURL); } }

        /// <summary>
        /// Gets the corporate secured job management URL.
        /// </summary>
        [Export(CompositionIdentifiers.SecuredJobManagementSiteURL)]
        public static string SecuredJobManagementSiteURL { get { return GetSettingValue(CompositionIdentifiers.SecuredJobManagementSiteURL); } }

        /// <summary>
        /// Gets the STS on azure certificate thumbprint.
        /// </summary>
        [Export(CompositionIdentifiers.STSCertificateThumbprint)]
        public static string STSOnAzureCertificateThumbprint { get { return GetSettingValue(CompositionIdentifiers.STSCertificateThumbprint); } }

        [Export(CompositionIdentifiers.DevelopmentSecurityTokenServiceUserTableName)]
        public static string DevelopmentSecurityTokenServiceUserTableName { get { return GetSettingValue(CompositionIdentifiers.DevelopmentSecurityTokenServiceUserTableName); } }

        [Export(CompositionIdentifiers.STSOnAzureConnectionString)]
        public static string STSOnAzureConnectionString { get { return GetSettingValue(CompositionIdentifiers.STSOnAzureConnectionString); } }

        [Export(CompositionIdentifiers.SecurityAllowInsecureAccess)]
        public static bool SecurityAllowInsecureAccess { get { return bool.Parse(GetSettingValue(CompositionIdentifiers.SecurityAllowInsecureAccess)); } }

        [Export(CompositionIdentifiers.GenericWorkerIsAccountingOn)]
        public static bool GenericWorkerIsAccountingOn { get { return bool.Parse(GetSettingValue(CompositionIdentifiers.GenericWorkerIsAccountingOn)); } }


        #endregion

        /// <summary>
        /// Gets the scaling service URL.
        /// </summary>
        [Export(CompositionIdentifiers.ScalingServiceURL)]
        public static string ScalingServiceURL { get { return GetSettingValue(CompositionIdentifiers.ScalingServiceURL); } }

        /// <summary>
        /// Gets the notification service URL.
        /// </summary>
        [Export(CompositionIdentifiers.NotificationServiceURL)]
        public static string NotificationServiceURL { get { return GetSettingValue(CompositionIdentifiers.NotificationServiceURL); } }

        #region Generic Worker

        /// <summary>
        /// Gets the job submission URL.
        /// </summary>
        [Export(CompositionIdentifiers.GenericWorkerURL)]
        public static string GenericWorkerURL { get { return GetSettingValue(CompositionIdentifiers.GenericWorkerURL); } }

        /// <summary>
        /// Gets the generic worker cloud connection string.
        /// </summary>
        [Export(CompositionIdentifiers.GenericWorkerConnectionString)]
        public static string GenericWorkerCloudConnectionString { get { return GetSettingValue(CompositionIdentifiers.GenericWorkerConnectionString); } }

        /// <summary>
        /// The shared machine key for WCF.
        /// </summary>
        [Export(CompositionIdentifiers.WCFSharedMachineKey)]
        public static string WCFSharedMachineKey { get { return GetSettingValue(CompositionIdentifiers.WCFSharedMachineKey); } }

        [Export(CompositionIdentifiers.SerializedGlobalSecurityPolicy)]
        public static string SerializedGlobalSecurityPolicy { get { return GetSettingValue(CompositionIdentifiers.SerializedGlobalSecurityPolicy); } }

        /// <summary>
        /// Gets the generic worker job BLOB store.
        /// </summary>
        [Export(CompositionIdentifiers.DevelopmentGenericWorkerBlobName)]
        public static string GenericWorkerJobBlobStore { get { return GetSettingValue(CompositionIdentifiers.DevelopmentGenericWorkerBlobName); } }

        /// <summary>
        /// Gets the generic worker notification subscription BLOB store.
        /// </summary>
        [Export(CompositionIdentifiers.DevelopmentGenericWorkerSubscriptionsBlobName)]
        public static string GenericWorkerSubscriptionBlobStore { get { return GetSettingValue(CompositionIdentifiers.DevelopmentGenericWorkerSubscriptionsBlobName); } }

        /// <summary>
        /// Gets the name of the generic worker index table.
        /// </summary>
        /// <value>
        /// The name of the generic worker index table.
        /// </value>
        [Export(CompositionIdentifiers.DevelopmentGenericWorkerIndexTableName)]
        public static string GenericWorkerIndexTableName { get { return GetSettingValue(CompositionIdentifiers.DevelopmentGenericWorkerIndexTableName); } }

        /// <summary>
        /// Gets the name of the generic worker details table.
        /// </summary>
        /// <value>
        /// The name of the generic worker details table.
        /// </value>
        [Export(CompositionIdentifiers.DevelopmentGenericWorkerDetailsTableName)]
        public static string GenericWorkerDetailsTableName { get { return GetSettingValue(CompositionIdentifiers.DevelopmentGenericWorkerDetailsTableName); } }

        /// <summary>
        /// Gets the name of the generic worker hygiene table.
        /// </summary>
        /// <value>
        /// The name of the generic worker hygiene table.
        /// </value>
        [Export(CompositionIdentifiers.DevelopmentGenericWorkerHygieneTableName)]
        public static string GenericWorkerHygieneTableName { get { return GetSettingValue(CompositionIdentifiers.DevelopmentGenericWorkerHygieneTableName); } }

        /// <summary>
        /// Gets the name of the generic worker hygiene table.
        /// </summary>
        /// <value>
        /// The name of the generic worker hygiene table.
        /// </value>
        [Export(CompositionIdentifiers.DevelopmentGenericWorkerAccountingTableName)]
        public static string GenericWorkerAccountingTableName { get { return GetSettingValue(CompositionIdentifiers.DevelopmentGenericWorkerAccountingTableName); } }

        /// <summary>
        /// Gets the name of the generic worker job progress queue.
        /// </summary>
        /// <value>
        /// The name of the generic worker job progress queue.
        /// </value>
        [Export(CompositionIdentifiers.DevelopmentGenericWorkerProgressQueueName)]
        public static string GenericWorkerProgressQueueName { get { return GetSettingValue(CompositionIdentifiers.DevelopmentGenericWorkerProgressQueueName); } }

        /// <summary>
        /// Gets the generic worker directory application installation.
        /// </summary>
        [Export(CompositionIdentifiers.GenericWorkerDirectoryApplicationInstallation)]
        public static string GenericWorkerDirectoryApplicationInstallation { get { return GetLocalDirectory("GWApps"); } }

        /// <summary>
        /// Gets the generic worker directory user folder.
        /// </summary>
        [Export(CompositionIdentifiers.GenericWorkerDirectoryUserFolder)]
        public static string GenericWorkerDirectoryUserFolder { get { return GetLocalDirectory("GWUsers"); } }

        /// <summary>
        /// Gets the generic worker directory reference data sets.
        /// </summary>
        [Export(CompositionIdentifiers.GenericWorkerDirectoryReferenceDataSets)]
        public static string GenericWorkerDirectoryReferenceDataSets { get { return GetLocalDirectory("GWData"); } }

        /// <summary>
        /// Gets whether the GW is deployed on Azure as Webrole
        /// </summary>
        [Export(CompositionIdentifiers.IsGWDeployedOnAzureWebRole)]
        public static bool IsGWDeployedOnAzureWebRole { get { return bool.Parse(GetSettingValue(CompositionIdentifiers.IsGWDeployedOnAzureWebRole)); } }

        /// <summary>
        /// Gets the instance identity.
        /// </summary>
        [Export(CompositionIdentifiers.InstanceIdentity)]
        public static string InstanceIdentity
        {
            get
            {
                return string.Format("Machine Name:{0}-number-{1:000}", System.Environment.MachineName, GetNumber());
            }
        }

        /// <summary>
        /// Gets the number of job entries per page.
        /// Currently the maximum number of job entries fitting in a WCF message is 333. 
        /// A Paging mechanism is needed to allow users retrieve job lists with more than 333 entires.
        /// </summary>
        [Export(CompositionIdentifiers.JobEntriesPerPage)]
        public static int JobEntriesPerPage
        {
            get
            {
                return 200;
            }
        }

        /// <summary>
        /// Gets the local job summission end point.
        /// </summary>
        [Export(CompositionIdentifiers.LocalJobSummissionEndPoint)]
        public static string LocalJobSummissionEndPoint
        {
            get
            {
                return String.Format("http://localhost/AcceptLocalJobs/{0}", InstanceIdentity);
            }
        }

        private static readonly object InstanceIdentityLock = new object();
        private static int _instanceIdentityCounter = 1;
        private static int GetNumber()
        {
            lock (InstanceIdentityLock)
            {
                return _instanceIdentityCounter++;
            }
        }

        #endregion

        #endregion
    }
}
