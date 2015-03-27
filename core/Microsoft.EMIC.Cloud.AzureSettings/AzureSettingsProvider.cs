//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Microsoft.EMIC.Cloud.AzureSettings
{
    using System;
    using System.ComponentModel.Composition;
    using System.IO;
    using System.Linq;
    using System.Security.Principal;
    using System.Text.RegularExpressions;
    using Microsoft.Win32;
    using Microsoft.WindowsAzure.ServiceRuntime;

    /// <summary>
    /// Provides configuration settings for a deployment on Windows Azure. 
    /// </summary>
    public static class AzureSettingsProvider
    {
        #region Logic

        private static bool IsDevMachine { get { return GetRegistryValue("Microsoft.EMIC.Cloud.IsDevMachine") != null && GetRegistryValue("Microsoft.EMIC.Cloud.IsDevMachine").ToLower().Equals("true"); } }
        private static bool InFabric { get { return RoleEnvironment.IsAvailable; } }
        private static bool InRealCloud { get { return !IsDevMachine && InFabric; } }
        private static bool InDevFabric { get { return IsDevMachine && InFabric; } }

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
            if (InRealCloud)
            {
                return RoleEnvironment.GetConfigurationSettingValue(key);
            }

            if (IsDevMachine)
            {
                var registryData = GetRegistryValue(key);
                if (registryData != null)
                {
                    return registryData;
                }

                return RoleEnvironment.GetConfigurationSettingValue(key);
            }

            throw new NotSupportedException();
        }

        private static string GetTableBlobQueueName(string key)
        {
            if (InRealCloud)
            {
                if (!CompositionIdentifiers.ProductionStorageName.ContainsKey(key))
                {
                    throw new NotSupportedException(
                        String.Format(ExceptionMessages.StorageMEFSetting, key));
                }

                return CompositionIdentifiers.ProductionStorageName[key];
            }

            if (IsDevMachine)
            {
                var registryData = GetRegistryValue(key);
                if (registryData != null)
                    return registryData;

                return string.Format("{0}{1}", UserPrefix, key);
            }

            throw new NotSupportedException();
        }

        private static string GetLocalDirectory(string azureKey)
        {
            if (InRealCloud)
            {
                var lr = RoleEnvironment.GetLocalResource(azureKey);

                return lr.RootPath;
            }

            string path = string.Format(@"C:\Resources\Directory\7WebRole.{0}", azureKey);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>
        /// Gets the user prefix.
        /// </summary>
        public static string UserPrefix
        {
            get
            {
                var windowsIdentity = WindowsIdentity.GetCurrent();
                if (windowsIdentity == null)
                {
                    return "UnknownWindowsIdentity";
                }

                var name = windowsIdentity.Name;
                if (name.Contains(@"\"))
                {
                    name = name.Substring(1 + name.IndexOf(@"\"));
                    name = Regex.Replace(name, "[^a-zA-Z0-9]", "");
                }
                return name.ToLower();
            }
        }

        #endregion

        #region Scaling

        /// <summary>
        /// Gets the name of the Azure Service.
        /// </summary>
        /// <value>
        /// The name of the azure service.
        /// </value>
        [Export(CompositionIdentifiers.AzureServiceName)]
        public static string AzureServiceName { get { return GetSettingValue(CompositionIdentifiers.AzureServiceName); } }

        /// <summary>
        /// Gets the azure Subscription ID.
        /// </summary>
        [Export(CompositionIdentifiers.AzureSubscriptionId)]
        public static string AzureSubscriptionId { get { return GetSettingValue(CompositionIdentifiers.AzureSubscriptionId); } }

        /// <summary>
        /// Gets the azure Management Certificate Thumbprint.
        /// </summary>
        [Export(CompositionIdentifiers.AzureMngmtCertThumbprint)]
        public static string AzureMngmtCertThumbprint { get { return GetSettingValue(CompositionIdentifiers.AzureMngmtCertThumbprint); } }

        #endregion

        #region STS

        /// <summary>
        /// Gets the corporate STS URL.
        /// </summary>
        [Export(CompositionIdentifiers.STSURL)]
        public static string CorporateSTSUrl { get { return AzureSettingsProvider.GetSettingValue(CompositionIdentifiers.STSURL); } }

        /// <summary>
        /// Gets the corporate secured job management URL.
        /// </summary>
        [Export(CompositionIdentifiers.SecuredJobManagementSiteURL)]
        public static string SecuredJobManagementSiteURL { get { return AzureSettingsProvider.GetSettingValue(CompositionIdentifiers.SecuredJobManagementSiteURL); } }

        /// <summary>
        /// Gets the STS on Azure Certificate Thumbprint.
        /// </summary>
        [Export(CompositionIdentifiers.STSCertificateThumbprint)]
        public static string STSOnAzureCertificateThumbprint { get { return GetSettingValue(CompositionIdentifiers.STSCertificateThumbprint); } }

        /// <summary>
        /// Gets the name of the development security token service user table.
        /// </summary>
        /// <value>
        /// The name of the development security token service user table.
        /// </value>
        [Export(CompositionIdentifiers.DevelopmentSecurityTokenServiceUserTableName)]
        public static string DevelopmentSecurityTokenServiceUserTableName { get { return GetTableBlobQueueName(CompositionIdentifiers.DevelopmentSecurityTokenServiceUserTableName); } }

        /// <summary>
        /// Gets the STS on azure connection string.
        /// </summary>
        [Export(CompositionIdentifiers.STSOnAzureConnectionString)]
        public static string STSOnAzureConnectionString { get { return GetSettingValue(CompositionIdentifiers.STSOnAzureConnectionString); } }

        /// <summary>
        /// Gets a value indicating whether insecure access is allowed.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if insecure access is allowed otherwise, <c>false</c>.
        /// </value>
        [Export(CompositionIdentifiers.SecurityAllowInsecureAccess)]
        public static bool SecurityAllowInsecureAccess { get { return bool.Parse(GetSettingValue(CompositionIdentifiers.SecurityAllowInsecureAccess)); } }

        /// <summary>
        /// Gets a value indicating whether generic worker accounting is on.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if generic worker accounting is on; otherwise, <c>false</c>.
        /// </value>
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
        /// Gets the max number of parallel tasks that can be handled by a generic worker instance.
        /// </summary>
        [Export(CompositionIdentifiers.GenericWorkerParallelTasks)]
        public static int GenericWorkerParallelTasks 
        {   get 
            { 
                var numTasksSetting = GetSettingValue(CompositionIdentifiers.GenericWorkerParallelTasks);
                int numTasks;
                if (Int32.TryParse(numTasksSetting, out numTasks) && numTasks>0)
                {
                    return numTasks;            
                }
                return 1;
            }
        }

        /// <summary>
        /// The shared machine key for WCF.
        /// </summary>
        [Export(CompositionIdentifiers.WCFSharedMachineKey)]
        public static string WCFSharedMachineKey { get { return GetSettingValue(CompositionIdentifiers.WCFSharedMachineKey); } }

        /// <summary>
        /// Gets the serialized global security policy.
        /// </summary>
        [Export(CompositionIdentifiers.SerializedGlobalSecurityPolicy)]
        public static string SerializedGlobalSecurityPolicy { get { return GetSettingValue(CompositionIdentifiers.SerializedGlobalSecurityPolicy); } }

        /// <summary>
        /// Gets the generic worker job BLOB store.
        /// </summary>
        [Export(CompositionIdentifiers.DevelopmentGenericWorkerBlobName)]
        public static string GenericWorkerJobBlobStore { get { return GetTableBlobQueueName(CompositionIdentifiers.DevelopmentGenericWorkerBlobName); } }

        /// <summary>
        /// Gets the generic worker notification subscription BLOB store.
        /// </summary>
        [Export(CompositionIdentifiers.DevelopmentGenericWorkerSubscriptionsBlobName)]
        public static string GenericWorkerSubscriptionBlobStore { get { return GetTableBlobQueueName(CompositionIdentifiers.DevelopmentGenericWorkerSubscriptionsBlobName); } }

        /// <summary>
        /// Gets the name of the generic worker index table.
        /// </summary>
        /// <value>
        /// The name of the generic worker index table.
        /// </value>
        [Export(CompositionIdentifiers.DevelopmentGenericWorkerIndexTableName)]
        public static string GenericWorkerIndexTableName { get { return GetTableBlobQueueName(CompositionIdentifiers.DevelopmentGenericWorkerIndexTableName); } }

        /// <summary>
        /// Gets the name of the generic worker details table.
        /// </summary>
        /// <value>
        /// The name of the generic worker details table.
        /// </value>
        [Export(CompositionIdentifiers.DevelopmentGenericWorkerDetailsTableName)]
        public static string GenericWorkerDetailsTableName { get { return GetTableBlobQueueName(CompositionIdentifiers.DevelopmentGenericWorkerDetailsTableName); } }

        /// <summary>
        /// Gets the name of the generic worker hygiene table.
        /// </summary>
        /// <value>
        /// The name of the generic worker hygiene table.
        /// </value>
        [Export(CompositionIdentifiers.DevelopmentGenericWorkerHygieneTableName)]
        public static string GenericWorkerHygieneTableName { get { return GetTableBlobQueueName(CompositionIdentifiers.DevelopmentGenericWorkerHygieneTableName); } }

        /// <summary>
        /// Gets the name of the generic worker hygiene table.
        /// </summary>
        /// <value>
        /// The name of the generic worker hygiene table.
        /// </value>
        [Export(CompositionIdentifiers.DevelopmentGenericWorkerAccountingTableName)]
        public static string GenericWorkerAccountingTableName { get { return GetTableBlobQueueName(CompositionIdentifiers.DevelopmentGenericWorkerAccountingTableName); } }

        /// <summary>
        /// Gets the name of the generic worker job progress queue.
        /// </summary>
        /// <value>
        /// The name of the generic worker job progress queue.
        /// </value>
        [Export(CompositionIdentifiers.DevelopmentGenericWorkerProgressQueueName)]
        public static string GenericWorkerProgressQueueName { get { return GetTableBlobQueueName(CompositionIdentifiers.DevelopmentGenericWorkerProgressQueueName); } }

        /// <summary>
        /// Gets the generic worker directory application installation.
        /// </summary>
        //[Export(CompositionIdentifiers.GenericWorkerDirectoryApplicationInstallation)]
        //public static string GenericWorkerDirectoryApplicationInstallation { get { return GetLocalDirectory("GWApps"); } }
        [Export(CompositionIdentifiers.GenericWorkerDirectoryApplicationInstallation)]
        public static string GenericWorkerDirectoryApplicationInstallation { get { return Path.Combine(GetLocalDirectory("GWApps"), string.Format("{0}-localAppFolder{1:000}", RoleEnvironment.CurrentRoleInstance.Id.Split('.').Last(),GetNumber())); } }

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
                if (RoleEnvironment.IsAvailable)
                    return string.Format("{0}-worker-{1:000}", RoleEnvironment.CurrentRoleInstance.Id, GetNumber());

                return string.Format("worker-{1:000}",
                    DateTime.UtcNow, GetNumber());
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
		        var entriesSetting = RoleEnvironment.GetConfigurationSettingValue(CompositionIdentifiers.JobEntriesPerPage);
                int numEntries;
                if (Int32.TryParse(entriesSetting, out numEntries))
                {
                    return numEntries;
                }
                return 100;
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
    }
}