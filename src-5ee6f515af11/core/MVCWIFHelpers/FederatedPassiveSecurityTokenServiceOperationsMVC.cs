//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace MVCWIFHelpers
{
    using System;
    using System.Web;
    using Microsoft.IdentityModel.Protocols.WSFederation;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public static class FederatedPassiveSecurityTokenServiceOperationsMVC
    {
        public static void ProcessSignInResponse(SignInResponseMessage signInResponseMessage, HttpResponseBase httpResponse)
        {
            if (signInResponseMessage == null)
            {
                throw new ArgumentNullException("signInResponseMessage");
            }
            if (httpResponse == null)
            {
                throw new ArgumentNullException("httpResponse");
            }

            signInResponseMessage.Write(httpResponse.Output);
            httpResponse.Flush();
            httpResponse.End();
        }
    }
}
