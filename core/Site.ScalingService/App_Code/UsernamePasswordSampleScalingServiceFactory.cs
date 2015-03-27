//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Site.ScalingService
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
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;
    using Microsoft.EMIC.Cloud.GenericWorker.AzureSecurity;
    using Microsoft.EMIC.Cloud.AzureSettings;

    public class UsernamePasswordSampleScalingServiceFactory : VenusServiceFactoryBase<ScalingServiceImpl>
    {
        public UsernamePasswordSampleScalingServiceFactory() { }

        public override Type ServiceType()
        {
            return ScalingServiceConfig.ServiceType();
        }

        public override Type ServiceInterfaceType()
        {
            return ScalingServiceConfig.ServiceInterfaceType();
        }

        public override CompositionContainer CreateCompositionContainer()
        {
            return ScalingServiceConfig.CreateCompositionContainer();
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

            var wcfSharedMachineKey = container.GetExportedValue<string>(CompositionIdentifiers.WCFSharedMachineKey);
            if (string.IsNullOrEmpty(wcfSharedMachineKey))
            {
                throw new Exception(ExceptionMessages.WCFSharedMachineKey);
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

            var wifServiceConfiguration = new Microsoft.IdentityModel.Configuration.ServiceConfiguration(loadConfig: false)
            {
                ClaimsAuthorizationManager = claimsAuthorizationManager,
                ServiceCertificate = serviceCert
            };

            #region Replace WindowsUserNameSecurityTokenHandler with DemoUserNamePasswordJobSubmissionSecurityTokenHandler

            var oldUPTokenHandler = wifServiceConfiguration.SecurityTokenHandlers.Where(sth => sth is Microsoft.IdentityModel.Tokens.WindowsUserNameSecurityTokenHandler).FirstOrDefault();
            if (oldUPTokenHandler != null)
            {
                wifServiceConfiguration.SecurityTokenHandlers.Remove(oldUPTokenHandler);
            }
            wifServiceConfiguration.SecurityTokenHandlers.Add(new DemoUserNamePasswordScalingServiceSecurityTokenHandler());

            #endregion

            FederatedServiceCredentials.ConfigureServiceHost(serviceHost, wifServiceConfiguration);
        }
    }

    public class DemoUserNamePasswordScalingServiceSecurityTokenHandler : Microsoft.IdentityModel.Tokens.UserNameSecurityTokenHandler
    {
        public DemoUserNamePasswordScalingServiceSecurityTokenHandler() { }

        public override Type TokenType { get { return typeof(UserNameSecurityToken); } }

        public override bool CanValidateToken { get { return true; } }
        
        internal UserTableTableDataContext CreateUserTableContext()
        {
            var container = new CompositionContainer(new AggregateCatalog(
                new AssemblyCatalog(typeof(CompositionIdentifiers).Assembly),
                new AssemblyCatalog(typeof(AzureSettingsProvider).Assembly)));

            var userTableConnectionString = container.GetExportedValue<string>(CompositionIdentifiers.STSOnAzureConnectionString);
            var userTableTableName = container.GetExportedValue<string>(CompositionIdentifiers.DevelopmentSecurityTokenServiceUserTableName);

            var account = CloudStorageAccount.Parse(userTableConnectionString);
            var tc = account.CreateCloudTableClient();
            if (!tc.DoesTableExist(userTableTableName))
            {
                tc.CreateTableIfNotExist(userTableTableName);
            }

            return new UserTableTableDataContext(account.TableEndpoint.AbsoluteUri, account.Credentials, userTableTableName);
        }

        public override ClaimsIdentityCollection ValidateToken(SecurityToken token)
        {
            var up = token as UserNameSecurityToken;
            if (up == null)
            {
                throw new ArgumentException("token", string.Format(ExceptionMessages.MustBe, typeof(UserNameSecurityToken).Name));
            }

            var ctx = CreateUserTableContext();
            var user = ctx.GetUser(up.UserName);
            if (user == null || user.Password != up.Password)
            {
                throw new SecurityTokenValidationException(ExceptionMessages.InvalidUsernamePassword);
            }

            var claimsId = new ClaimsIdentity();

            claimsId.Claims.Add(new Claim(ClaimTypes.Name, up.UserName));
            claimsId.Claims.Add(new Claim(ClaimTypes.AuthenticationMethod, Microsoft.IdentityModel.Claims.AuthenticationMethods.Password));
            claimsId.Claims.Add(VenusClaimsAuthorizationManager.Roles.ComputeAdministrator);         

            return new ClaimsIdentityCollection(new IClaimsIdentity[] { claimsId });
        }
    }
}