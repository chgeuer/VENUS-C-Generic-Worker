//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Microsoft.EMIC.Cloud.Security
{
    using System;
    using System.IdentityModel.Tokens;
    using Microsoft.IdentityModel.Claims;
    using Microsoft.IdentityModel.Tokens;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class ProtectedCookieSessionSecurityTokenHandler : SessionSecurityTokenHandler
    {
        /// <summary>
        /// Validates the token.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="endpointId">The endpoint id.</param>
        /// <returns></returns>
        public override ClaimsIdentityCollection ValidateToken(SessionSecurityToken token, string endpointId)
        {
            if (token == null)
            {
                throw new ArgumentNullException("token");
            }
            if (String.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentException("endpointId");
            }

            // in active cases where absolute uris are used check the all parts of the token's 
            // endpoint id and this endpoint's id for equality except the port number
            Uri listenerEndpointId;
            var listenerHasUri = Uri.TryCreate(endpointId, UriKind.Absolute, out listenerEndpointId);
            Uri tokenEndpointId;
            var tokenHasUri = Uri.TryCreate(token.EndpointId, UriKind.Absolute, out tokenEndpointId);

            if (listenerHasUri && tokenHasUri)
            {
                if (listenerEndpointId.Scheme != tokenEndpointId.Scheme ||
                    listenerEndpointId.DnsSafeHost != tokenEndpointId.DnsSafeHost ||
                    listenerEndpointId.AbsolutePath != tokenEndpointId.AbsolutePath)
                {
                    throw new SecurityTokenValidationException(String.Format(ExceptionMessages.IncomingTokenNotScoped, tokenEndpointId, listenerEndpointId));
                }
            }
            else if (String.Equals(endpointId, token.EndpointId, StringComparison.Ordinal) == false)
            {
                // in all other cases, fall back to string comparison
                throw new SecurityTokenValidationException(String.Format(ExceptionMessages.IncomingTokenNotScoped, token.EndpointId, endpointId));
            }

            return this.ValidateToken(token);
        }
    }
}
