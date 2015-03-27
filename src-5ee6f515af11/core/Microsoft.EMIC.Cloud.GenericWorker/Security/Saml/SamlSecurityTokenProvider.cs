//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿
using System;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.IO;
using System.ServiceModel.Security;
using System.Xml;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.SecurityTokenService;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens.Saml11;


namespace Microsoft.EMIC.Cloud.Security.Saml
{
    /// <summary>
    /// class that derives from SecurityTokenProvider and returns a SecurityToken that represents a SAML assertion.
    /// </summary>
    public class SamlSecurityTokenProvider : SecurityTokenProvider
    {
        /// <summary>
        /// The SAML assertion that the SamlSecurityTokenProvider returns as a SecurityToken.
        /// </summary>
        SamlAssertion assertion;

        /// <summary>
        /// The proof token associated with the SAML assertion.
        /// </summary>
        SecurityToken proofToken;

        /// <summary>
        /// The X509 certificate for encrypting the SAML assertion.
        /// </summary>
        X509Certificate2 relyingPartyCert;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="assertion">The SAML assertion that the SamlSecurityTokenProvider returns as a SecurityToken</param>
        /// <param name="proofToken">The proof token associated with the SAML assertion</param>
        /// <param name="relyingPartyCert">The X509 certificate for encrypting the SAML assertion</param>
        public SamlSecurityTokenProvider(SamlAssertion assertion, SecurityToken proofToken, X509Certificate2 relyingPartyCert)
        {
            this.assertion = assertion;
            this.proofToken = proofToken;
            this.relyingPartyCert = relyingPartyCert;
        }

        /// <summary>
        /// Creates the security token
        /// </summary>
        /// <param name="timeout">Maximum amount of time the method is supposed to take. Ignored in this implementation.</param>
        /// <returns>A SecurityToken that corresponds to the SAML assertion and proof key specified at construction time</returns>
        protected override SecurityToken GetTokenCore(TimeSpan timeout)
        {
            // Create a SamlSecurityToken from the provided assertion.
            SamlSecurityToken samlToken = new SamlSecurityToken(assertion);

            // To encrypt the SAML Token, it has to wrapped by an encrypted Token.
            var encryptCredentials = new EncryptedKeyEncryptingCredentials(relyingPartyCert);
            EncryptedSecurityToken encryptedToken = new EncryptedSecurityToken(samlToken, encryptCredentials);
            
            // Create a EncryptedSecurityTokenHandler that is used to serialize the EncryptedSecurityToken
            EncryptedSecurityTokenHandler est = new EncryptedSecurityTokenHandler();

            // Create a Saml11SecurityTokenHandler that is used to serialize the SamlSecurityToken inside the EncryptedSecurityToken
            Saml11SecurityTokenHandler sst = new Saml11SecurityTokenHandler();

            // Add both handler to a collection
            SecurityTokenHandlerCollection sthc = new SecurityTokenHandlerCollection();
            sthc.Add(est);
            sthc.Add(sst);


            // Create a memory stream to write the serialized token into
            // Use an initial size of 64Kb
            MemoryStream s = new MemoryStream(UInt16.MaxValue);

            // Create an XmlWriter over the stream
            XmlWriter xw = XmlWriter.Create(s);
            
            // Write the SamlSecurityToken into the stream
            est.WriteToken(xw, encryptedToken);

            // Be sure that everything is written to the stream
            xw.Flush();
            xw.Close();
            s.Flush();

            // Seek back to the beginning of the stream
            s.Seek(0, SeekOrigin.Begin);

            // Load the serialized token into a DOM
            XmlDocument dom = new XmlDocument();
            dom.Load(s);
            
            // Create a KeyIdentifierClause for the SamlSecurityToken
            SamlAssertionKeyIdentifierClause samlKeyIdentifierClause = samlToken.CreateKeyIdentifierClause<SamlAssertionKeyIdentifierClause>();

            // Return a GenericXmlToken from the XML for the SamlSecurityToken, the proof token, the valid from 
            // and valid until times from the assertion and the key identifier clause created above            
            return new GenericXmlSecurityToken(dom.DocumentElement, proofToken, assertion.Conditions.NotBefore, assertion.Conditions.NotOnOrAfter, samlKeyIdentifierClause, samlKeyIdentifierClause, null);
        }
    }
}

