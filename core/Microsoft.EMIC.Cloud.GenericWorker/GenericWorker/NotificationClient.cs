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
    using Microsoft.EMIC.Cloud.Notification;
    using OGF.BES;

    /// <summary>
    /// A client to access the notification service.
    /// </summary>
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    public partial class NotificationServiceClient : System.ServiceModel.ClientBase<INotificationService>, INotificationService
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationServiceClient"/> class.
        /// </summary>
        public NotificationServiceClient()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationServiceClient"/> class.
        /// </summary>
        /// <param name="endpointConfigurationName">Name of the endpoint configuration.</param>
        public NotificationServiceClient(string endpointConfigurationName) :
            base(endpointConfigurationName)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationServiceClient"/> class.
        /// </summary>
        /// <param name="endpointConfigurationName">Name of the endpoint configuration.</param>
        /// <param name="remoteAddress">The remote address.</param>
        public NotificationServiceClient(string endpointConfigurationName, string remoteAddress) :
            base(endpointConfigurationName, remoteAddress)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationServiceClient"/> class.
        /// </summary>
        /// <param name="endpointConfigurationName">Name of the endpoint configuration.</param>
        /// <param name="remoteAddress">The remote address.</param>
        public NotificationServiceClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) :
            base(endpointConfigurationName, remoteAddress)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationServiceClient"/> class.
        /// </summary>
        /// <param name="binding">The binding.</param>
        /// <param name="remoteAddress">The remote address.</param>
        public NotificationServiceClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) :
            base(binding, remoteAddress)
        {
        }

        /// <summary>
        /// Unsubscribes notification for the specified job.
        /// </summary>
        /// <param name="job">The job.</param>
        public void Unsubscribe(EndpointReferenceType job)
        {
            base.Channel.Unsubscribe(job);
        }

        /// <summary>
        /// Creates the subscription on a job for a given set of statuses.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="statuses">The statuses.</param>
        /// <param name="pluginConfig">The plugin config.</param>
        public void CreateSubscriptionForStatuses(EndpointReferenceType job, List<JobStatus> statuses, List<SerializableKeyValuePair<string, string>> pluginConfig)
        {
            base.Channel.CreateSubscriptionForStatuses(job, statuses, pluginConfig);
        }

        /// <summary>
        /// Creates the subscription on a job for all statuses.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="pluginConfig">The plugin config.</param>
        public void CreateSubscription(OGF.BES.EndpointReferenceType job, List<SerializableKeyValuePair<string, string>> pluginConfig)
        {
            base.Channel.CreateSubscription(job, pluginConfig);
        }

        /// <summary>
        /// Creates the unprotected client.
        /// </summary>
        /// <param name="notificationServiceUrl">The notification service URL.</param>
        /// <returns></returns>
        public static NotificationServiceClient CreateUnprotectedClient(string notificationServiceUrl)
        {
            return new NotificationServiceClient(
                      new WS2007HttpBinding(SecurityMode.None, reliableSessionEnabled: false),
                      new EndpointAddress(new Uri(notificationServiceUrl)));
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
        public static NotificationServiceClient CreateSecureClient(EndpointAddress address, EndpointAddress issuer, string username, string password, X509Certificate2 serviceCert)
        {
            var secureBinding = WCFUtils.CreateSecureUsernamePasswordClientBinding(issuer);

            var client = new NotificationServiceClient(secureBinding, address);
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
        public static NotificationServiceClient CreateSecureClient(EndpointAddress address, ClaimSet claimset, X509Certificate2 clientCert, X509Certificate2 serviceCert)
        {
            var secureBinding = WCFUtils.CreateSecureSamlBinding();
            var client = new NotificationServiceClient(secureBinding, address);

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

        /// <summary>
        /// Creates a notification subscription for a given group of jobs on a given set of statuses.
        /// </summary>
        /// <param name="groupName">Name of the group.</param>
        /// <param name="statuses">The statuses.</param>
        /// <param name="pluginConfig">The plugin config.</param>
        public void CreateSubscriptionForGroupStatuses(string groupName, List<JobStatus> statuses, List<SerializableKeyValuePair<string, string>> pluginConfig)
        {
            base.Channel.CreateSubscriptionForGroupStatuses(groupName, statuses, pluginConfig);
        }
    }
}
