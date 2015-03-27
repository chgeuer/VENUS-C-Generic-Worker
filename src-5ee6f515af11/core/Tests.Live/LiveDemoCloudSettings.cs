//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Microsoft.EMIC.Cloud;

namespace Microsoft.EMIC.Cloud.LiveDemo
{
    internal enum HostSwitch
    {
        DevelopmentFabric,
        OnPremises,
        Cloud
    }

    internal static class LiveDemoCloudSettings
    {
        private static HostSwitch HostSwitch
        {
            get
            {
                return HostSwitch.Cloud;
            }
        }

        #region Logic

        private static string GetRegistryValue(string key)
        {
            RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft EMIC\Cloud\VENUS-C", false);
            if (regKey != null)
            {
                var regVal = regKey.GetValue(key);
                if (regVal != null)
                    return regVal.ToString();
            }

            return null;
        }

        private static string GetSettingValue(string key)
        {
            var registryData = GetRegistryValue(key);
            if (registryData != null)
                return registryData;

            return ConfigurationManager.AppSettings[key];
        }

        private static string GetTableBlobQueueName(string key)
        {
            return "gwjobblobstore";
        }

        public static string UserPrefix
        {
            get
            {
                return "";
            }
        }

        private static string RootFolder
        {
            get
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows).Substring(0, 3), "GW");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return path;
            }
        }

        #endregion
        
        [Export(CompositionIdentifiers.DemoUserBlobName)]
        public static string UserDataStoreBlobName { get { return GetTableBlobQueueName(CompositionIdentifiers.DemoUserBlobName); } }

        [Export(CompositionIdentifiers.TestsConnectionString)]
        public static string TestCloudConnectionString { get { return GetSettingValue(CompositionIdentifiers.TestsConnectionString); } }

        [Export(CompositionIdentifiers.LiveTestsConnectionString)]
        public static string LiveTestsCloudConnectionString { get { return GetSettingValue(CompositionIdentifiers.LiveTestsConnectionString); } }

        [Export(CompositionIdentifiers.GenericWorkerConnectionString)]
        public static string GenericWorkerCloudConnectionString { get { return GetSettingValue(CompositionIdentifiers.GenericWorkerConnectionString); } }
        
        [Export(CompositionIdentifiers.DevelopmentGenericWorkerBlobName)]
        public static string GenericWorkerJobBlobStore { get { return GetTableBlobQueueName(CompositionIdentifiers.DevelopmentGenericWorkerBlobName); } }

        [Export(CompositionIdentifiers.DevelopmentGenericWorkerSubscriptionsBlobName)]
        public static string GenericWorkerSubscriptionBlobStore { get { return GetTableBlobQueueName(CompositionIdentifiers.DevelopmentGenericWorkerSubscriptionsBlobName); } }

        [Export(CompositionIdentifiers.DevelopmentGenericWorkerDetailsTableName)]
        public static string GenericWorkerDetailsTableName { get { return CompositionIdentifiers.ProductionStorageName[CompositionIdentifiers.DevelopmentGenericWorkerDetailsTableName]; } }
        
        [Export(CompositionIdentifiers.DevelopmentGenericWorkerIndexTableName)]
        public static string GenericWorkerIndexTableName { get { return CompositionIdentifiers.ProductionStorageName[CompositionIdentifiers.DevelopmentGenericWorkerIndexTableName]; } }

        [Export(CompositionIdentifiers.DevelopmentGenericWorkerHygieneTableName)]
        public static string GenericWorkerHygieneTableName { get { return GetTableBlobQueueName(CompositionIdentifiers.DevelopmentGenericWorkerHygieneTableName); } }

        [Export(CompositionIdentifiers.DevelopmentGenericWorkerAccountingTableName)]
        public static string GenericWorkerAccountingTableName { get { return GetTableBlobQueueName(CompositionIdentifiers.DevelopmentGenericWorkerAccountingTableName); } }

        [Export(CompositionIdentifiers.GenericWorkerDirectoryApplicationInstallation)]
        public static string GenericWorkerDirectoryApplicationInstallation { get { return Path.Combine(RootFolder, string.Format("localAppFolder{0:000}", GetNumber())); } }

        [Export(CompositionIdentifiers.GenericWorkerDirectoryUserFolder)]
        public static string GenericWorkerDirectoryUserFolder { get { return Path.Combine(RootFolder, @"localUserFiles"); } }

        [Export(CompositionIdentifiers.GenericWorkerDirectoryReferenceDataSets)]
        public static string GenericWorkerDirectoryReferenceDataSets { get { return Path.Combine(RootFolder, @"localReferenceDataSets"); } }

        [Export(CompositionIdentifiers.IsGWDeployedOnAzureWebRole)]
        public static bool IsGWDeployedOnAzureWebRole { get { return bool.Parse(GetSettingValue(CompositionIdentifiers.IsGWDeployedOnAzureWebRole)); } }

        private static string GetHostName(HostSwitch hs, string suffix)
        {
            var key = "Microsoft.EMIC.Cloud.Development.Host.Address." + hs.ToString();
            var host = GetRegistryValue(key);
            var url = string.Format("http://{0}{1}", host, suffix);
            return url;
        }
        
        [Export(CompositionIdentifiers.GenericWorkerURL)]
        public static string GenericWorkerUrl { get { return GetHostName(LiveDemoCloudSettings.HostSwitch, "/JobSubmission/Service.svc"); } }

        [Export(CompositionIdentifiers.ScalingServiceURL)]
        public static string ScalingServiceUrl { get { return GetHostName(LiveDemoCloudSettings.HostSwitch, "/ScalingService/Service.svc"); } }

        [Export(CompositionIdentifiers.NotificationServiceURL)]
        public static string NotificationServiceURL { get { return GetHostName(LiveDemoCloudSettings.HostSwitch, "/NotificationService/Service.svc"); } }

        [Export(CompositionIdentifiers.STSURL)]
        public static string CorporateSTSUrl { get { return GetHostName(LiveDemoCloudSettings.HostSwitch, "/STS/UsernamePassword.svc"); } }

        /// <summary>
        /// Gets the corporate secured job management URL.
        /// </summary>
        [Export(CompositionIdentifiers.SecuredJobManagementSiteURL)]
        public static string SecuredJobManagementSiteURL { get { return GetHostName(LiveDemoCloudSettings.HostSwitch, "/JobManagement/"); } }


        [Export(CompositionIdentifiers.DevelopmentSecurityTokenServiceUserTableName)]
        public static string DevelopmentSecurityTokenServiceUserTableName { get { return GetTableBlobQueueName(CompositionIdentifiers.DevelopmentSecurityTokenServiceUserTableName); } }

        [Export(CompositionIdentifiers.STSOnAzureConnectionString)]
        public static string STSOnAzureConnectionString { get { return GetSettingValue(CompositionIdentifiers.STSOnAzureConnectionString); } }

        #region Scaling

        [Export(CompositionIdentifiers.AzureServiceName)]
        public static string AzureServiceName { get { return GetSettingValue(CompositionIdentifiers.AzureServiceName); } }

        [Export(CompositionIdentifiers.AzureSubscriptionId)]
        public static string AzureSubscriptionId { get { return GetSettingValue(CompositionIdentifiers.AzureSubscriptionId); } }

        [Export(CompositionIdentifiers.AzureMngmtCertThumbprint)]
        public static string AzureMngmtCertThumbprint { get { return GetSettingValue(CompositionIdentifiers.AzureMngmtCertThumbprint); } }

        #endregion

        [Export(CompositionIdentifiers.ProcessIdentityFilename)]
        public static string ProcessIdentityFilename { get { return Path.Combine(RootFolder, "gwProcessIdentity.txt"); } }

        [Export(CompositionIdentifiers.LocalJobSummissionEndPoint)]
        public static string LocalJobSummissionEndPoint { get { return "http://localhost/AcceptLocalJobs/dev_Enviroment_Port1"; } }

        [Export(CompositionIdentifiers.SerializedGlobalSecurityPolicy)]
        public static string SerializedGlobalSecurityPolicy { get { return GetSettingValue(CompositionIdentifiers.SerializedGlobalSecurityPolicy); } }

        [Export(CompositionIdentifiers.WCFSharedMachineKey)]
        public static string WCFSharedMachineKey { get { return GetSettingValue(CompositionIdentifiers.GenericWorkerConnectionString); } }

        [Export(CompositionIdentifiers.SecurityAllowInsecureAccess)]
        public static bool SecurityAllowInsecureAccess
        {
            get
            {
                Trace.TraceWarning("Security disabled for testing");
         
                return true;
            }
        }


        [Export(CompositionIdentifiers.GenericWorkerIsAccountingOn)]
        public static bool GenericWorkerIsAccountingOn
        {
            get
            {
                Trace.TraceWarning("Accounting is always on for testing");
         
                return true;
            }
        }

        private static object _instanceIdentityLock = new object();
        private static int _instanceIdentityCounter = 1;
        private static int GetNumber()
        {
            lock (_instanceIdentityLock)
            {
                return _instanceIdentityCounter++;
            }
        }

        [Export(CompositionIdentifiers.InstanceIdentity)]
        public static string InstanceIdentity
        {
            get
            {
                return string.Format("worker-{0:yyyy.MM.dd-HHmmss}-number-{1:000}",
                    DateTime.UtcNow, GetNumber());
            }
        }

        /// <summary>
        /// Gets the number of job entries per page.
        /// Currently the maximum number of job entries fitting in a WCF message is 333. 
        /// A Paging mechanism is needed to allow users retrieve job lists with more than 333 entires.
        /// In order to have shorter execution time for tests concerning paging, we use here a pagesize of 20 job entries.
        /// </summary>
        [Export(CompositionIdentifiers.JobEntriesPerPage)]
        public static int JobEntriesPerPage
        {
            get
            {
                return 20;
            }
        }
    }    
}
