//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OGF.BES;
using Microsoft.EMIC.Cloud.GenericWorker;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Net;
using System.ServiceModel.Security;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.EMIC.Cloud.COMPSsClient
{
    public class COMPSsClient : BESFactoryPortTypeClient
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="COMPsClient"/> class.
        /// </summary>
        public COMPSsClient() : base() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="COMPsClient"/> class.
        /// </summary>
        /// <param name="endpointConfigurationName">Name of the endpoint configuration.</param>
        public COMPSsClient(string endpointConfigurationName) : base(endpointConfigurationName) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="COMPsClient"/> class.
        /// </summary>
        /// <param name="endpointConfigurationName">Name of the endpoint configuration.</param>
        /// <param name="remoteAddress">The remote address.</param>
        public COMPSsClient(string endpointConfigurationName, string remoteAddress) : base(endpointConfigurationName, remoteAddress) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="COMPsClient"/> class.
        /// </summary>
        /// <param name="endpointConfigurationName">Name of the endpoint configuration.</param>
        /// <param name="remoteAddress">The remote address.</param>
        public COMPSsClient(string endpointConfigurationName, EndpointAddress remoteAddress) : base(endpointConfigurationName, remoteAddress) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="COMPsClient"/> class.
        /// </summary>
        /// <param name="binding">The binding.</param>
        /// <param name="remoteAddress">The remote address.</param>
        public COMPSsClient(Binding binding, EndpointAddress remoteAddress) : base(binding, remoteAddress) { }

        #endregion

        #region DefaultBESFactoryPortTypeClient members

        new public CreateActivityResponse CreateActivity(CreateActivityRequest request)
        {
            return ((BESFactoryPortType)this).CreateActivity(request);
        }

        new public GetActivityStatusesResponse GetActivityStatuses(GetActivityStatusesRequest request)
        {
            return ((BESFactoryPortType)this).GetActivityStatuses(request);
        }

        new public TerminateActivitiesResponse TerminateActivities(TerminateActivitiesRequest request)
        {
            return ((BESFactoryPortType)this).TerminateActivities(request);
        }

        new public GetActivityDocumentsResponse GetActivityDocuments(GetActivityDocumentsRequest request)
        {
            return ((BESFactoryPortType)this).GetActivityDocuments(request);
        }

        new public GetFactoryAttributesDocumentResponse GetFactoryAttributesDocument(GetFactoryAttributesDocumentRequest request)
        {
            return ((BESFactoryPortType)this).GetFactoryAttributesDocument(request);
        }

        #endregion

        #region BESJSDLExtensions

        new public void RemoveTerminatedJobs(string owner)
        {
            throw new NotSupportedException(ExceptionMessages.COMPSsRemoveTerminatedJobs);
        }

        new public int GetNumberOfJobs(string owner, List<JobStatus> statuses)
        {
            throw new NotSupportedException(ExceptionMessages.COMPSsGetNumberOfJobs);
        }

        public List<OGF.BES.EndpointReferenceType> GetJobs(string owner)
        {
            throw new NotSupportedException(ExceptionMessages.COMPSsGetJobs);
        }

        public List<OGF.BES.EndpointReferenceType> GetAllJobs()
        {
            throw new NotSupportedException(ExceptionMessages.COMPSsGetAllJobs);
        }

        public List<OGF.BES.EndpointReferenceType> GetHierarchy(EndpointReferenceType root)
        {
            throw new NotSupportedException(ExceptionMessages.COMPSsGetHierarchy);
        }

        new public OGF.BES.EndpointReferenceType GetRoot(EndpointReferenceType job)
        {
            throw new NotSupportedException(ExceptionMessages.COMPSsGetRoot);
        }

        public List<EndpointReferenceType> GetJobsByGroup(string owner, string groupName)
        {
            throw new NotSupportedException(ExceptionMessages.COMPSsGetJobsByGroup);
        }

        new public void CancelGroup(string owner, string groupName)
        {
            throw new NotSupportedException(ExceptionMessages.COMPSsCancelGroup);
        }

        new public void CancelHierarchy(EndpointReferenceType root)
        {
            throw new NotSupportedException(ExceptionMessages.COMPSsCancelHierarchy);
        }

        #endregion

        private static Binding CreateUnsecureBinding()
        {
            TextMessageEncodingBindingElement textMessageEncoding = new TextMessageEncodingBindingElement()
            {
                MaxReadPoolSize = 64,
                MaxWritePoolSize = 16,
                MessageVersion = MessageVersion.Soap12,
                WriteEncoding = Encoding.UTF8,
                ReaderQuotas = new System.Xml.XmlDictionaryReaderQuotas()
                {
                    MaxDepth = 32,
                    MaxStringContentLength = 8192,
                    MaxArrayLength = 16384,
                    MaxBytesPerRead = 4096,
                    MaxNameTableCharCount = 16384
                }
            };

            HttpTransportBindingElement httpTransport = new HttpTransportBindingElement()
            {
                ManualAddressing = false,
                MaxBufferPoolSize = 524288,
                MaxReceivedMessageSize = 65536,
                AllowCookies = false,
                AuthenticationScheme = AuthenticationSchemes.Anonymous,
                BypassProxyOnLocal = false,
                DecompressionEnabled = true,
                HostNameComparisonMode = HostNameComparisonMode.StrongWildcard,
                KeepAliveEnabled = true,
                MaxBufferSize = 65536,
                ProxyAuthenticationScheme = AuthenticationSchemes.Anonymous,
                Realm = "",
                TransferMode = TransferMode.Buffered,
                UnsafeConnectionNtlmAuthentication = false,
                UseDefaultWebProxy = true
            };
            BindingElementCollection collection = new BindingElementCollection() { textMessageEncoding, httpTransport };

            CustomBinding customBinding = new CustomBinding(collection);

            return customBinding;
        }

        public static COMPSsClient CreateUnprotectedClient(string endPointAddress)
        {

            var customBinding = CreateUnsecureBinding();

            return new COMPSsClient(customBinding, new EndpointAddress(new Uri(endPointAddress)));
        }

        private static Binding CreateSecureBinding()
        {
            //Security binding element
            TransportSecurityBindingElement security = SecurityBindingElement.CreateUserNameOverTransportBindingElement();
            security.DefaultAlgorithmSuite = SecurityAlgorithmSuite.Basic128;
            security.SetKeyDerivation(true);
            security.SecurityHeaderLayout = SecurityHeaderLayout.Lax;
            security.IncludeTimestamp = true;
            security.KeyEntropyMode = SecurityKeyEntropyMode.CombinedEntropy;
            security.MessageSecurityVersion = MessageSecurityVersion.Default;
            security.EnableUnsecuredResponse = true;

            //LocalClientSettings of the securitybinding element
            var fiveMin = new TimeSpan(0, 5, 0);
            var tenMin = new TimeSpan(0, 10, 0);
            security.LocalClientSettings.CacheCookies = true;
            security.LocalClientSettings.DetectReplays = false;
            security.LocalClientSettings.ReplayCacheSize = 900000;
            security.LocalClientSettings.MaxClockSkew = fiveMin;
            security.LocalClientSettings.MaxCookieCachingTime = TimeSpan.MaxValue;
            security.LocalClientSettings.ReplayWindow = fiveMin;
            security.LocalClientSettings.SessionKeyRenewalInterval = tenMin;
            security.LocalClientSettings.SessionKeyRolloverInterval = fiveMin;
            security.LocalClientSettings.ReconnectTransportOnFailure = true;
            security.LocalClientSettings.TimestampValidityDuration = fiveMin;
            security.LocalClientSettings.CookieRenewalThresholdPercentage = 60;

            //LocalServiceSettings of the securitybinding element
            security.LocalServiceSettings.DetectReplays = false;
            security.LocalServiceSettings.IssuedCookieLifetime = new TimeSpan(10, 0, 0);
            security.LocalServiceSettings.MaxStatefulNegotiations = 128;
            security.LocalServiceSettings.ReplayCacheSize = 900000;
            security.LocalServiceSettings.MaxClockSkew = fiveMin;
            security.LocalServiceSettings.NegotiationTimeout = new TimeSpan(0, 1, 0);
            security.LocalServiceSettings.ReplayWindow = fiveMin;
            security.LocalServiceSettings.InactivityTimeout = new TimeSpan(0, 2, 0);
            security.LocalServiceSettings.SessionKeyRenewalInterval = new TimeSpan(15, 0, 0);
            security.LocalServiceSettings.SessionKeyRolloverInterval = fiveMin;
            security.LocalServiceSettings.ReconnectTransportOnFailure = true;
            security.LocalServiceSettings.MaxPendingSessions = 128;
            security.LocalServiceSettings.MaxCachedCookies = 1000;
            security.LocalServiceSettings.TimestampValidityDuration = fiveMin;

            TextMessageEncodingBindingElement textMessageEncoding = new TextMessageEncodingBindingElement()
            {
                MaxReadPoolSize = 64,
                MaxWritePoolSize = 16,
                MessageVersion = MessageVersion.Default,
                WriteEncoding = Encoding.UTF8,
                ReaderQuotas = new System.Xml.XmlDictionaryReaderQuotas()
                {
                    MaxDepth = 32,
                    MaxStringContentLength = 8192,
                    MaxArrayLength = 16384,
                    MaxBytesPerRead = 4906,
                    MaxNameTableCharCount = 16384
                }
            };

            HttpsTransportBindingElement httpsTransport = new HttpsTransportBindingElement()
            {
                ManualAddressing = false,
                MaxBufferPoolSize = 524288,
                MaxReceivedMessageSize = 65536,
                AllowCookies = false,
                AuthenticationScheme = AuthenticationSchemes.Anonymous,
                BypassProxyOnLocal = false,
                DecompressionEnabled = true,
                HostNameComparisonMode = HostNameComparisonMode.StrongWildcard,
                KeepAliveEnabled = true,
                MaxBufferSize = 65536,
                ProxyAuthenticationScheme = AuthenticationSchemes.Anonymous,
                Realm = "",
                TransferMode = TransferMode.Buffered,
                UnsafeConnectionNtlmAuthentication = false,
                UseDefaultWebProxy = true,
                RequireClientCertificate = false
            };

            BindingElementCollection collection = new BindingElementCollection() { security, textMessageEncoding, httpsTransport };

            var customBinding = new CustomBinding(collection);

            return customBinding;
        }

        public static COMPSsClient CreateSecureClient(EndpointAddress address, string username, string password, X509Certificate2 serviceCert)
        {
            var binding = CreateSecureBinding();

            var client = new COMPSsClient(binding, address);
            client.ClientCredentials.UserName.UserName = username;
            client.ClientCredentials.UserName.Password = password;
            client.ClientCredentials.ServiceCertificate.DefaultCertificate = serviceCert;

            return client;
        }
    }
}
