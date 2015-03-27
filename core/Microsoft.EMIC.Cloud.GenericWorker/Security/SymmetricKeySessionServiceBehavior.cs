//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Diagnostics;

namespace Microsoft.EMIC.Cloud.Security
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.IdentityModel.Web;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class SymmetricKeySessionServiceBehavior : IServiceBehavior
    {
        private readonly byte[] _key;
        
        private SymmetricKeySessionServiceBehavior() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SymmetricKeySessionServiceBehavior"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        public SymmetricKeySessionServiceBehavior(byte[] key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            this._key = key;
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

            var ssth = new SymmetricKeySessionSecurityTokenHandler(this._key);
            credentials.SecurityTokenHandlers.AddOrReplace(ssth);
        }
    }
}