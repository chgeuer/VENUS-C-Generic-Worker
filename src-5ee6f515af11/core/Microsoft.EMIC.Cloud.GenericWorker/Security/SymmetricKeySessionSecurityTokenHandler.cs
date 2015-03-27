//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Microsoft.EMIC.Cloud.Security
{
    using System;
    using System.Collections.Generic;
    using Microsoft.IdentityModel.Web;
    
    /// <summary>
    /// Handles session cookie tokens in a load-balanced environment by encrypting &amp; signing them under a symmetric key. 
    /// </summary>
    public class SymmetricKeySessionSecurityTokenHandler : ProtectedCookieSessionSecurityTokenHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SymmetricKeySessionSecurityTokenHandler"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        public SymmetricKeySessionSecurityTokenHandler(byte[] key)
        {
             if (key == null)
                throw new ArgumentNullException("key");

            var transforms = new List<CookieTransform>
                                 {
                                     new DeflateCookieTransform(),
                                     new SymmetricKeyCookieTransform(key)
                                 };

            this.SetTransforms(transforms);
        }
    }
}
