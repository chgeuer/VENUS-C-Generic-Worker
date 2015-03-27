//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace MVCWIFHelpers
{
    using System;
    using System.Web.Mvc;
    using Microsoft.IdentityModel.Protocols.WSFederation;
    using Microsoft.IdentityModel.Web;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizationFilterAttribute : FilterAttribute, IAuthorizationFilter
    {
        public AuthorizationFilterAttribute()
        {
        }

        #region IAuthorizationFilter Members

        public void OnAuthorization(System.Web.Mvc.AuthorizationContext filterContext)
        {
            var fam = FederatedAuthentication.WSFederationAuthenticationModule;
            var signIn = new SignInRequestMessage(new Uri(fam.Issuer), fam.Realm)
            {
                Context = filterContext.HttpContext.Request.RawUrl
            };

            string redirectUrl = signIn.WriteQueryString();
            filterContext.Result = new RedirectResult(redirectUrl);
        }

        #endregion
    }
}
