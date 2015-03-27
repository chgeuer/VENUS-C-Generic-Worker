//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Microsoft.EMIC.Cloud.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics;
    using System.ServiceModel;
    using System.ServiceModel.Activation;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;

    /// <summary>
    /// This class provides functionality to create secured web service endpoints.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SecureVenusServiceFactoryBase<T> : VenusServiceFactoryBase<T>
    {
        /// <summary>
        /// Creates the binding.
        /// </summary>
        /// <returns></returns>
        public override Binding CreateBinding()
        {
            return container.CreateSecureServiceBinding();
        }

        /// <summary>
        /// Secures the service host.
        /// </summary>
        /// <param name="serviceHost">The service host.</param>
        /// <param name="baseAddresses">The base addresses.</param>
        public override void SecureServiceHost(ServiceHost serviceHost, Uri[] baseAddresses)
        {
            container.SecureServiceHost(serviceHost, baseAddresses);
        }
    }

    /// <summary>
    /// This class provides functionality to create unsecured web service endpoints.
    /// </summary>
    public abstract class UnprotectedVenusServiceFactoryBase<T> : VenusServiceFactoryBase<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnprotectedVenusServiceFactoryBase&lt;T&gt;"/> class.
        /// </summary>
        protected UnprotectedVenusServiceFactoryBase()
        {
            if (!container.GetExportedValue<bool>(CompositionIdentifiers.SecurityAllowInsecureAccess))
            {
                throw new NotSupportedException(
                    string.Format(
                        ExceptionMessages.UnprotectedEndpointNotExposed,
                        CompositionIdentifiers.SecurityAllowInsecureAccess));
            }
        }

        /// <summary>
        /// Creates the binding.
        /// </summary>
        /// <returns></returns>
        public override Binding CreateBinding()
        {
            return WCFUtils.CreateUnprotectedBinding();
        }

        /// <summary>
        /// Secures the service host.
        /// </summary>
        /// <param name="serviceHost">The service host.</param>
        /// <param name="baseAddresses">The base addresses.</param>
        public override void SecureServiceHost(ServiceHost serviceHost, Uri[] baseAddresses)
        {
            serviceHost.EnableWindowsIdentityFoundationForUnprotectedServiceHost();
        }
    }

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public abstract class VenusServiceFactoryBase<T> : ServiceHostFactory
    {
        /// <summary>
        /// Gets the type of the service.
        /// </summary>
        /// <returns></returns>
        public abstract Type ServiceType();
        /// <summary>
        /// Gets the type of the service interface.
        /// </summary>
        /// <returns></returns>
        public abstract Type ServiceInterfaceType();
        /// <summary>
        /// Creates a new instance of a composition container.
        /// </summary>
        /// <returns></returns>
        public abstract CompositionContainer CreateCompositionContainer();
        /// <summary>
        /// Creates the binding.
        /// </summary>
        /// <returns></returns>
        public abstract Binding CreateBinding();
        /// <summary>
        /// Secures the service host.
        /// </summary>
        /// <param name="serviceHost">The service host.</param>
        /// <param name="baseAddresses">The base addresses.</param>
        public abstract void SecureServiceHost(ServiceHost serviceHost, Uri[] baseAddresses);
        /// <summary>
        /// The instance of composition container if created
        /// </summary>
        protected CompositionContainer container;
        /// <summary>
        /// The name of the port
        /// </summary>
        protected string portName;


        /// <summary>
        /// Initializes a new instance of the <see cref="VenusServiceFactoryBase&lt;T&gt;"/> class.
        /// </summary>
        protected VenusServiceFactoryBase()
        {
            container = CreateCompositionContainer();
        }

        /// <summary>
        /// Creates a <see cref="T:System.ServiceModel.ServiceHost"/> for a specified type of service with a specific base address.
        /// </summary>
        /// <param name="serviceType">Specifies the type of service to host.</param>
        /// <param name="baseAddresses">The <see cref="T:System.Array"/> of type <see cref="T:System.Uri"/> that contains the base addresses for the service hosted.</param>
        /// <returns>
        /// A <see cref="T:System.ServiceModel.ServiceHost"/> for the type of service specified with a specific base address.
        /// </returns>
        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            if (serviceType != ServiceType())
            {
                throw new ArgumentException(string.Format(ExceptionMessages.IllegalServiceType,
                    ServiceType()), "serviceType");
            }

            var serviceBinding = CreateBinding();

            var serviceHost = new ServiceHost(ServiceType(), baseAddresses);
            serviceHost.AddServiceEndpoint(ServiceInterfaceType(), serviceBinding, "");

           
            #region ServiceMetadataBehavior

            var smb = serviceHost.Description.Behaviors.Find<ServiceMetadataBehavior>();
            if (smb == null)
            {
                smb = new ServiceMetadataBehavior();
                serviceHost.Description.Behaviors.Add(smb);
            }
            if (baseAddresses.Any(uri => uri.Scheme.Equals("http"))) smb.HttpGetEnabled = true;
            if (baseAddresses.Any(uri => uri.Scheme.Equals("https"))) smb.HttpsGetEnabled = true;

            serviceHost.AddServiceEndpoint(typeof(IMetadataExchange), MetadataExchangeBindings.CreateMexHttpBinding(), "mex");

            #endregion

            #region ServiceDebugBehavior

            var sdb = serviceHost.Description.Behaviors.Find<ServiceDebugBehavior>();
            if (sdb == null)
            {
                sdb = new ServiceDebugBehavior();
                serviceHost.Description.Behaviors.Add(sdb);
            }
            sdb.IncludeExceptionDetailInFaults = true;

            #endregion

            #region UseRequestHeadersForMetadataAddressBehavior

            var urhfmab = new UseRequestHeadersForMetadataAddressBehavior();
            urhfmab.DefaultPortsByScheme.Add(new KeyValuePair<string, int>("http", 80));
            serviceHost.Description.Behaviors.Add(urhfmab);

            #endregion

            serviceHost.Description.Behaviors.Add(new MyServiceBehavior<T>(container));

            if (!String.IsNullOrEmpty(container.GetExportedValueOrDefault<string>(CompositionIdentifiers.OGFWsdlPortName)))
            { 
                OGF.BES.PortNameWsdlBehavior extension = new OGF.BES.PortNameWsdlBehavior();
                extension.Name = "BESFactoryPortTypePort";

                foreach (ServiceEndpoint se in serviceHost.Description.Endpoints)
                    se.Behaviors.Add(extension);
            }


            this.SecureServiceHost(serviceHost, baseAddresses);

            // Just try to check whether the actual service instance can be created. 
            var svc = container.GetExportedValue<T>();
            Debug.Assert(
                svc != null,
                "container.GetExportedValue<T>() != null");

            return serviceHost;
        }
    }
}
