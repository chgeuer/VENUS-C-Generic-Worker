//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Microsoft.EMIC.Cloud.Security
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.IdentityModel.Web;
    using Microsoft.IdentityModel.Tokens;

    /// <summary>
    /// Handles session cookie tokens in a load-balanced environment by encrypting &amp; signing them with the service's certificate. 
    /// 
    /// The default encryption strategy followed by WIF for session tokens is 
    /// to use DPAPI, would create problems when the client interacts with 
    /// multiple instances: a session token encrypted by a given instance would 
    /// not be readable by any other. As an alternative you will use the 
    /// service certificate for securing the session: more about this below. 
    /// The mechanism that WIF provides for customizing the way in which 
    /// session tokens are processed consists in providing a custom 
    /// <see cref="SessionSecurityTokenHandler "/> class.
    /// </summary>
    public class RsaSessionSecurityTokenHandler : ProtectedCookieSessionSecurityTokenHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RsaSessionSecurityTokenHandler"/> class.
        /// </summary>
        /// <param name="certificate">The certificate.</param>
        public RsaSessionSecurityTokenHandler(X509Certificate2 certificate)
        {
            if (certificate == null)
                throw new ArgumentNullException("certificate");

            var transforms = new List<CookieTransform>
                                 {
                                     new DeflateCookieTransform(),
                                     new RsaEncryptionCookieTransform(certificate),
                                     new RsaSignatureCookieTransform(certificate)
                                 };
            this.SetTransforms(transforms);
        }
    }
}
