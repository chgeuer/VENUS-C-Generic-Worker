//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.EMIC.Cloud.Utilities
{
    using System;
    using System.ComponentModel.Composition.Hosting;
    using System.IdentityModel.Selectors;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;
    using System.ServiceModel.Security.Tokens;
    using Microsoft.EMIC.Cloud.Security;
    using Microsoft.EMIC.Cloud.Security.AuthorizationPolicy;
    using Microsoft.IdentityModel.Configuration;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.IdentityModel.Tokens.Saml11;
    using System.IdentityModel.Tokens;
    using System.ServiceModel.Security;

    /// <summary>
    /// Utility methods for Windows Communication Foundation (WCF) and Windows Identity Foundation (WIF). 
    /// </summary>
    public static class WCFUtils
    {
        /// <summary>
        /// Gets a value indicating whether reliable session is enabled.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if reliable session is enabled; otherwise, <c>false</c>.
        /// </value>
        public static bool ReliableSessionEnabled { get { return false; } }
        /// <summary>
        /// Gets a value indicating whether security token service establishes security context.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if security token service establish security context; otherwise, <c>false</c>.
        /// </value>
        public static bool SecurityTokenServiceEstablishSecurityContext { get { return true; } }
        /// <summary>
        /// Gets a value indicating whether security token service negotiates service credential.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if security token service negotiates service credential; otherwise, <c>false</c>.
        /// </value>
        public static bool SecurityTokenServiceNegotiateServiceCredential { get { return false; } }
        /// <summary>
        /// Gets a value indicating whether service establishes security context.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if service establishes security context; otherwise, <c>false</c>.
        /// </value>
        public static bool ServiceEstablishSecurityContext { get { return true; } }
        /// <summary>
        /// Gets a value indicating whether service negotiates service credential.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if service negotiates service credential; otherwise, <c>false</c>.
        /// </value>
        public static bool ServiceNegotiateServiceCredential { get { return false; } }

        /// <summary>
        /// The issuer name of the co-located STS. 
        /// </summary>
        public static string StsIssuerNameOfTheCoLocatedSts { get { return "STS"; } }

        /// <summary>
        /// Disables the requirement of security context cancellation.
        /// </summary>
        /// <param name="bindingElements">The binding elements.</param>
        public static void DisableRequireSecurityContextCancellation (this BindingElementCollection bindingElements)
        {
            var securityBindingElement = bindingElements.OfType<SymmetricSecurityBindingElement>().FirstOrDefault();
            if (securityBindingElement == null)
            {
                throw new NotSupportedException(ExceptionMessages.SymmetricSecurityBindingElement);
            }
            var protectionTokenParameters = securityBindingElement.ProtectionTokenParameters;
            if (protectionTokenParameters == null)
            {
                throw new NotSupportedException(ExceptionMessages.ProtectionTokenParameters);
            }
            var secureConversationSecurityTokenParameters = protectionTokenParameters as SecureConversationSecurityTokenParameters;
            if (secureConversationSecurityTokenParameters == null)
            {
                throw new NotSupportedException(ExceptionMessages.SecureConversationSecurityTokenParameters);
            }
            // this should be equivalent to setting requireSecurityContextCancellation to false. 
            secureConversationSecurityTokenParameters.RequireCancellation = false;
        }

        /// <summary>
        /// Creates the username password security token service binding.
        /// </summary>
        /// <returns></returns>
        public static Binding CreateUsernamePasswordSecurityTokenServiceBinding()
        {
            var issuerBinding = new WS2007HttpBinding(SecurityMode.Message, ReliableSessionEnabled);
                
           
            issuerBinding.Security.Message.NegotiateServiceCredential = WCFUtils.SecurityTokenServiceNegotiateServiceCredential;
            issuerBinding.Security.Message.EstablishSecurityContext = WCFUtils.SecurityTokenServiceEstablishSecurityContext;
            issuerBinding.Security.Message.ClientCredentialType =  MessageCredentialType.UserName;

            var bindingElements = issuerBinding.CreateBindingElements();
            bindingElements.DisableRequireSecurityContextCancellation();
            return new CustomBinding(bindingElements);
        }

        /// <summary>
        /// Creates the secure client binding using username/password.
        /// </summary>
        /// <param name="issuerAddress">The issuer address.</param>
        /// <param name="httpKeepAliveEnabled">if set to <c>false</c> the HTTP channel is not re-used. Useful for load-balancer testing purposes.</param>
        /// <returns></returns>
        public static Binding CreateSecureUsernamePasswordClientBinding(EndpointAddress issuerAddress, bool httpKeepAliveEnabled = true)
        {
            var serviceBinding = new WS2007FederationHttpBinding(WSFederationHttpSecurityMode.Message, ReliableSessionEnabled);

            serviceBinding.Security.Message.EstablishSecurityContext = ServiceEstablishSecurityContext;
            serviceBinding.Security.Message.NegotiateServiceCredential = ServiceNegotiateServiceCredential;
            serviceBinding.Security.Message.IssuerAddress = issuerAddress;
            serviceBinding.Security.Message.IssuerBinding = CreateUsernamePasswordSecurityTokenServiceBinding();
          
            var bindingElements = serviceBinding.CreateBindingElements();
            bindingElements.DisableRequireSecurityContextCancellation();

            if (!httpKeepAliveEnabled)
            {
                var httpBe = bindingElements.OfType<HttpTransportBindingElement>().FirstOrDefault();
                if (httpBe != null)
                {
                    httpBe.KeepAliveEnabled = false;
                }
            }

            return new CustomBinding(bindingElements);
        }

        /// <summary>
        /// Creates a new instance of WS2007FederationHttpBinding class
        /// </summary>
        /// <returns></returns>
        public static Binding CreateSecureSamlBinding()
        {
            var serviceBinding = new WS2007FederationHttpBinding(WSFederationHttpSecurityMode.Message);

            serviceBinding.Security.Message.EstablishSecurityContext = ServiceEstablishSecurityContext;
            serviceBinding.Security.Message.NegotiateServiceCredential = ServiceNegotiateServiceCredential;
            serviceBinding.Security.Message.IssuedKeyType = SecurityKeyType.SymmetricKey;
            serviceBinding.Security.Message.IssuedTokenType = System.IdentityModel.Tokens.SecurityTokenTypes.Saml;  

            return serviceBinding;
        }

        /// <summary>
        /// Creates the secure service binding for the CompositionContainer.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns></returns>
        public static Binding CreateSecureServiceBinding(this CompositionContainer container)
        {
            var binding = new WS2007FederationHttpBinding(WSFederationHttpSecurityMode.Message, ReliableSessionEnabled);

            binding.Security.Message.EstablishSecurityContext = ServiceEstablishSecurityContext;
            binding.Security.Message.NegotiateServiceCredential = ServiceNegotiateServiceCredential;

            binding.Security.Message.IssuerMetadataAddress = new EndpointAddress(container.GetSecurityTokenServiceMetadataAddress());

            var bindingElements = binding.CreateBindingElements();
            bindingElements.DisableRequireSecurityContextCancellation();
            return new CustomBinding(bindingElements);
        }

        /// <summary>
        /// Adds a new symmetric key session service behavior with the specified key to the servicehost.
        /// </summary>
        /// <param name="serviceHost">The service host.</param>
        /// <param name="key">The key.</param>
        public static void AddSessionServiceBehavior(ServiceHost serviceHost, byte[] key)
        {
            var sessionServiceBehavior = serviceHost.Description.Behaviors.Find<SymmetricKeySessionServiceBehavior>();
            if (sessionServiceBehavior == null)
            {
                sessionServiceBehavior = new SymmetricKeySessionServiceBehavior(key);
                serviceHost.Description.Behaviors.Add(sessionServiceBehavior);
            }
        }

        /// <summary>
        /// Configures the service host according to the specified CompositionContainer input.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="serviceHost">The service host.</param>
        /// <param name="baseAddresses">The base addresses.</param>
        public static void SecureServiceHost(this CompositionContainer container, ServiceHost serviceHost, Uri[] baseAddresses)
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

            #region Create IssuerNameRegistry

            var issuerNameRegistry = new ConfigurationBasedCertStoreIssuerNameRegistry();
            var stsCertificateThumbprint = container.GetExportedValue<string>(CompositionIdentifiers.STSCertificateThumbprint);
            issuerNameRegistry.AddTrustedIssuer(stsCertificateThumbprint, StsIssuerNameOfTheCoLocatedSts);

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
                                                  IssuerNameRegistry = issuerNameRegistry,
                                                  ClaimsAuthorizationManager = claimsAuthorizationManager,
                                                  ServiceCertificate = serviceCert
                                              };

            #region Set SAML AudienceRestriction

            if (container.GetExportedValue<bool>(CompositionIdentifiers.IsGWDeployedOnAzureWebRole))
            {
                wifServiceConfiguration.AudienceRestriction.AudienceMode = AudienceUriMode.Always;

                var audiences = new List<string>()
                                {
                                    container.GetSecureJobSubmissionAddress(),
                                    container.GetSecureScalingServiceAddress(),
                                    container.GetSecureNotificationServiceAddress(),
                                };

                audiences.ForEach(a => wifServiceConfiguration.AudienceRestriction.AllowedAudienceUris.Add(new Uri(a)));
            }
            else
            {
                wifServiceConfiguration.AudienceRestriction.AudienceMode = AudienceUriMode.Never;
            }

            #endregion

            #region Configure WIF so that principal.Name and principal.IsInRole() work as expected

            var samlTokenHandler = wifServiceConfiguration.SecurityTokenHandlers.OfType<Saml11SecurityTokenHandler>().FirstOrDefault();
            if (samlTokenHandler != null)
            {
                samlTokenHandler.SamlSecurityTokenRequirement.NameClaimType = Microsoft.IdentityModel.Claims.ClaimTypes.Name;
                samlTokenHandler.SamlSecurityTokenRequirement.RoleClaimType = Microsoft.IdentityModel.Claims.ClaimTypes.Role;
            }

            #endregion

            FederatedServiceCredentials.ConfigureServiceHost(serviceHost, wifServiceConfiguration);
        }

        /// <summary>
        /// Creates an unprotected binding with a new instance of WS2007HttpBinding class.
        /// </summary>
        /// <returns></returns>
        public static Binding CreateUnprotectedBinding()
        {
            return new WS2007HttpBinding(SecurityMode.None, ReliableSessionEnabled);
        }

        /// <summary>
        /// Enables the windows identity foundation for unprotected service host.
        /// </summary>
        /// <param name="serviceHost">The service host.</param>
        public static void EnableWindowsIdentityFoundationForUnprotectedServiceHost(this ServiceHost serviceHost)
        {
            FederatedServiceCredentials.ConfigureServiceHost(serviceHost);
        }

        /// <summary>
        /// Factory for a MEX <see cref="Binding"/> according to the <see cref="Uri.Scheme"/>.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static Binding GetMetadataExchangeBinding(Uri uri)
        {
            if (uri.Scheme.Equals("http")) return MetadataExchangeBindings.CreateMexHttpBinding();
            if (uri.Scheme.Equals("https")) return MetadataExchangeBindings.CreateMexHttpsBinding();
            if (uri.Scheme.Equals("net.pipe")) return MetadataExchangeBindings.CreateMexNamedPipeBinding();
            if (uri.Scheme.Equals("net.tcp")) return MetadataExchangeBindings.CreateMexTcpBinding();

            throw new NotSupportedException(string.Format(ExceptionMessages.CanNotCreateMEXBinding, uri.Scheme));
        }
    }
}