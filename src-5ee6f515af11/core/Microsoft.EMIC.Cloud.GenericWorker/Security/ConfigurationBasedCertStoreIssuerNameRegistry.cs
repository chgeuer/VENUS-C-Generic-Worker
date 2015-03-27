//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.EMIC.Cloud.Security
{
    /// <summary>
    /// An <see cref="Microsoft.IdentityModel.Tokens.IssuerNameRegistry"/> which allows to refer to X.509 certificates which are trusted. 
    /// </summary>
    /// <example>
    /// This example shows how configure the system within the application's &lt;microsoft.identityModel&gt; config section. 
    /// <code>
    ///&lt;issuerNameRegistry type="Microsoft.EMIC.Cloud.Security.ConfigurationBasedCertStoreIssuerNameRegistry, Microsoft.EMIC.Cloud.GenericWorker"&gt;
    ///  &lt;trustedCertificates&gt;
    ///    &lt;add name="OwnSTS" 
    ///         findValue="CN=my.genericworker.com" 
    ///         storeLocation="LocalMachine" storeName="AddressBook" 
    ///         x509FindType="FindBySubjectDistinguishedName" /&gt;
    ///    &lt;add name="CN=ca.breincompany1.com" 
    ///         thumbprint="7F7B698294804CC0500B704A0288BECA2C431AB1" /&gt;
    ///  &lt;/trustedCertificates&gt;
    ///&lt;/issuerNameRegistry&gt;
    /// </code>
    /// </example>
    public class ConfigurationBasedCertStoreIssuerNameRegistry : ConfigurationBasedIssuerNameRegistry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationBasedCertStoreIssuerNameRegistry"/> class.
        /// </summary>
        public ConfigurationBasedCertStoreIssuerNameRegistry() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationBasedCertStoreIssuerNameRegistry"/> class.
        /// </summary>
        /// <param name="customConfiguration">The custom configuration.</param>
        public ConfigurationBasedCertStoreIssuerNameRegistry(XmlNodeList customConfiguration)
            : base()
        {
            Trace.TraceInformation("WIF processes the ConfigurationBasedCertStoreIssuerNameRegistry");

            if (customConfiguration == null)
            {
                throw new ArgumentNullException("customConfiguration");
            }
            List<XmlElement> xmlElements = ConfigurationBasedCertStoreIssuerNameRegistry.GetXmlElements(customConfiguration);
            if (xmlElements.Count != 1)
            {
                throw new ArgumentException(ExceptionMessages.InvalidXMLElementCount, "customConfiguration");
            }
            XmlElement customConfigurationElement = xmlElements[0];
            if (!StringComparer.Ordinal.Equals(customConfigurationElement.LocalName, N.trustedCertificates))
            {
                throw new ArgumentException(string.Format(ExceptionMessages.MustBe, N.trustedCertificates), "customConfiguration");
            }
            foreach (XmlNode node in customConfigurationElement.ChildNodes)
            {
                var element = node as XmlElement;
                if (element != null)
                {
                    var trustedCertType = DetermineTrustedCertType(element);
                    if (trustedCertType == TrustedCertType.Error)
                    {
                        throw new ArgumentException(string.Format(
                            ExceptionMessages.ConfusingAttributes,
                            N.thumbprint,
                            N.storeLocation, N.storeName, N.findValue, N.findType,
                            element.OuterXml));
                    }

                    if (StringComparer.Ordinal.Equals(element.LocalName, N.add))
                    {
                        if (element.Attributes.GetNamedItem(N.issuerName) == null)
                        {
                            throw new ArgumentException(string.Format(ExceptionMessages.MissingAttribute, N.issuerName), "customConfiguration");
                        }

                        string issuerName = element.Attributes.GetNamedItem(N.issuerName).Value;

                        if (trustedCertType == TrustedCertType.CertStore)
                        {
                            string thumbprint;
                            if (GetThumbPrintFromCertificateReference(element, out thumbprint))
                            {
                                this.ConfiguredTrustedIssuers.Add(thumbprint, issuerName);
                            }
                        }
                        else if (trustedCertType == TrustedCertType.ThumbPrint)
                        {
                            string thumbprint = element.Attributes.GetNamedItem(N.thumbprint).Value;
                            this.ConfiguredTrustedIssuers.Add(thumbprint, issuerName);
                        }
                        else
                        {
                            throw new ArgumentException();
                        }
                    }
                    else if (StringComparer.Ordinal.Equals(element.LocalName, N.remove))
                    {
                        if (trustedCertType == TrustedCertType.CertStore)
                        {
                            string thumbprint;
                            if (GetThumbPrintFromCertificateReference(element, out thumbprint))
                            {
                                this.ConfiguredTrustedIssuers.Remove(thumbprint);
                            }
                        }
                        else if (trustedCertType == TrustedCertType.ThumbPrint)
                        {
                            string thumbprint = element.Attributes.GetNamedItem(N.thumbprint).Value;
                            this.ConfiguredTrustedIssuers.Remove(thumbprint);
                        }
                        else
                        {
                            throw new ArgumentException();
                        }
                    }
                    else if (!StringComparer.Ordinal.Equals(element.LocalName, N.clear))
                    {
                        this.ConfiguredTrustedIssuers.Clear();
                    }
                    else
                    {
                        throw new ArgumentException();
                    }
                }
            }
        }

        // Method overridden to allow setting of breakpoints for debugging purposes.
        /// <summary>
        /// Gets the name of the issuer.
        /// </summary>
        /// <param name="securityToken">The security token.</param>
        /// <returns></returns>
        public override string GetIssuerName(SecurityToken securityToken)
        {
            Trace.TraceInformation("WIF GetIssuerName()");

            var token = securityToken as X509SecurityToken;
            
            try
            {
                string issuer = base.GetIssuerName(securityToken);

                Trace.TraceInformation(string.Format("WIF GetIssuerName() == \"{0}\"", issuer));
                
                return issuer;
            }
            catch (Exception ex)
            {
                string message = String.Format(
                    "IssuerNameRegistry cannot resolve the given issuer token:{0}{1}{2}" +
                    "If this is a X509SecurityToken configure the IssuerNameRegistry to resolve this certificate.",
                    Environment.NewLine,
                    (token != null ? token.Certificate.ToString() : securityToken.GetType().ToString()),
                    Environment.NewLine);

                Trace.TraceError(string.Format("Exception:{0}", message));

                throw new InvalidOperationException(message, ex);
            }
        }

        private bool GetThumbPrintFromCertificateReference(XmlElement e, out string thumbprint)
        {
            string findTypeStr = e.Attributes.GetNamedItem(N.findType).Value;
            var ft = (X509FindType)Enum.Parse(typeof(X509FindType), findTypeStr);

            string fv = e.Attributes.GetNamedItem(N.findValue).Value;

            string storeLocationStr = e.Attributes.GetNamedItem(N.storeLocation).Value;
            var sl = (StoreLocation)Enum.Parse(typeof(StoreLocation), storeLocationStr);

            string sn = e.Attributes.GetNamedItem(N.storeName).Value;

            return GetThumbPrintFromLocallyInstalledCertificate(sl, sn, ft, fv, out thumbprint);
        }

        /// <summary>
        /// Gets the thumb print from locally installed certificate.
        /// </summary>
        /// <param name="sl">The sl.</param>
        /// <param name="sn">The sn.</param>
        /// <param name="ft">The ft.</param>
        /// <param name="fv">The fv.</param>
        /// <param name="thumbprint">The thumbprint.</param>
        /// <returns></returns>
        private bool GetThumbPrintFromLocallyInstalledCertificate(StoreLocation sl, string sn, X509FindType ft, string fv, out string thumbprint)
        {
            #region Lookup thumbprint

            X509Store store = null;
            try
            {
                store = new X509Store(sn, sl);
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certs =
                    store.Certificates.Find(ft, fv, false);

                if (certs.Count == 0)
                {
                    Trace.TraceWarning(string.Format("Cannot locate certificate \"{0}\" in certificate store {1}/{2} (when searching using {3})",
                        fv, sl, sn, ft.ToString()));

                    thumbprint = null;
                    return false;
                }

                var cert = (X509Certificate2)certs[0];
                thumbprint = cert.Thumbprint;
                cert.Reset();

                return true;
            }
            finally
            {
                if (store != null) store.Close();
            }

            #endregion
        }

        private void Add(string toBeRegisteredTokenIssuerName, X509Certificate2 certificate)
        {
            this.ConfiguredTrustedIssuers.Add(toBeRegisteredTokenIssuerName, certificate.Thumbprint);
        }

        /// <summary>
        /// Adds the specified to be registered token issuer name.
        /// </summary>
        /// <param name="toBeRegisteredTokenIssuerName">Name of to be registered token issuer.</param>
        /// <param name="storeLocation">The store location.</param>
        /// <param name="storeName">Name of the store.</param>
        /// <param name="x509FindType">Type of the X509 find.</param>
        /// <param name="x509CertificateFindValue">The X509 certificate find value.</param>
        public void Add(string toBeRegisteredTokenIssuerName, StoreLocation storeLocation,
            string storeName, X509FindType x509FindType, string x509CertificateFindValue)
        {
            string thumbprint;
            if (GetThumbPrintFromLocallyInstalledCertificate(storeLocation, storeName, x509FindType, x509CertificateFindValue, out thumbprint))
            {
                this.ConfiguredTrustedIssuers.Add(toBeRegisteredTokenIssuerName, thumbprint);
            }
            else
            {
                throw new ArgumentException(string.Format(
                    "Cannot locate certificate \"{0}\" in certificate store {1}/{2} (when searching using {3})",
                        x509CertificateFindValue, storeLocation, storeName, x509FindType.ToString()));
            }
        }

        private static TrustedCertType DetermineTrustedCertType(XmlElement e)
        {
            if (null == e) return TrustedCertType.Error;
            var attributes = e.Attributes;

            bool thumbprint = attributes.GetNamedItem(N.thumbprint) != null;
            bool findType = attributes.GetNamedItem(N.findType) != null;
            bool findValue = attributes.GetNamedItem(N.findValue) != null;
            bool storeLocation = attributes.GetNamedItem(N.storeLocation) != null;
            bool storeName = attributes.GetNamedItem(N.storeName) != null;

            if (thumbprint && !findType && !findValue && !storeLocation && !storeName) return TrustedCertType.ThumbPrint;
            if (!thumbprint && findType && findValue && storeLocation && storeName) return TrustedCertType.CertStore;
            return TrustedCertType.Error;
        }

        /// <summary>
        /// Gets the XML elements.
        /// </summary>
        /// <param name="nodeList">The node list.</param>
        /// <returns></returns>
        public static List<XmlElement> GetXmlElements(XmlNodeList nodeList)
        {
            return nodeList.OfType<XmlElement>().ToList();
        }

        private enum TrustedCertType
        {
            ThumbPrint,
            CertStore,
            Error
        }

        private static class N
        {
            internal const string trustedCertificates = "trustedCertificates";
            internal const string add = "add";
            internal const string remove = "remove";
            internal const string clear = "clear";

            internal const string issuerName = "name";
            internal const string thumbprint = "thumbprint";

            internal const string storeLocation = "storeLocation";
            internal const string storeName = "storeName";
            internal const string findType = "x509FindType";
            internal const string findValue = "findValue";
        }
    }
}
