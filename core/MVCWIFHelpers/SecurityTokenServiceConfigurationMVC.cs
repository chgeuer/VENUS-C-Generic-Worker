//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace MVCWIFHelpers
{
    using System.Security.Cryptography.X509Certificates;
    using System.Web;
    using System.Web.Configuration;
    using Microsoft.IdentityModel.Configuration;
    using Microsoft.IdentityModel.SecurityTokenService;
    using Microsoft.EMIC.Cloud.Security;
    using System.ComponentModel.Composition.Hosting;
    using Microsoft.EMIC.Cloud;
    using MVCWIFHelpersSettings;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class SecurityTokenServiceConfigurationMVC : SecurityTokenServiceConfiguration
    {
        internal CompositionContainer CompositionContainer { get; private set; }
        static readonly object syncRoot = new object();
        const string CustomSecurityTokenServiceConfigurationKey = "CustomSecurityTokenServiceConfigurationKey";

        /// <summary>
        /// Provides a model for creating a single Configuration object for the application. The first call creates a new CustomSecruityTokenServiceConfiguration and 
        /// places it into the current HttpApplicationState using the key "CustomSecurityTokenServiceConfigurationKey". Subsequent calls will return the same
        /// Configuration object.  This maintains any state that is set between calls and improves performance.
        /// </summary>
        public static SecurityTokenServiceConfigurationMVC Current
        {
            get
            {
                HttpApplicationState httpAppState = HttpContext.Current.Application;

                var customConfiguration = httpAppState.Get(CustomSecurityTokenServiceConfigurationKey) as SecurityTokenServiceConfigurationMVC;

                if (customConfiguration == null)
                {
                    lock (syncRoot)
                    {
                        customConfiguration = httpAppState.Get(CustomSecurityTokenServiceConfigurationKey) as SecurityTokenServiceConfigurationMVC;

                        if (customConfiguration == null)
                        {
                            customConfiguration = new SecurityTokenServiceConfigurationMVC(Settings.Container);
                            httpAppState.Add(CustomSecurityTokenServiceConfigurationKey, customConfiguration);
                        }
                    }
                }

                return customConfiguration;
            }
        }

        /// <summary>
        /// CustomSecurityTokenServiceConfiguration constructor.
        /// </summary>
        public SecurityTokenServiceConfigurationMVC(CompositionContainer container)
            : base(container.GetExportedValue<string>(CompositionIdentifiers.PassiveSTSURL))
        {
            this.SecurityTokenService = typeof(CustomSecurityTokenService);

            this.CompositionContainer = container;            
            this.SigningCredentials = new X509SigningCredentials(STSCertificate);
        }

        internal X509Certificate2 STSCertificate
        {
            get
            {
                return X509Helper.GetX509Certificate2(StoreLocation.LocalMachine, StoreName.My, STSCertificateThumbprint, X509FindType.FindByThumbprint);
            }
        }

        internal string STSCertificateThumbprint
        {
            get
            {
                return this.CompositionContainer.GetExportedValue<string>(CompositionIdentifiers.STSCertificateThumbprint);
            }
        }

        internal string IssuerAddress
        {
            get
            {
                return this.CompositionContainer.GetExportedValue<string>(CompositionIdentifiers.PassiveSTSURL);
            }
        }
    }
}
