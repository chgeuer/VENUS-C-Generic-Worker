//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.ObjectModel;

namespace Microsoft.EMIC.Cloud.Security
{
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;
    using Microsoft.IdentityModel.Tokens;
    using System.Diagnostics;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class RsaSessionServiceBehavior : IServiceBehavior
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RsaSessionServiceBehavior"/> class.
        /// </summary>
        public RsaSessionServiceBehavior()
        {
            Trace.TraceInformation("Created RsaSessionServiceBehavior()");
        }

        /// <summary>
        /// Provides the ability to pass custom data to binding elements to support the contract implementation.
        /// </summary>
        /// <param name="serviceDescription">The service description of the service.</param>
        /// <param name="serviceHostBase">The host of the service.</param>
        /// <param name="endpoints">The service endpoints.</param>
        /// <param name="bindingParameters">Custom objects to which binding elements have access.</param>
        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, 
            Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters) { }

        /// <summary>
        /// Provides the ability to change run-time property values or insert custom extension objects such as error handlers, message or parameter interceptors, security extensions, and other custom extension objects.
        /// </summary>
        /// <param name="serviceDescription">The service description.</param>
        /// <param name="serviceHostBase">The host that is currently being built.</param>
        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase) { }

        /// <summary>
        /// Provides the ability to inspect the service host and the service description to confirm that the service can run successfully.
        /// </summary>
        /// <param name="serviceDescription">The service description.</param>
        /// <param name="serviceHostBase">The service host that is currently being constructed.</param>
        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            var credentials = serviceHostBase.Description.Behaviors.Find<FederatedServiceCredentials>();
            if (credentials == null)
            {
                throw new NotSupportedException(ExceptionMessages.FederatedServiceCredentials);
            }
            if (serviceHostBase == null ||
                serviceHostBase.Credentials == null ||
                serviceHostBase.Credentials.ServiceCertificate == null ||
                serviceHostBase.Credentials.ServiceCertificate.Certificate == null)
            {
                throw new NotSupportedException(ExceptionMessages.ServiceCertificate);
            }

            var ssth = new RsaSessionSecurityTokenHandler(serviceHostBase.Credentials.ServiceCertificate.Certificate);
            credentials.SecurityTokenHandlers.AddOrReplace(ssth);
        }
    }
}