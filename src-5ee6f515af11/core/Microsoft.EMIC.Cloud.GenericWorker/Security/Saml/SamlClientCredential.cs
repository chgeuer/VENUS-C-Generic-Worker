//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿
using System.IdentityModel.Claims;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.ServiceModel.Description;
using System;


namespace Microsoft.EMIC.Cloud.Security.Saml
{
    /// <summary>
    /// Custom client credentials class that allows a SAML assertion and associated proof token to be specified
    /// </summary>
    public class SamlClientCredentials : ClientCredentials
    {
        /// <summary>
        /// A ClaimSet containing the claims that will be put into the SAML assertion
        /// </summary>
        ClaimSet claims;

        /// <summary>
        /// The SAML assertion
        /// </summary>
        SamlAssertion assertion;

        /// <summary>
        /// The proof token associated with the SAML assertion
        /// </summary>
        SecurityToken proofToken;

        /// <summary>
        /// The Uri of the endpoint where these credentials are used
        /// </summary>
        Uri audience;
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public SamlClientCredentials()
            : base()
        {
            // Set SupportInteractive to false to suppress Cardspace UI
            base.SupportInteractive = false;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="other">The SamlClientCredentials to create a copy of</param>
        protected SamlClientCredentials(SamlClientCredentials other) : base ( other )
        {
            // Just do reference copy given sample nature
            this.assertion = other.assertion;            
            this.claims = other.claims;
            this.proofToken = other.proofToken;
            this.audience = other.audience;
        }

        /// <summary>
        /// Property allowing the SAML assertion to be specified and retrieved
        /// </summary>
        public SamlAssertion Assertion { get { return assertion; } set { assertion = value; } }

        /// <summary>
        /// Property allowing the proof token to be specified and retrieved
        /// </summary>
        public SecurityToken ProofToken { get { return proofToken; } set { proofToken = value; } }

        /// <summary>
        /// Property allowing the ClaimSet to be specified and retrieved
        /// </summary>
        public ClaimSet Claims { get { return claims; } set { claims = value; } }

        /// <summary>
        /// Property allowing the audience-Uri to be specified and retrieved
        /// </summary>
        public Uri Audience { get { return audience; } set { audience = value; } }

        /// <summary>
        /// Creates a new copy of this <see cref="T:System.ServiceModel.Description.ClientCredentials"/> instance.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.ServiceModel.Description.ClientCredentials"/> instance.
        /// </returns>
        protected override ClientCredentials CloneCore()
        {
            return new SamlClientCredentials(this);            
        }

        /// <summary>
        /// Creates a security token manager for this instance. This method is rarely called explicitly; it is primarily used in extensibility scenarios and is called by the system itself.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.ServiceModel.ClientCredentialsSecurityTokenManager"/> for this <see cref="T:System.ServiceModel.Description.ClientCredentials"/> instance.
        /// </returns>
        public override SecurityTokenManager CreateSecurityTokenManager()
        {
            // return custom security token manager
            return new SamlSecurityTokenManager(this);
        }
    }
}

