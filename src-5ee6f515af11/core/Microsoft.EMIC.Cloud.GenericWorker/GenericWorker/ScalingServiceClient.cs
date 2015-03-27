//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Microsoft.EMIC.Cloud.Utilities;
using System.Security.Cryptography.X509Certificates;
using Microsoft.EMIC.Cloud.GenericWorker;
using System.IdentityModel.Claims;
using Microsoft.EMIC.Cloud.Security.Saml;
using System.ServiceModel.Description;

namespace Microsoft.EMIC.Cloud.GenericWorker
{
    using System.Runtime.Serialization;


    /// <summary>
    /// Represents the Cloud Provider ID and the number of available GW instances that are deployed on the cloud specified by the cloud ID.
    /// </summary>
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "3.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name = "DeploymentSize", Namespace = "http://schemas.datacontract.org/2004/07/Microsoft.EMIC.Cloud.GenericWorker")]
    public partial class DeploymentSize : object, System.Runtime.Serialization.IExtensibleDataObject
    {

        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;

        private string CloudProviderIDField;

        private int InstanceCountField;

        /// <summary>
        /// Gets or sets the structure that contains extra data.
        /// </summary>
        /// <returns>An <see cref="T:System.Runtime.Serialization.ExtensionDataObject"/> that contains data that is not recognized as belonging to the data contract.</returns>
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData
        {
            get
            {
                return this.extensionDataField;
            }
            set
            {
                this.extensionDataField = value;
            }
        }

        /// <summary>
        /// Gets or sets the cloud provider ID.
        /// </summary>
        /// <value>
        /// The cloud provider ID.
        /// </value>
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string CloudProviderID
        {
            get
            {
                return this.CloudProviderIDField;
            }
            set
            {
                this.CloudProviderIDField = value;
            }
        }

        /// <summary>
        /// Gets or sets the instance count.
        /// </summary>
        /// <value>
        /// The instance count.
        /// </value>
        [System.Runtime.Serialization.DataMemberAttribute()]
        public int InstanceCount
        {
            get
            {
                return this.InstanceCountField;
            }
            set
            {
                this.InstanceCountField = value;
            }
        }
    }
}


/// <summary>
/// The client class for accessing the scaling service.
/// </summary>
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
public partial class ScalingServiceClient : System.ServiceModel.ClientBase<IScalingService>, IScalingService
{

    /// <summary>
    /// Initializes a new instance of the <see cref="ScalingServiceClient"/> class.
    /// </summary>
    public ScalingServiceClient()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScalingServiceClient"/> class.
    /// </summary>
    /// <param name="endpointConfigurationName">Name of the endpoint configuration.</param>
    public ScalingServiceClient(string endpointConfigurationName) :
        base(endpointConfigurationName)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScalingServiceClient"/> class.
    /// </summary>
    /// <param name="endpointConfigurationName">Name of the endpoint configuration.</param>
    /// <param name="remoteAddress">The remote address.</param>
    public ScalingServiceClient(string endpointConfigurationName, string remoteAddress) :
        base(endpointConfigurationName, remoteAddress)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScalingServiceClient"/> class.
    /// </summary>
    /// <param name="endpointConfigurationName">Name of the endpoint configuration.</param>
    /// <param name="remoteAddress">The remote address.</param>
    public ScalingServiceClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) :
        base(endpointConfigurationName, remoteAddress)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScalingServiceClient"/> class.
    /// </summary>
    /// <param name="binding">The binding.</param>
    /// <param name="remoteAddress">The remote address.</param>
    public ScalingServiceClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) :
        base(binding, remoteAddress)
    {
    }

    /// <summary>
    /// Lists the deployment sizes for the different cloud providers.
    /// </summary>
    /// <returns>A collection of DeploymentSize instances describing the available instances for the different cloud providers.</returns>
    public List<Microsoft.EMIC.Cloud.GenericWorker.DeploymentSize> ListDeployments()
    {
        return base.Channel.ListDeployments();
    }

    /// <summary>
    /// Updates the given deployment.
    /// </summary>
    /// <param name="newDeploymentSize">New size of the deployment.</param>
    public void UpdateDeployment(Microsoft.EMIC.Cloud.GenericWorker.DeploymentSize newDeploymentSize)
    {
        base.Channel.UpdateDeployment(newDeploymentSize);
    }

    /// <summary>
    /// Creates the unprotected client.
    /// </summary>
    /// <param name="scalingServiceUrl">The scaling service URL.</param>
    /// <returns></returns>
    public static ScalingServiceClient CreateUnprotectedClient(string scalingServiceUrl)
    {
        return new ScalingServiceClient(
                  new WS2007HttpBinding(SecurityMode.None, reliableSessionEnabled: false),
                  new EndpointAddress(new Uri(scalingServiceUrl)));
    }

    /// <summary>
    /// Creates the secure client.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <param name="issuer">The issuer.</param>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <param name="serviceCert">The service cert.</param>
    /// <returns></returns>
    public static ScalingServiceClient CreateSecureClient(EndpointAddress address, EndpointAddress issuer, string username, string password, X509Certificate2 serviceCert)
    {
        var secureBinding = WCFUtils.CreateSecureUsernamePasswordClientBinding(issuer);

        var client = new ScalingServiceClient(secureBinding, address);
        client.ClientCredentials.UserName.UserName = username;
        client.ClientCredentials.UserName.Password = password;
        client.ClientCredentials.ServiceCertificate.DefaultCertificate = serviceCert;

        return client;
    }

    /// <summary>
    /// Creates the secure client for self issued tokens.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <param name="claimset">The claimset.</param>
    /// <param name="clientCert">The client cert.</param>
    /// <param name="serviceCert">The service cert.</param>
    /// <returns></returns>
    public static ScalingServiceClient CreateSecureClient(EndpointAddress address, ClaimSet claimset, X509Certificate2 clientCert, X509Certificate2 serviceCert)
    {
        var secureBinding = WCFUtils.CreateSecureSamlBinding();
        var client = new ScalingServiceClient(secureBinding, address);

        #region Create new credential class

        var samlCC = new SamlClientCredentials();

        // Set the client certificate. This is the cert that will be used to sign the SAML token in the symmetric proof key case
        samlCC.ClientCertificate.Certificate = clientCert;

        // Set the service certificate. This is the cert that will be used to encrypt the proof key in the symmetric proof key case
        samlCC.ServiceCertificate.DefaultCertificate = serviceCert;

        samlCC.Claims = claimset;

        samlCC.Audience = address.Uri;

        #endregion

        // set new credentials
        client.ChannelFactory.Endpoint.Behaviors.Remove(typeof(ClientCredentials));
        client.ChannelFactory.Endpoint.Behaviors.Add(samlCC);

        return client;
    }
}
