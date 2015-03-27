//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Globalization;
using System.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace Microsoft.EMIC.Cloud.Security
{
    /// <summary>
    /// Utility class to support retrieval of X.509 certificates from a given store. 
    /// </summary>
    public static class X509Helper
    {
        /// <summary>
        /// Supports retrieval of X.509 certificates from a given store
        /// </summary>
        /// <param name="storeLocation">Location of certificate store (e.g. LocalMachine, CurrentUser)</param>
        /// <param name="storeName">Name of certificate store (e.g. My, TrustedPeople)</param>
        /// <param name="findValue">The find value.</param>
        /// <param name="x509FindType">Type of the X509 find.</param>
        /// <returns>
        /// The specified X509 certificate
        /// </returns>
        public static X509Certificate2 LookupCertificate(StoreLocation storeLocation, string storeName, string findValue, X509FindType x509FindType)
        {
            X509Store store = null;
            try
            {
                store = new X509Store(storeName, storeLocation);
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certs = store.Certificates.Find(x509FindType, findValue, false);
                if (certs.Count != 1)
                {
                    throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture,
                        ExceptionMessages.FedUtilCertificateNotFound,
                        findValue, certs.Count, Enum.GetName(typeof(StoreLocation), storeLocation), storeName));
                }

                return (X509Certificate2)certs[0];
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                if (store != null)
                {
                    store.Close();
                }
            }
        }

        /// <summary>
        /// Public Utility method to get a X509 Certificate from a given store
        /// </summary>
        /// <param name="storeLocation">Location of certificate store (e.g. LocalMachine, CurrentUser)</param>
        /// <param name="storeName">Name of certificate store (e.g. My, TrustedPeople)</param>
        /// <param name="subjectDistinguishedName">The Subject Distinguished Name of the certificate</param>
        /// <returns>The specified X509 certificate</returns>
        public static X509Certificate2 GetX509Certificate2(StoreLocation storeLocation, StoreName storeName, string subjectDistinguishedName)
        {
            return LookupCertificate(storeLocation, storeName, subjectDistinguishedName);
        }

        /// <summary>
        /// Gets the X509 certificate2.
        /// </summary>
        /// <param name="storeLocation">The store location.</param>
        /// <param name="storeName">Name of the store.</param>
        /// <param name="findValue">The find value.</param>
        /// <param name="x509FindType">Type of the X509 find.</param>
        /// <returns></returns>
        public static X509Certificate2 GetX509Certificate2(StoreLocation storeLocation, StoreName storeName, string findValue, X509FindType x509FindType)
        {
            return LookupCertificate(storeLocation, storeName.ToString(), CleanNonAlphanumericCharacters(findValue), x509FindType);
        }

        private static string CleanNonAlphanumericCharacters(string input)
        {
            return Regex.Replace(input, @"[^a-zA-Z0-9]", "");
        }

        #region GetX509TokenFromCert()

        /// <summary>
        /// Utility method to get a X509 Token from a given certificate
        /// </summary>
        /// <param name="storeName">Name of certificate store (e.g. My, TrustedPeople)</param>
        /// <param name="storeLocation">Location of certificate store (e.g. LocalMachine, CurrentUser)</param>
        /// <param name="subjectDistinguishedName">The Subject Distinguished Name of the certificate</param>
        /// <returns>The corresponding X509 Token</returns>
        public static X509SecurityToken GetX509TokenFromCert(StoreName storeName,
                                                              StoreLocation storeLocation,
                                                              string subjectDistinguishedName)
        {
            X509Certificate2 certificate = LookupCertificate(storeLocation, storeName, subjectDistinguishedName);
            X509SecurityToken t = new X509SecurityToken(certificate);
            return t;
        }

        #endregion

        #region GetCertificateThumbprint
        /// <summary>
        /// Utility method to get an X509 Certificate thumbprint
        /// </summary>
        /// <param name="storeName">Name of certificate store (e.g. My, TrustedPeople)</param>
        /// <param name="storeLocation">Location of certificate store (e.g. LocalMachine, CurrentUser)</param>
        /// <param name="subjectDistinguishedName">The Subject Distinguished Name of the certificate</param>
        /// <returns>The corresponding X509 Certificate thumbprint</returns>
        public static byte[] GetCertificateThumbprint(StoreName storeName, StoreLocation storeLocation, string subjectDistinguishedName)
        {
            X509Certificate2 certificate = LookupCertificate(storeLocation, storeName, subjectDistinguishedName);
            return certificate.GetCertHash();
        }

        #endregion

        private static X509Certificate2 LookupCertificate(StoreLocation storeLocation, StoreName storeName, string subjectDistinguishedName)
        {
            return LookupCertificate(storeLocation, storeName.ToString(), subjectDistinguishedName, X509FindType.FindBySubjectDistinguishedName);
        }
    }
}
