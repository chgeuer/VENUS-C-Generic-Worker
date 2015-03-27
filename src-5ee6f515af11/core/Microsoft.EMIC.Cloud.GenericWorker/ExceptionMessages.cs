//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.EMIC.Cloud
{
    /// <summary>
    /// A static class that collects all the exception messages used in the system.
    /// </summary>
    public static class ExceptionMessages
    {
        #region Microsoft.EMIC.Cloud.Administrator.Host
        /// <summary>
        /// Exception message string used when the caller is not the expected caller
        /// </summary>
        public static readonly string IllegalCaller = "Illegal caller {0}, should have been {1}.";
        #endregion

        #region Microsoft.EMIC.Cloud.AzureSettings

        /// <summary>
        /// Exception message string used when the specified storage object does not exist
        /// </summary>
        public static readonly string StorageMEFSetting = "Cannot determine storage container/table/queue name for MEF setting '{0}'.";

        #endregion

        #region Microsoft.EMIC.Cloud.COMPSsClient
        /// <summary>
        /// Exception message string used when the method is not supported in COMPSs
        /// </summary>
        public static readonly string COMPSsRemoveTerminatedJobs = "RemoveTerminatedJobs is not supported in COMPSs.";
        /// <summary>
        /// Exception message string used when the method is not supported in COMPSs
        /// </summary>
        public static readonly string COMPSsGetNumberOfJobs = "GetNumberOfJobs is not supported in COMPSs.";
        /// <summary>
        /// Exception message string used when the method is not supported in COMPSs
        /// </summary>
        public static readonly string COMPSsGetJobs = "GetJobs is not supported in COMPSs.";
        /// <summary>
        /// Exception message string used when the method is not supported in COMPSs
        /// </summary>
        public static readonly string COMPSsGetAllJobs = "GetAllJobs is not supported in COMPSs.";
        /// <summary>
        /// Exception message string used when the method is not supported in COMPSs
        /// </summary>
        public static readonly string COMPSsGetHierarchy = "GetHierarchy is not supported in COMPSs.";
        /// <summary>
        /// Exception message string used when the method is not supported in COMPSs
        /// </summary>
        public static readonly string COMPSsGetRoot = "GetRoot is not supported in COMPSs.";
        /// <summary>
        /// Exception message string used when the method is not supported in COMPSs
        /// </summary>
        public static readonly string COMPSsGetJobsByGroup = "GetJobsByGroup is not supported in COMPSs.";
        /// <summary>
        /// Exception message string used when the method is not supported in COMPSs
        /// </summary>
        public static readonly string COMPSsCancelGroup = "CancelGroup is not supported in COMPSs.";
        /// <summary>
        /// Exception message string used when the method is not supported in COMPSs
        /// </summary>
        public static readonly string COMPSsCancelHierarchy = "CancelHierarchy is not supported in COMPSs.";
        #endregion

        #region Microsoft.EMIC.Cloud.DocumentationDemo

        /// <summary>
        /// Exception message string used when parent jobID is missing
        /// </summary>
        public static readonly string ParentJobID = "Parent jobId cannot be read from environment variables.";

        #endregion

        #region Microsoft.EMIC.Cloud.GenericWorker
        
        #region FaultExceptions

        internal static readonly string ChildrenJobNotAccepted = "Children Job is Not Accepted by AcceptLocal Job Method. Please take a look at the Detail.Message section of this fault for more details.";
        internal static readonly string ServiceBusy = "Service is busy and could not accept new jobs: The details are: {0}";
        internal static readonly string JobDoesNotExistError = "Job with the specified endpoint reference {0} cannot be found in the system. Please check the endpoint reference";
        internal static readonly string GeneralRequestError = "Exception Encountered in {0} Method. Please see the details : {1}";
        internal static readonly string CancelGroupQueue = "CancelGroup task could not be queued. The task queue reached its maximum size. Wait until some tasks are processed and try again.";
        internal static readonly string RemoveTerminatedJobsQueue = "RemoveTerminatedJobs task could not be queued. The task queue reached its maximum size. Wait until some tasks are processed and try again.";
        internal static readonly string JobNotAccepted = "Job is Not Accepted by the CreateActivity Method. Please take a look at the Detail.Message section of this fault for more details.";
        internal static readonly string DefineStatuses = "You have to define statuses.";
        internal static readonly string ProvidePluginConfig = "You have to provide a pluginConfig.";
        internal static readonly string ProvideValidJobReference = "You have to provide a valid job reference.";        
        internal static readonly string IdentityNotRetrieved = "Identity could not be retrieved, even though security is enabled.";
        internal static readonly string NameClaimNotMatching = "Name Claim is not matching the parameter for the job owner.";
        internal static readonly string JobAlreadyTerminated = "Job is already terminated.";
        internal static readonly string JobStatusMoreThanOnce = "No job status should appear more than once in your list.";
        internal static readonly string JobNotFound = "Job not found.";
        internal static readonly string NotificationOnlyFinishedFailed = "For group level notifications only the statuses finished and failed are supported.";
        internal static readonly string RuntimeScalingPlugin = "There was a runtime exception in the scaling plugin. Check the inner exception for more details.";
        internal static readonly string DeploymentSizeNull = "Please provide a non null DeploymentSize instance.";
        internal static readonly string InstanceCount = "It is not possible to set the instance count for a deployment to: {0}.";
        internal static readonly string NoScalingPlugin = "There is no scaling plugin available for cloud provider: {0}.";
        internal static readonly string TerminateActivityStateError = "Job is not in a status that can be terminated. Job status is: {0}";
        /// <summary>
        /// Exception message string used when a security exception occurs because the request is not authenticated
        /// </summary>
        public static readonly string UnauthenticatedCall = "Unauthenticated calls are not allowed. The error details are : {0}";

        #endregion

        internal static readonly string AzureArgSingleReferenceChanged = @"The underlying representation of AzureArgumentSingleReference arguments has changed, 
            please reinstall your application and run your jobs again.";
        internal static readonly string NoImplementationToLoad = @"There is no implementation available to load {{{0}}}{1}.";
        internal static readonly string CanNotLocateType = @"Cannot locate type for {{{0}}}{1}.";
        internal static readonly string CanNotCreateInstance = @"Cannot create instance of type {0}.";
        internal static readonly string FileUploadsNotSupportedByHttpGetReference = @"FileUploads are not supported by the HttpGetReference.";
        internal static readonly string FileDoesNotExist = "Could not upload file. File {0} does not exist.";
        internal static readonly string ProviderSpecificReference = "The Reference element needs to have a child element for the ProviderSpecificReference.";
        internal static readonly string NeedReferenceObject = "You need to provide a Reference object.";
        internal static readonly string AtLeastOneReference = "A ReferenceArray needs to have at least one reference.";
        internal static readonly string SingleReferenceChild = "The SingleReference element needs to have a child element named Reference.";
        internal static readonly string CancelHierarchyQueue = "CancelHierarchy task could not be queued. The task queue reached its maximum size. Wait until some tasks are processed and try again.";
        internal static readonly string UnhandledArgType = "Unknown arg type is provided. Please check your application description.";
        internal static readonly string CanNotSetStatus = "Cannot set {0} status to {1}, tried {2} times.";
        internal static readonly string CanNotResetStatus = "Cannot reset {0} to {1}, tried {2} times.";
        internal static readonly string RuntimeNull = "Value of 'runtime' must not be null.";
        internal static readonly string CanNotDetermineParameters = "Cannot determine parameters from local connection.";
        internal static readonly string CanNotDetermineProcess = "Cannot determine originating process for local connection from {0}:{1}.";
        internal static readonly string LocalUserMap = "Local Windows user \"{0}\" maps to owner \"{1}\".";
        internal static readonly string ScalingPlugin = "There was a runtime exception in the scaling plugin.";
        internal static readonly string InvalidXMLElementCount = "Invalid XmlElement count. Make sure the STSIssuerNameRegistry has one entry. Please also check if the certificate configured under Microsoft.EMIC.Cloud.STS.Certificate.Thumbprint is available on the server under LocalMachine\\My.";
        internal static readonly string ConfusingAttributes = @"Confusing attributes; element must either contain one {0} to refer to 
                            the certificate's thumbprint, or it must contain the four attributes {1}/{2}/{3}/{4} 
                            to refer to a locally installed certificate. 
                            The problematic element is this: \r\n{5}.";
        internal static readonly string MissingAttribute = "Missing {0} attribute. The STSIssuerNameRegistry has to contain this attribute.";
        internal static readonly string ImpersonationFailed = "Impersonation failed.";
        internal static readonly string AlreadyDisposed = "Already disposed.";
        internal static readonly string UndoTwice = "Cannot undo twice.";
        internal static readonly string IncomingTokenNotScoped = "The incoming token for '{0}' is not scoped to the endpoint '{1}'.";
        internal static readonly string FederatedServiceCredentials = "Cannot retrieve FederatedServiceCredentials.";
        internal static readonly string ServiceCertificate = "Cannot retrieve ServiceCertificate.";
        internal static readonly string ClaimSetAtLeastOneClaim = "Provided ClaimSet must contain at least one claim. Please check the usertable in the storage account used by the GW.";
        internal static readonly string MustBeInRange = "The keysize of the symmetric proof key has to be in the range of 128 to 2048 bits. ";
        internal static readonly string CanNotFindFolder = "Cannot find folder {0} in address {1}. Has the service been moved?";
        internal static readonly string ErrorInsufficentBuffer = "ERROR_INSUFFICIENT_BUFFER!";
        internal static readonly string ErrorInvalidParameter = "ERROR_INVALID_PARAMETER!";
        internal static readonly string UnknownErrorCode = "Unknown error code \"{0}\". See DOS error codes in <error.h>.";
        internal static readonly string IllegalServiceType = "Illegal serviceType, must be a {0}.";
        internal static readonly string SymmetricSecurityBindingElement = "Cannot locate SymmetricSecurityBindingElement.";
        internal static readonly string ProtectionTokenParameters = "Cannot locate ProtectionTokenParameters.";
        internal static readonly string SecureConversationSecurityTokenParameters = "Cannot locate SecureConversationSecurityTokenParameters.";
        internal static readonly string CanNotCreateMEXBinding = "Cannot create MEX binding for URI Scheme {0}.";
        internal static readonly string FedUtilCertificateNotFound = "FedUtil: Certificate {0} not found in store {2}/{3} (there were {1} certs matching the query).";
        internal static readonly string UnprotectedEndpointNotExposed = "Unprotected endpoint is not exposed. Set Azure setting {0} to true to enable insecure access.";
        #endregion

        #region Microsoft.EMIC.Cloud.GenericWorker.SampleNotifications
        /// <summary>
        /// Exception message string used when plugin config does not specify a connectionstring 
        /// </summary>
        public static readonly string SubscriptionConnStr = "Subscription for queueplugin did not specify a connectionstring.";
        /// <summary>
        /// Exception message string used when plugin config does not specify a queuename 
        /// </summary>
        public static readonly string SubscriptionQueueName = "Subscription for queueplugin did not specify a queuename.";
        #endregion

        #region Microsoft.EMIC.Cloud.GenericWorker.AzureProvider

        /// <summary>
        /// Exception message string used when finished job data can not be saved in accounting
        /// </summary>
        public static readonly string FinishedJobSavedInAccounting = "The  data of the finished job {0} is tried to be saved in accounting.";
        /// <summary>
        /// Exception message string used when record not found
        /// </summary>
        public static readonly string SettingStartTime = "Existing record for the job {0} cannot be found while setting the start time in accounting.";
        /// <summary>
        /// Exception message string used when record not found
        /// </summary>
        public static readonly string ResettingData = "Existing record for the job {0} cannot be found while reseting data in accounting.";
        /// <summary>
        /// Exception message string used when record not found
        /// </summary>
        public static readonly string SavingNetworkData = "Existing record for the job {0} cannot be found while saving network data in accounting.";
        /// <summary>
        /// Exception message string used when record not found
        /// </summary>
        public static readonly string SavingStorageData = "Existing record for the job {0} cannot be found while saving storage data in accounting.";
        /// <summary>
        /// Exception message string used when mutex can not be claimed
        /// </summary>
        public static readonly string CanNotClaimMutex = "Cannot claim Mutex.";
        /// <summary>
        /// Exception message string used when job already exists
        /// </summary>
        public static readonly string JobExists = "Job exists in the system.";
        /// <summary>
        /// Exception message string used when parent job not found
        /// </summary>
        public static readonly string ParentJobNotFound = "Parent Job cannot be found in the system.";
        /// <summary>
        /// Exception message string used when parent job ID not found
        /// </summary>
        public static readonly string ParentJobIDNotFound = "Parent Job ID cannot be found in the system";
        /// <summary>
        /// Exception message string used when another job with same customerID exists in the hierarchy
        /// </summary>
        public static readonly string UniqueHierarchicalNameError = "Another job who has the same customer Job ID is already in the system. Hierarhical jobs' CustomerJOBIDs should be unique.";
        /// <summary>
        /// Exception message string used when one of the ancestors of the jon is already failed or cancelled
        /// </summary>
        public static readonly string AncestorIsFailedorCancelled = "One of the ancestors of the job is alredy failed or cancelled. The job cannot be accepted.";
        /// <summary>
        /// Exception message string used when one of the ancestor of the job cannot be found in the system
        /// </summary>
        public static readonly string AncestorCannotFound = "One of the ancestors of the job cannot be found in the system. The job cannot be accepted.";
        /// <summary>
        /// Exception message string used when one level policy in hierarchy is broken
        /// </summary>
        public static readonly string LevelHierarchyError = "Parent jobs can only add jobs to one level below. The other operations are prohibited.";
        /// <summary>
        /// Exception message string used when group head not found
        /// </summary>
        public static readonly string GroupHeadNotFound = "Could not find a group head for the specified groupname.";
        /// <summary>
        /// Exception message string used when job not part of a group
        /// </summary>
        public static readonly string JobNotPartOfAGroup = "Specified job is not part of a group.";
        /// <summary>
        /// Exception message string used when group not exists
        /// </summary>
        public static readonly string NoGroupWithName = "There is no group with the provided name stored under your useraccount in the system.";
        /// <summary>
        /// Exception message string used when subscription container is null
        /// </summary>
        public static readonly string SubscriptionContainerNull = "The given subscription container is null.";
        /// <summary>
        /// Exception message string used when scaling certificate is missing
        /// </summary>
        public static readonly string ScalingCertificateNotFound = @"The specified certificate for scaling could not be found. Please check if the management certificate is installed in Localmachine\\My
                            The searched certificates thumbprint is: {0}.";
        /// <summary>
        /// Exception message string used when service name, subscription id, or management thumbprint is missing
        /// </summary>
        public static readonly string ScalingConfigIncomplete = "Your scaling configuration seems to be incomplete. Please check the documentation for scaling for more information.";
        /// <summary>
        /// Exception message string used when instance count is tried to be set to 0
        /// </summary>
        public static readonly string InstanceCountZero = "The number of instances cannot be set to 0. Please use the tools of your CloudProvider to remove the deployment.";
        #endregion

        #region Microsoft.Emic.Cloud.Storage.Azure
        /// <summary>
        /// Exception message string used when download of a component fails
        /// </summary>
        public static readonly string DownloadError = @"Download Error {0}.";
        /// <summary>
        /// Exception message string used when the cloud provider is not Azure
        /// </summary>
        public static readonly string NotAzure = "Not Azure?!?";

        #region CatalogHandler
        /// <summary>
        /// Exception message string used when get command is used without first initializing the catalog
        /// </summary>
        public static readonly string AzureCatalogNotInitialized = "AzureCatalogue has not been initialized.";
        /// <summary>
        /// Exception message string used in CatalogHandler when the specified logical name not found
        /// </summary>
        public static readonly string ArgumentNotFound = "The argument with PartitionKey {0} and logical name \"{1}\" cannot be found.";
        #endregion

        #endregion
        
        #region Services

        #region Common
        /// <summary>
        /// Exception message string used when an instance is not of the requested type, thus must be an other type
        /// </summary>
        public static readonly string MustBe = "The provided security token must be of type {0}.";
        /// <summary>
        /// Exception message string used when WCFSharedMachineKey is not set in the composition container
        /// </summary>
        public static readonly string WCFSharedMachineKey = "Cannot determine WCFSharedMachineKey.";
        /// <summary>
        /// Exception message string when authentication fails
        /// </summary>
        public static readonly string InvalidUsernamePassword = "Invalid username or password.";
        #endregion

        #region STS
        /// <summary>
        /// Exception message string used when caller principal is null
        /// </summary>
        public static readonly string CallerPrincipalNull = "The caller's principal is null.";
        /// <summary>
        /// Exception message string used when principal has no identities
        /// </summary>
        public static readonly string NoClaimsIdentityDefined = "The caller's principal has no ClaimsIdentity defined.";
        /// <summary>
        /// Exception message string used when the user does not exist
        /// </summary>
        public static readonly string CanNotFindUser = "Cannot find user {0} in database.";
        /// <summary>
        /// Exception message string used when security token service configuration is not of the expected type
        /// </summary>
        public static readonly string SecurityTokenServiceConfiguration = "SecurityTokenServiceConfiguration must be a {0}.";
        /// <summary>
        /// Exception message string used when security token service configuration is not of the expected type
        /// </summary>
        public static readonly string SecurityTokenServiceConfigurationWrong = "The SecurityTokenServiceConfiguration was expected to be a {0}, but was {1}.";
        /// <summary>
        /// Exception message string used when security token service configuration is not of the expected type
        /// </summary>
        public static readonly string WrongType = "The type {0} is not a {1}.";
        #endregion

        #endregion
    }
}

