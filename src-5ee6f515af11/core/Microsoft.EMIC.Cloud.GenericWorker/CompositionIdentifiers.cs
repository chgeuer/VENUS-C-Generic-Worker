//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Microsoft.EMIC.Cloud
{
    using System.Collections.Generic;

    /// <summary>
    /// Contains string literals for use with a <see cref="System.ComponentModel.Composition.Hosting.CompositionContainer"/>.
    /// </summary>
    public static class CompositionIdentifiers
    {
        /// <summary>
        /// Provides the Windows Azure Storage table/blob/queue names for a production deployment. 
        /// </summary>
        public static Dictionary<string, string> ProductionStorageName = new Dictionary<string, string>()
                                                                             {
                                                                                 { DevelopmentGenericWorkerBlobName, "gwjobblobstore" },
                                                                                 { DevelopmentGenericWorkerDetailsTableName, "gwjobdetails" },
                                                                                 { DevelopmentGenericWorkerIndexTableName, "gwjobindex" },
                                                                                 { DevelopmentGenericWorkerHygieneTableName, "gwhygiene" },
                                                                                 { DevelopmentSecurityTokenServiceUserTableName, "gwstsusers" },
                                                                                 { DevelopmentGenericWorkerAccountingTableName, "gwaccountingtable"},
                                                                                 { DevelopmentGenericWorkerSubscriptionsBlobName, "gwnotificationsubscriptions"},
                                                                                 { DevelopmentGenericWorkerProgressQueueName, "gwprogressqueue"}
                                                                             };

        /// <summary>
        /// A Dictionary for system-wide constants (like port names)
        /// </summary>
        public static Dictionary<string, string> Constants = new Dictionary<string, string>() 
                                                                            {
                                                                                { OGFWsdlPortName, "BESFactoryPortTypePort"}
                                                                            };

        /// <summary>
        /// Configuration key for Diagnostics Connection String
        /// </summary>
        public const string DiagnosticsConnectionString = "Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString";
        /// <summary>
        /// Configuration key for GenericWorker Connection String
        /// </summary>
        public const string GenericWorkerConnectionString = "Microsoft.EMIC.Cloud.GenericWorker.ConnectionString";

        /// <summary>
        /// Configuration key for the number of parallel GenericWorker Tasks
        /// </summary>
        public const string GenericWorkerParallelTasks = "Microsoft.EMIC.Cloud.GenericWorker.ParallelTasks";

        /// <summary>
        /// Configuration key for GenericWorker URL
        /// </summary>
        public const string GenericWorkerURL = "Microsoft.EMIC.Cloud.GenericWorker.URL";

        /// <summary>
        /// Configuration key for Azure Service Name
        /// </summary>
        public const string AzureServiceName = "Microsoft.EMIC.Cloud.Azure.ServiceName";
        /// <summary>
        /// Configuration key for Azure Subscription Id
        /// </summary>
        public const string AzureSubscriptionId = "Microsoft.EMIC.Cloud.Azure.SubscriptionId";
        /// <summary>
        /// Configuration key for Azure Management Certificate Thumbprint
        /// </summary>
        public const string AzureMngmtCertThumbprint                = "Microsoft.EMIC.Cloud.Azure.MgmtCertThumbprint";

        /// <summary>
        /// Configuration key for GenericWorker Is Accounting On Switch
        /// </summary>
        public const string GenericWorkerIsAccountingOn = "Microsoft.EMIC.Cloud.GenericWorker.IsAccountingOn";
        /// <summary>
        /// Configuration key for Scaling Service URL
        /// </summary>
        public const string ScalingServiceURL = "Microsoft.EMIC.Cloud.ScalingService.URL";
        /// <summary>
        /// Configuration key for Notification Service URL
        /// </summary>
        public const string NotificationServiceURL = "Microsoft.EMIC.Cloud.NotificationService.URL";
        /// <summary>
        /// Configuration key for STS Certificate Thumbprint
        /// </summary>
        public const string STSCertificateThumbprint = "Microsoft.EMIC.Cloud.STS.Certificate.Thumbprint";
        /// <summary>
        /// Configuration key for STS URL
        /// </summary>
        public const string STSURL = "Microsoft.EMIC.Cloud.STS.URL";
        /// <summary>
        /// Configuration key for Passive STS URL
        /// </summary>
        public const string PassiveSTSURL = "Microsoft.EMIC.Cloud.PassiveSTS.URL";
        /// <summary>
        /// Configuration key for Secured Job Management Site URL
        /// </summary>
        public const string SecuredJobManagementSiteURL = "Microsoft.EMIC.Cloud.SecuredJobManagementSiteURL";
        /// <summary>
        /// Configuration key for STS On Azure Connection String
        /// </summary>
        public const string STSOnAzureConnectionString = "Microsoft.EMIC.Cloud.STS.Azure.ConnectionString";
        /// <summary>
        /// Configuration key for WCF Shared Machine Key
        /// </summary>
        public const string WCFSharedMachineKey = "Microsoft.EMIC.Cloud.WCF.SharedMachineSymmetricKey";
        /// <summary>
        /// Configuration key for Serialized Global Security Policy
        /// </summary>
        public const string SerializedGlobalSecurityPolicy = "Microsoft.EMIC.Cloud.SerializedGlobalSecurityPolicy";
        /// <summary>
        /// Configuration key for Security Allow In Secure Access Switch
        /// </summary>
        public const string SecurityAllowInsecureAccess             = "Microsoft.EMIC.Cloud.Security.AllowInsecureAccess";
        /// <summary>
        /// Configuration key for the number of Job Entries that will be showed Per Page
        /// </summary>
        public const string JobEntriesPerPage                       = "Microsoft.EMIC.Cloud.GenericWorker.JobEntriesPerPage";

        /// <summary>
        /// Configuration key for Process Identity Filename
        /// </summary>
        public const string ProcessIdentityFilename = "ProcessIdentityFilename";
        /// <summary>
        /// Configuration key for Local Job Summission End Point
        /// </summary>
        public const string LocalJobSummissionEndPoint = "LocalJobSummissionEndPoint";
        /// <summary>
        /// Configuration key for Instance Identity
        /// </summary>
        public const string InstanceIdentity = "InstanceIdentity";

        /// <summary>
        /// Configuration key for GenericWorker Directory Application Installation
        /// </summary>
        public const string GenericWorkerDirectoryApplicationInstallation = "GenericWorkerDirectoryApplicationInstallation";
        /// <summary>
        /// Configuration key for GenericWorker Directory User Folder
        /// </summary>
        public const string GenericWorkerDirectoryUserFolder = "GenericWorkerDirectoryUserFolder";
        /// <summary>
        /// Configuration key for GenericWorker Directory Reference DataSets
        /// </summary>
        public const string GenericWorkerDirectoryReferenceDataSets = "GenericWorkerDirectoryReferenceDataSets";

        /// <summary>
        /// Configuration key for GenericWorker Development Blob Name
        /// </summary>
        public const string DevelopmentGenericWorkerBlobName = "Microsoft.EMIC.Cloud.Development.GenericWorker.BlobName";
        /// <summary>
        /// Configuration key for GenericWorker Development Subscription Blob Name
        /// </summary>
        public const string DevelopmentGenericWorkerSubscriptionsBlobName= "Microsoft.EMIC.Cloud.Development.GenericWorker.NotificationSubscriptionsBlobName";

        /// <summary>
        /// Configuration key for GenericWorker Development Details Table Name
        /// </summary>
        public const string DevelopmentGenericWorkerDetailsTableName = "Microsoft.EMIC.Cloud.Development.GenericWorker.DetailsTableName";
        /// <summary>
        /// Configuration key for GenericWorker Development Index Table Name
        /// </summary>
        public const string DevelopmentGenericWorkerIndexTableName = "Microsoft.EMIC.Cloud.Development.GenericWorker.IndexTableName";
        /// <summary>
        /// Configuration key for GenericWorker Development Hygiene Table Name
        /// </summary>
        public const string DevelopmentGenericWorkerHygieneTableName = "Microsoft.EMIC.Cloud.Development.GenericWorker.HygieneTableName";
        /// <summary>
        /// Configuration key for Development STS User Table Name
        /// </summary>
        public const string DevelopmentSecurityTokenServiceUserTableName = "Microsoft.EMIC.Cloud.Development.STS.Azure.UserTableName";
        /// <summary>
        /// Configuration key for GenericWorker Development Accounting Table Name
        /// </summary>
        public const string DevelopmentGenericWorkerAccountingTableName = "Microsoft.EMIC.Cloud.Development.GenericWorker.AccountingTableName";

        /// <summary>
        /// Configuration key for GenericWorker Development Progress Queue Name
        /// </summary>
        public const string DevelopmentGenericWorkerProgressQueueName = "Microsoft.EMIC.Cloud.Development.GenericWorker.ProgressQueueName";

        /// <summary>
        /// Configuration key for Tests Connection String
        /// </summary>
        public const string TestsConnectionString = "Microsoft.EMIC.Cloud.Tests.ConnectionString";
        /// <summary>
        /// Configuration key for Live Tests Connection String
        /// </summary>
        public const string LiveTestsConnectionString                     = "Microsoft.EMIC.Cloud.LiveTests.ConnectionString";

        /// <summary>
        /// Configuration key for Demo User Blob Name
        /// </summary>
        public const string DemoUserBlobName = "Microsoft.EMIC.Cloud.Demo.User.BlobName";
        /// <summary>
        /// Configuration key for Demo User Connection String
        /// </summary>
        public const string DemoUserConnectionString                      = "Microsoft.EMIC.Cloud.Demo.User.ConnectionString";

        /// <summary>
        /// Configuration key for Account Encrypted Password
        /// </summary>
        public const string AccountEncryptedPassword = "Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountEncryptedPassword";
        /// <summary>
        /// Configuration key for Account User Name
        /// </summary>
        public const string AccountUserName                                 = "Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountUsername";

        /// <summary>
        /// Configuration key for SSL Cerfificate
        /// </summary>
        public const string SSLCert = "Microsoft.EMIC.Cloud.SSLCert";
        /// <summary>
        /// Configuration key for STS Certificate
        /// </summary>
        public const string STSCert = "Microsoft.EMIC.Cloud.STSCert";
        /// <summary>
        /// Configuration key for Management Certificate
        /// </summary>
        public const string MgmtCert = "Microsoft.EMIC.Cloud.MgmtCert";
        /// <summary>
        /// Configuration key for Password Encrypted Certificate
        /// </summary>
        public const string PasswordEncrypCert                              = "Microsoft.WindowsAzure.Plugins.RemoteAccess.PasswordEncryption";

        /// <summary>
        /// Configuration key for OGF Target Namespace
        /// </summary>
        public const string OGFTargetNamespace                              = "http://schemas.ggf.org/bes/2006/08/bes-factory";
        /// <summary>
        /// Configuration key for OGF Wsdl Port Name
        /// </summary>
        public const string OGFWsdlPortName                                 = "OGF.BES.WSDLPortName";

        /// <summary>
        /// Check whether the deployment is on Azure WebRole or not
        /// </summary>
        public const string IsGWDeployedOnAzureWebRole = "Microsoft.EMIC.Cloud.GenericWorker.IsWebRole";
    }
}
