//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Site.JobListingService
{

    using System;
    using System.ComponentModel.Composition.Hosting;
    using System.IdentityModel.Tokens;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using Microsoft.EMIC.Cloud;
    using Microsoft.EMIC.Cloud.GenericWorker;
    using Microsoft.EMIC.Cloud.Security;
    using Microsoft.EMIC.Cloud.Security.AuthorizationPolicy;
    using Microsoft.EMIC.Cloud.Utilities;
    using Microsoft.IdentityModel.Claims;
    using Microsoft.IdentityModel.Configuration;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.EMIC.Cloud.GenericWorker.AzureProvider;

    public class UsernamePasswordSampleJobListingServiceFactory : VenusServiceFactoryBase<JobListingService>
    {
        public UsernamePasswordSampleJobListingServiceFactory() { }

        public override Type ServiceType()
        {
            return JobListingServiceConfig.ServiceType();
        }

        public override Type ServiceInterfaceType()
        {
            return JobListingServiceConfig.ServiceInterfaceType();
        }

        public override CompositionContainer CreateCompositionContainer()
        {
            return JobListingServiceConfig.CreateCompositionContainer();
        }

        public override Binding CreateBinding()
        {
            return WCFUtils.CreateUsernamePasswordSecurityTokenServiceBinding();
        }

        public override void SecureServiceHost(ServiceHost serviceHost, Uri[] baseAddresses)
        {
            // TODO: Use the appropriate service cert
            var serviceCertificateThumbprint = container.GetExportedValue<string>(CompositionIdentifiers.STSCertificateThumbprint);
            serviceHost.Credentials.ServiceCertificate.SetCertificate(StoreLocation.LocalMachine, StoreName.My, X509FindType.FindByThumbprint, serviceCertificateThumbprint);

            #region Add SessionServiceBehavior

            // [System.Convert]::ToBase64String([System.Security.Cryptography.AesManaged]::Create().Key)
            var wcfSharedMachineKey = container.GetExportedValue<string>(CompositionIdentifiers.WCFSharedMachineKey);
            if (string.IsNullOrEmpty(wcfSharedMachineKey))
            {
                throw new Exception("Cannot determine WCFSharedMachineKey");
            }
            var key = Convert.FromBase64String(wcfSharedMachineKey);

            WCFUtils.AddSessionServiceBehavior(serviceHost, key);

            #endregion

            #region Create ClaimsAuthorizationManager

            var serializedGlobalSecurityPolicy = container.GetExportedValue<string>(CompositionIdentifiers.SerializedGlobalSecurityPolicy);
            var globalSecurityPolicy = new ClaimRequirementPolicy(serializedGlobalSecurityPolicy);
            var claimsAuthorizationManager = new VenusClaimsAuthorizationManager(globalSecurityPolicy);

            #endregion

            #region Create serviceCertificate

            var serviceCert = X509Helper.GetX509Certificate2(StoreLocation.LocalMachine, StoreName.My, serviceCertificateThumbprint, X509FindType.FindByThumbprint);

            #endregion

            var wifServiceConfiguration = new ServiceConfiguration(loadConfig: false)
            {
                ClaimsAuthorizationManager = claimsAuthorizationManager,
                ServiceCertificate = serviceCert
            };

            #region Replace WindowsUserNameSecurityTokenHandler with DemoUserNamePasswordJobSubmissionSecurityTokenHandler

            var oldUPTokenHandler = wifServiceConfiguration.SecurityTokenHandlers.Where(sth => sth is WindowsUserNameSecurityTokenHandler).FirstOrDefault();
            if (oldUPTokenHandler != null)
            {
                wifServiceConfiguration.SecurityTokenHandlers.Remove(oldUPTokenHandler);
            }
            wifServiceConfiguration.SecurityTokenHandlers.Add(new DemoUserNamePasswordJobListingServiceSecurityTokenHandler());

            #endregion

            FederatedServiceCredentials.ConfigureServiceHost(serviceHost, wifServiceConfiguration);
        }
    }

    public class DemoUserNamePasswordJobListingServiceSecurityTokenHandler : UserNameSecurityTokenHandler
    {
        public DemoUserNamePasswordJobListingServiceSecurityTokenHandler() { }

        public override Type TokenType { get { return typeof(UserNameSecurityToken); } }

        public override bool CanValidateToken { get { return true; } }

        public override ClaimsIdentityCollection ValidateToken(SecurityToken token)
        {
            var up = token as UserNameSecurityToken;
            if (up == null)
            {
                throw new ArgumentException("token", string.Format("Must be a {0}", typeof(UserNameSecurityToken).Name));
            }

            if (up.UserName != "Hakan" || up.Password != "test123!")
            {
                throw new SecurityTokenValidationException("Invalid username or password");
            }

            var claimsId = new ClaimsIdentity();

            claimsId.Claims.Add(new Claim(ClaimTypes.Name, up.UserName));
            claimsId.Claims.Add(new Claim(ClaimTypes.AuthenticationMethod, AuthenticationMethods.Password));
            claimsId.Claims.Add(VenusClaimsAuthorizationManager.Roles.ComputeAdministrator);         

            return new ClaimsIdentityCollection(new IClaimsIdentity[] { claimsId });
        }
    }
}