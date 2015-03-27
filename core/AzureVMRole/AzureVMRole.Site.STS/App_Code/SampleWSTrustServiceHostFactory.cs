//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Microsoft.EMIC.Cloud.STS
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition.Hosting;
    using System.Linq;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using System.ServiceModel;
    using System.ServiceModel.Activation;
    using System.ServiceModel.Description;
    using System.Web.Compilation;
    using Microsoft.EMIC.Cloud.AzureSettings;
    using Microsoft.EMIC.Cloud.Security;
    using Microsoft.EMIC.Cloud.Utilities;
    using Microsoft.IdentityModel.Configuration;
    using Microsoft.IdentityModel.Protocols.WSTrust;
    using Microsoft.IdentityModel.Tokens;

    public class SampleWSTrustServiceHostFactory : ServiceHostFactory
    {
        public static readonly Type WsTrustType = typeof(IWSTrust13SyncContract);

        protected CompositionContainer container;

        public SampleWSTrustServiceHostFactory()
        {
            container = new CompositionContainer(new AggregateCatalog(
                new AssemblyCatalog(typeof(CompositionIdentifiers).Assembly),
                new AssemblyCatalog(typeof(AzureSettingsProvider).Assembly)));
        }

        public override ServiceHostBase CreateServiceHost(string constructorString, Uri[] baseAddresses)
        {
            if (string.IsNullOrEmpty(constructorString))
            {
                throw new ArgumentNullException("constructorString");
            }
            var securityTokenServiceConfiguration = this.CreateSecurityTokenServiceConfiguration(constructorString);
            if (securityTokenServiceConfiguration == null)
            {
                throw new InvalidOperationException();
            }

            var stsHost = new WSTrustServiceHost(securityTokenServiceConfiguration, baseAddresses);
            
            var stsBinding = WCFUtils.CreateUsernamePasswordSecurityTokenServiceBinding();
            stsHost.AddServiceEndpoint(WsTrustType, stsBinding, "");

            #region ServiceBehavior: AddressFilterMode.Any

            var sba = stsHost.Description.Behaviors.Find<ServiceBehaviorAttribute>();
            if (sba == null)
            {
                sba = new ServiceBehaviorAttribute();
                stsHost.Description.Behaviors.Add(sba);
            }
            sba.AddressFilterMode = AddressFilterMode.Any;

            #endregion

            #region ServiceMetadataBehavior

            var smb = stsHost.Description.Behaviors.Find<ServiceMetadataBehavior>();
            if (smb == null)
            {
                smb = new ServiceMetadataBehavior();
                stsHost.Description.Behaviors.Add(smb);
            }
            if (baseAddresses.Any(uri => uri.Scheme.Equals("http"))) smb.HttpGetEnabled = true;
            if (baseAddresses.Any(uri => uri.Scheme.Equals("https"))) smb.HttpsGetEnabled = true;


            #endregion

            #region ServiceDebugBehavior

            var sdb = stsHost.Description.Behaviors.Find<ServiceDebugBehavior>();
            if (sdb == null)
            {
                sdb = new ServiceDebugBehavior();
                stsHost.Description.Behaviors.Add(sdb);
            }
            sdb.IncludeExceptionDetailInFaults = true;

            #endregion

            #region UseRequestHeadersForMetadataAddressBehavior

            var urhfmab = new UseRequestHeadersForMetadataAddressBehavior();
            urhfmab.DefaultPortsByScheme.Add(new KeyValuePair<string, int>("http", 80));
            urhfmab.DefaultPortsByScheme.Add(new KeyValuePair<string, int>("https", 443));
            stsHost.Description.Behaviors.Add(urhfmab);

            #endregion

            #region Add SessionServiceBehavior

            var wcfSharedMachineKey = container.GetExportedValue<string>(CompositionIdentifiers.WCFSharedMachineKey);
            if (string.IsNullOrEmpty(wcfSharedMachineKey))
            {
                throw new Exception(ExceptionMessages.WCFSharedMachineKey);
            }
            var key = Convert.FromBase64String(wcfSharedMachineKey);

            WCFUtils.AddSessionServiceBehavior(stsHost, key);

            #endregion

            var certificateFindValue = container.GetExportedValue<string>(CompositionIdentifiers.STSCertificateThumbprint);
            stsHost.Credentials.ServiceCertificate.SetCertificate(StoreLocation.LocalMachine, StoreName.My, X509FindType.FindByThumbprint, certificateFindValue);
            var serviceCert = X509Helper.GetX509Certificate2(StoreLocation.LocalMachine, StoreName.My, certificateFindValue, X509FindType.FindByThumbprint);

            var wifServiceConfiguration = new ServiceConfiguration(loadConfig: false)
            {
                ServiceCertificate = serviceCert
            };

            #region Replace WindowsUserNameSecurityTokenHandler with SampleCustomUserNameSecurityTokenHandler

            var oldUPTokenHandler = wifServiceConfiguration.SecurityTokenHandlers.Where(sth => sth is WindowsUserNameSecurityTokenHandler).FirstOrDefault();
            if (oldUPTokenHandler != null)
            {
                wifServiceConfiguration.SecurityTokenHandlers.Remove(oldUPTokenHandler);
            }
            wifServiceConfiguration.SecurityTokenHandlers.Add(new SampleCustomUserNameSecurityTokenHandler(container));

            #endregion

            FederatedServiceCredentials.ConfigureServiceHost(stsHost, wifServiceConfiguration);

            return stsHost;
        }

        protected virtual SecurityTokenServiceConfiguration CreateSecurityTokenServiceConfiguration(string constructorString)
        {
            if (string.IsNullOrEmpty(constructorString))
            {
                throw new ArgumentNullException("constructorString");
            }
            var type = BuildManager.GetType(constructorString, throwOnError: true);
            if (!type.IsSubclassOf(typeof(SecurityTokenServiceConfiguration)))
            {
                throw new InvalidOperationException(string.Format(ExceptionMessages.WrongType, constructorString, typeof(SecurityTokenServiceConfiguration)));
            }
            const BindingFlags bindingFlags = BindingFlags.CreateInstance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

            object[] args = new object[1] { container };

            var cfg = (Activator.CreateInstance(type: type, bindingAttr: bindingFlags, binder: null,args: args, culture: null) as SampleVENUSSecurityTokenServiceConfiguration);

            return cfg;
        }
    }
}
