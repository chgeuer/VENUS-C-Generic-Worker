//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Microsoft.EMIC.Cloud.STS
{
    using System;
    using System.Diagnostics;
    using Microsoft.EMIC.Cloud.Security;
    using Microsoft.IdentityModel.Claims;
    using Microsoft.IdentityModel.Protocols.WSTrust;
    using Microsoft.IdentityModel.SecurityTokenService;
    
    public class SampleVENUSSecurityTokenService : SecurityTokenService
    {
        public SampleVENUSSecurityTokenService(SampleVENUSSecurityTokenServiceConfiguration config) : base(config) { }

        protected override IClaimsIdentity GetOutputClaimsIdentity(IClaimsPrincipal principal, RequestSecurityToken request, Scope scope)
        {
            var cfg = base.SecurityTokenServiceConfiguration as SampleVENUSSecurityTokenServiceConfiguration;
            if (cfg == null)
            {
                throw new InvalidRequestException(string.Format(ExceptionMessages.SecurityTokenServiceConfiguration,
                    typeof(SampleVENUSSecurityTokenServiceConfiguration).Name));
            }

            if (principal == null)
            {
                throw new InvalidRequestException(ExceptionMessages.CallerPrincipalNull);
            }
            var incomingIdentity = principal.Identities[0];
            if (incomingIdentity == null)
            {
                throw new InvalidRequestException(ExceptionMessages.NoClaimsIdentityDefined);
            }

            var ctx = cfg.CreateUserTableContext();
            var userRecord = ctx.GetUser(incomingIdentity.Name);
            if (userRecord == null)
            {
                throw new InvalidRequestException(string.Format(ExceptionMessages.CanNotFindUser, incomingIdentity.Name));
            }

            var outputIdentity = new ClaimsIdentity(userRecord.Claims);

            outputIdentity.Claims.Add(new Claim(ClaimTypes.Name, incomingIdentity.Name));

            // add authorization information to the existing authentication data
            if (userRecord.IsComputeAdministrator) outputIdentity.Claims.Add(VenusClaimsAuthorizationManager.Roles.ComputeAdministrator);
            if (userRecord.IsResearcher) outputIdentity.Claims.Add(VenusClaimsAuthorizationManager.Roles.Researcher);

            return outputIdentity;
        }

        protected override Scope GetScope(IClaimsPrincipal principal, RequestSecurityToken request)
        {
            Trace.TraceInformation(string.Format("Issuing a token for service {0}", request.AppliesTo.Uri.AbsoluteUri));

            var scope = new Scope(
                appliesToAddress: request.AppliesTo.Uri.AbsoluteUri,
                signingCredentials: this.SecurityTokenServiceConfiguration.SigningCredentials);

            var cfg = this.SecurityTokenServiceConfiguration as SampleVENUSSecurityTokenServiceConfiguration;
            if (cfg == null)
            {
                throw new NotSupportedException(string.Format(ExceptionMessages.SecurityTokenServiceConfigurationWrong,
                    typeof(SampleVENUSSecurityTokenServiceConfiguration).Name, 
                    SecurityTokenServiceConfiguration == null ? "null" : this.SecurityTokenServiceConfiguration.GetType().Name));
            }

            // $HACK Here we're assuming that the service has the same cert as the STS
            var serviceCert = cfg.STSCertificate;
            scope.EncryptingCredentials = new X509EncryptingCredentials(serviceCert);

            return scope;
        }

        protected override string GetIssuerName()
        {
            var cfg = base.SecurityTokenServiceConfiguration as SampleVENUSSecurityTokenServiceConfiguration;
            if (cfg == null)
            {
                throw new InvalidRequestException(string.Format(ExceptionMessages.SecurityTokenServiceConfiguration,
                    typeof(SampleVENUSSecurityTokenServiceConfiguration).Name));
            }
          
            return cfg.IssuerAddress;
        }
    }
}