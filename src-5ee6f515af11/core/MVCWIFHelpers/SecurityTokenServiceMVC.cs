//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Web.Configuration;
using Microsoft.EMIC.Cloud.Security;
using Microsoft.EMIC.Cloud.GenericWorker.AzureSecurity;
using Microsoft.IdentityModel.Claims;
using Microsoft.IdentityModel.Configuration;
using Microsoft.IdentityModel.Protocols.WSTrust;
using Microsoft.IdentityModel.SecurityTokenService;
using System.ComponentModel.Composition.Hosting;
using Microsoft.EMIC.Cloud;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using MVCWIFHelpers;
using MVCWIFHelpersSettings;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class CustomSecurityTokenService : SecurityTokenService
    {
        // TODO: Set enableAppliesToValidation to true to enable only the RP Url's specified in the PassiveRedirectBasedClaimsAwareWebApps array to get a token from this STS
        static bool enableAppliesToValidation = false;

        // TODO: Add relying party Url's that will be allowed to get token from this STS
        static readonly string[] PassiveRedirectBasedClaimsAwareWebApps = {  };

        /// <summary>
        /// Creates an instance of CustomSecurityTokenService.
        /// </summary>
        /// <param name="configuration">The SecurityTokenServiceConfiguration.</param>
        public CustomSecurityTokenService(SecurityTokenServiceConfiguration configuration)
            : base(configuration)
        {
        }

        /// <summary>
        /// Validates appliesTo and throws an exception if the appliesTo is null or contains an unexpected address.
        /// </summary>
        /// <param name="appliesTo">The AppliesTo value that came in the RST.</param>
        /// <exception cref="ArgumentNullException">If 'appliesTo' parameter is null.</exception>
        /// <exception cref="InvalidRequestException">If 'appliesTo' is not valid.</exception>
        void ValidateAppliesTo(EndpointAddress appliesTo)
        {
            if (appliesTo == null)
            {
                throw new ArgumentNullException("appliesTo");
            }

            // TODO: Enable AppliesTo validation for allowed relying party Urls by setting enableAppliesToValidation to true. By default it is false.
            if (enableAppliesToValidation)
            {
                bool validAppliesTo = false;
                foreach (string rpUrl in PassiveRedirectBasedClaimsAwareWebApps)
                {
                    if (appliesTo.Uri.Equals(new Uri(rpUrl)))
                    {
                        validAppliesTo = true;
                        break;
                    }
                }

                if (!validAppliesTo)
                {
                    throw new InvalidRequestException(String.Format("The 'appliesTo' address '{0}' is not valid.", appliesTo.Uri.OriginalString));
                }
            }
        }

        /// <summary>
        /// This method returns the configuration for the token issuance request. The configuration
        /// is represented by the Scope class. In our case, we are only capable of issuing a token for a
        /// single RP identity represented by the EncryptingCertificateName.
        /// </summary>
        /// <param name="principal">The caller's principal.</param>
        /// <param name="request">The incoming RST.</param>
        /// <returns>The scope information to be used for the token issuance.</returns>
        protected override Scope GetScope(IClaimsPrincipal principal, RequestSecurityToken request)
        {
            ValidateAppliesTo(request.AppliesTo);

            //
            // Note: The signing certificate used by default has a Distinguished name of "CN=STSTestCert",
            // and is located in the Personal certificate store of the Local Computer. Before going into production,
            // ensure that you change this certificate to a valid CA-issued certificate as appropriate.
            //
            Scope scope = new Scope(request.AppliesTo.Uri.OriginalString, SecurityTokenServiceConfiguration.SigningCredentials);
            var cfg = this.SecurityTokenServiceConfiguration as SecurityTokenServiceConfigurationMVC;
            if (cfg == null)
            {
                throw new NotSupportedException(string.Format("The SecurityTokenServiceConfiguration was expected to be a {0}, but was {1}",
                    typeof(SecurityTokenServiceConfigurationMVC).Name, 
                    SecurityTokenServiceConfiguration == null ? "null" : this.SecurityTokenServiceConfiguration.GetType().Name));
            }

            scope.EncryptingCredentials = new X509EncryptingCredentials(cfg.STSCertificate); 

            // Set the ReplyTo address for the WS-Federation  passive protocol (wreply). This is the address to which responses will be directed. 
            // In this template, we have chosen to set this to the AppliesToAddress.
            //scope.ReplyToAddress = scope.AppliesToAddress
            scope.ReplyToAddress = request.ReplyTo;

            return scope;
        }


    internal UserTableTableDataContext CreateUserTableContext(CloudStorageAccount account, string userTableTableName)
    {
        return new UserTableTableDataContext(account.TableEndpoint.AbsoluteUri, account.Credentials,
                                             userTableTableName);
    }

    /// <summary>
    /// This method returns the claims to be issued in the token.
    /// </summary>
    /// <param name="principal">The caller's principal.</param>
    /// <param name="request">The incoming RST, can be used to obtain addtional information.</param>
    /// <param name="scope">The scope information corresponding to this request.</param> 
    /// <exception cref="ArgumentNullException">If 'principal' parameter is null.</exception>
    /// <returns>The outgoing claimsIdentity to be included in the issued token.</returns>
    protected override IClaimsIdentity GetOutputClaimsIdentity( IClaimsPrincipal principal, RequestSecurityToken request, Scope scope )
    {
        var CompositionContainer = Settings.Container;
        var userTableConnectionString = CompositionContainer.GetExportedValue<string>(CompositionIdentifiers.STSOnAzureConnectionString);
        var userTableTableName = CompositionContainer.GetExportedValue<string>(CompositionIdentifiers.DevelopmentSecurityTokenServiceUserTableName);
        var account = CloudStorageAccount.Parse(userTableConnectionString);
        var tc = account.CreateCloudTableClient();
        if (!tc.DoesTableExist(userTableTableName))
        {
            tc.CreateTableIfNotExist(userTableTableName);
        }

        var incomingIdentity = principal.Identities[1]; //principal.Identities[0]
        if (incomingIdentity == null)
        {
            throw new InvalidRequestException("The caller's principal has no ClaimsIdentity defined.");
        }

        // Issue custom claims.
        // TODO: Change the claims below to issue custom claims required by your application.
        // Update the application's configuration file too to reflect new claims requirement.
        var ctx = CreateUserTableContext(account,userTableTableName);
        var userRecord = ctx.GetUser(incomingIdentity.Name);
        if (userRecord == null)
        {
            throw new InvalidRequestException(string.Format("Cannot find user {0} in database", incomingIdentity.Name));
        }

        var outputIdentity = new ClaimsIdentity(userRecord.Claims);

        outputIdentity.Claims.Add(new Claim(ClaimTypes.Name, incomingIdentity.Name));

        // add authorization information to the existing authentication data
        if (userRecord.IsComputeAdministrator) outputIdentity.Claims.Add(VenusClaimsAuthorizationManager.Roles.ComputeAdministrator);
        if (userRecord.IsResearcher) outputIdentity.Claims.Add(VenusClaimsAuthorizationManager.Roles.Researcher);

        return outputIdentity;
    }
}

