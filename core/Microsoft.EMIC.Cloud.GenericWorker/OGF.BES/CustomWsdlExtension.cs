//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Configuration;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;

namespace OGF.BES
{
    /// <summary>
    /// Extension class to owerwrite the default value of the PortName in wsdl file. WCF sets the name of the port if not specified explicitly.
    /// It is required for OGF BES interoperability
    /// </summary>
    public class PortNameWsdlBehavior : IWsdlExportExtension, IEndpointBehavior
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Writes custom Web Services Description Language (WSDL) elements into the generated WSDL for a contract.
        /// </summary>
        /// <param name="exporter">The <see cref="T:System.ServiceModel.Description.WsdlExporter"/> that exports the contract information.</param>
        /// <param name="context">Provides mappings from exported WSDL elements to the contract description.</param>
        public void ExportContract(WsdlExporter exporter, WsdlContractConversionContext context)
        {
        }

        /// <summary>
        /// Writes custom Web Services Description Language (WSDL) elements into the generated WSDL for an endpoint.
        /// </summary>
        /// <param name="exporter">The <see cref="T:System.ServiceModel.Description.WsdlExporter"/> that exports the endpoint information.</param>
        /// <param name="context">Provides mappings from exported WSDL elements to the endpoint description.</param>
        public void ExportEndpoint(WsdlExporter exporter, WsdlEndpointConversionContext context)
        {
            if (!string.IsNullOrEmpty(Name))
            {
                context.WsdlPort.Name = Name;
            }
        }

        /// <summary>
        /// Implement to pass data at runtime to bindings to support custom behavior.
        /// </summary>
        /// <param name="endpoint">The endpoint to modify.</param>
        /// <param name="bindingParameters">The objects that binding elements require to support the behavior.</param>
        public void AddBindingParameters(ServiceEndpoint endpoint, System.ServiceModel.Channels.BindingParameterCollection bindingParameters)
        {
        }

        /// <summary>
        /// Implements a modification or extension of the client across an endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint that is to be customized.</param>
        /// <param name="clientRuntime">The client runtime to be customized.</param>
        public void ApplyClientBehavior(ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.ClientRuntime clientRuntime)
        {
        }

        /// <summary>
        /// Implements a modification or extension of the service across an endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint that exposes the contract.</param>
        /// <param name="endpointDispatcher">The endpoint dispatcher to be modified or extended.</param>
        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.EndpointDispatcher endpointDispatcher)
        {
        }

        /// <summary>
        /// Implement to confirm that the endpoint meets some intended criteria.
        /// </summary>
        /// <param name="endpoint">The endpoint to validate.</param>
        public void Validate(ServiceEndpoint endpoint)
        {
        }
    }

    /// <summary>
    /// Enables to change the portname from the configuration file of the wcf service.
    /// </summary>
    public class PortNameWsdlBehaviorExtension : BehaviorExtensionElement
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        [ConfigurationProperty("name")]
        public string Name
        {
            get
            {
                object value = this["name"];
                return value != null ? value.ToString() : string.Empty;
            }
            set { this["name"] = value; }
        }

        /// <summary>
        /// Gets the type of behavior.
        /// </summary>
        /// <returns>A <see cref="T:System.Type"/>.</returns>
        public override Type BehaviorType
        {
            get { return typeof(PortNameWsdlBehavior); }
        }

        /// <summary>
        /// Creates a behavior extension based on the current configuration settings.
        /// </summary>
        /// <returns>
        /// The behavior extension.
        /// </returns>
        protected override object CreateBehavior()
        {
            return new PortNameWsdlBehavior { Name = Name };
        }
    }
}
