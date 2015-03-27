//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.Samples.WindowsAzure.ServiceManagement;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Security.Cryptography.X509Certificates;
using System.ComponentModel.Composition;
using System.ServiceModel.Activation;
using System.Diagnostics;
using System.Xml.Linq;

namespace Microsoft.EMIC.Cloud.GenericWorker.AzureProvider
{

    /// <summary>
    /// This class serves as an unimplemented skeleton for scaling plugins
    /// </summary>
    [System.ServiceModel.ServiceBehaviorAttribute(InstanceContextMode = System.ServiceModel.InstanceContextMode.PerCall, ConcurrencyMode = System.ServiceModel.ConcurrencyMode.Multiple)]
    public class ScalingServiceType : IScalingService, IPartImportsSatisfiedNotification
    {
        /// <summary>
        /// Returns a collection of objects of type DeploymentSize to describe the current deployments of the GW.
        /// </summary>
        /// <returns></returns>
        public virtual List<DeploymentSize> ListDeployments()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Updates the number of instances of a deployment.
        /// </summary>
        /// <param name="newDeploymentSize">New size of the deployment.</param>
        public void UpdateDeployment(DeploymentSize newDeploymentSize)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Called when a part's imports have been satisfied and it is safe to use.
        /// </summary>
        public void OnImportsSatisfied()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// This plugin is to provide scaling for azure deployments
    /// </summary>
    [Export(typeof(IScalingPlugin))] 
    public class AzureScalingPlugin : IScalingPlugin, IPartImportsSatisfiedNotification
    {
        const string CloudProviderID = "Azure";
        const string Slot = "production";
        const string ServiceEndpoint = "https://management.core.windows.net";

        IServiceManagement managementClient;

        #pragma warning disable 0649 //these values are provides by MEF

        [Import(CompositionIdentifiers.AzureServiceName)]
        string ServiceName;

        [Import(CompositionIdentifiers.AzureSubscriptionId)]
        string SubscriptionId;

        [Import(CompositionIdentifiers.AzureMngmtCertThumbprint)]
        string ManagementCertificateThumbprint;

        #pragma warning restore 0649

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureScalingPlugin"/> class.
        /// </summary>
        public AzureScalingPlugin()
        {

        }

        private static string EncodeToBase64String(string original)
        {
            if (string.IsNullOrEmpty(original))
            {
                return original;
            }
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(original));
        }

        private static string DecodeFromBase64String(string original)
        {
            if (string.IsNullOrEmpty(original))
            {
                return original;
            }
            return Encoding.UTF8.GetString(Convert.FromBase64String(original));
        }

        private static Binding WebHttpBinding()
        {
            var binding = new WebHttpBinding(WebHttpSecurityMode.Transport);
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;
            binding.ReaderQuotas.MaxStringContentLength = 67108864;

            return binding;
        }

        static X509Certificate2 GetCertificate(string certThumbPrint)
        {
            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
            var certs = store.Certificates.Find(X509FindType.FindByThumbprint, certThumbPrint, false);
            if (certs.Count == 0)
            {
                throw new SystemException(string.Format(ExceptionMessages.ScalingCertificateNotFound, certThumbPrint));
            }
            return certs[0];
        }

        string IScalingPlugin.CloudProviderID
        {
            get { return CloudProviderID; }
        }

        private void throwOnInvalidConfiguration()
        {
            if (string.IsNullOrWhiteSpace(ServiceName) || string.IsNullOrWhiteSpace(SubscriptionId) || string.IsNullOrWhiteSpace(ManagementCertificateThumbprint))
            {
                throw new Exception(ExceptionMessages.ScalingConfigIncomplete);
            }
        }

        /// <summary>
        /// Property to access the instance count of a deployment
        /// </summary>
        /// <value>
        /// The instance count.
        /// </value>
        public int InstanceCount
        {
            get
            {
                throwOnInvalidConfiguration();
                using (new OperationContextScope((IContextChannel)managementClient))
                {
                    var deployment = managementClient.GetDeploymentBySlot(SubscriptionId, ServiceName, Slot);

                    var instances = deployment.RoleInstanceList.Count;

                    return instances;
                }
            }
            set
            {
                throwOnInvalidConfiguration();
                var newInstanceCount = value;
                if (newInstanceCount == 0)
                    throw new NotSupportedException(ExceptionMessages.InstanceCountZero);

                var changeConfigurationInput = new ChangeConfigurationInput();

                using (new OperationContextScope((IContextChannel)managementClient))
                {
                    var deployment = managementClient.GetDeploymentBySlot(SubscriptionId, ServiceName, Slot);
                    var oldInstanceCount = deployment.RoleInstanceList.Count;
                    if (oldInstanceCount == value)
                    {
                        Trace.TraceInformation("******************Scaling - Update has no effect on the deployment");
                        return;
                    }
                    string configurationXml = ServiceManagementHelper.DecodeFromBase64String(deployment.Configuration);

                    var serviceConfiguration = XDocument.Parse(configurationXml);

                    serviceConfiguration
                    .Descendants()
                    .Single(d => d.Name.LocalName == "Role" && d.Attributes().Single(a => a.Name.LocalName == "name").Value == "Cloud.WebRole")
                    .Elements()
                    .Single(e => e.Name.LocalName == "Instances")
                    .Attributes()
                    .Single(a => a.Name.LocalName == "count").Value = newInstanceCount.ToString();


                    changeConfigurationInput.Configuration = EncodeToBase64String(serviceConfiguration.ToString(SaveOptions.DisableFormatting));
                }
                using (new OperationContextScope((IContextChannel)managementClient))
                {
                    Trace.TraceInformation("Scaling - set Instance number for {0} to {1}", CloudProviderID, newInstanceCount);

                    managementClient.ChangeConfigurationBySlot(SubscriptionId, ServiceName, Slot, changeConfigurationInput);
                }
            }
        }

        /// <summary>
        /// Called when a part's imports have been satisfied and it is safe to use.
        /// </summary>
        public void OnImportsSatisfied()
        {
            managementClient = Microsoft.Samples.WindowsAzure.ServiceManagement.ServiceManagementHelper.CreateServiceManagementChannel(
WebHttpBinding(), new Uri(ServiceEndpoint), GetCertificate(ManagementCertificateThumbprint));
        }
    }
}
