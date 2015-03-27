//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace MVCWIFHelpers
{
    using System;
    using System.Web.Mvc;
    using System.Web.Routing;
    using Microsoft.IdentityModel.Protocols.WSFederation;
    using Microsoft.IdentityModel.Web;
    using System.Text;
    using Microsoft.EMIC.Cloud;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthenticateAndAuthorizeAttribute : FilterAttribute, IAuthorizationFilter
    {
        public string Issuer { get; set; }
        public string Roles { get; set; }
        public bool RequireSsl { get; set; }

        public AuthenticateAndAuthorizeAttribute() 
        {
            Issuer = SecurityTokenServiceConfigurationMVC.Current.CompositionContainer.GetExportedValue<string>(CompositionIdentifiers.PassiveSTSURL);    
        }

        public void OnAuthorization(System.Web.Mvc.AuthorizationContext filterContext)
        {
            var InSecureAccessAllowed = SecurityTokenServiceConfigurationMVC.Current.CompositionContainer.GetExportedValue<bool>(CompositionIdentifiers.SecurityAllowInsecureAccess);

            if (!filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                AuthenticateUser(filterContext);
            }
            else
            {
                AuthorizeUser(filterContext);
            }
        }

        private void AuthenticateUser(System.Web.Mvc.AuthorizationContext context)
        {
            var returnUrl = GetReturnUrl(context.RequestContext);
            // user is not authenticated and it's entering for the first time 
            var fam = FederatedAuthentication.WSFederationAuthenticationModule;
            var configuration = FederatedAuthentication.ServiceConfiguration;
            var realm = GetRealm(context.RequestContext);
            configuration.AudienceRestriction.AllowedAudienceUris.Add(new Uri(realm));
            fam.Realm = realm;
            if (!String.IsNullOrEmpty(Issuer))
            {
                fam.Issuer = Issuer;
                fam.RequireHttps = RequireSsl;
            }
            var signIn = new SignInRequestMessage(new Uri(fam.Issuer), realm, returnUrl.ToString());
            context.Result = new RedirectResult(signIn.WriteQueryString());
        }

        private static String GetRealm(RequestContext context)
        {
            var request = context.HttpContext.Request;
            var reqUrl = request.Url;
            var wreply = new StringBuilder();
            wreply.Append(reqUrl.Scheme); // e.g. "http"        
            wreply.Append("://");
            wreply.Append(request.Headers["Host"] ?? reqUrl.Authority);
            return wreply.ToString();
        }

        private static Uri GetReturnUrl(RequestContext context)
        {
            var request = context.HttpContext.Request;
            var result = GetRealm(context);
            var wreply = new StringBuilder(result);
            wreply.Append(request.RawUrl);
            if (!request.ApplicationPath.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                wreply.Append("/");
            }
            return new Uri(wreply.ToString());
        }

        private void AuthorizeUser(System.Web.Mvc.AuthorizationContext context)
        {
            if (String.IsNullOrEmpty(Roles))
                return;
            var authorizedRoles = this.Roles.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            bool hasValidRole = false;
            foreach (var role in authorizedRoles)
            {
                if (context.HttpContext.User.IsInRole(role.Trim()))
                {
                    hasValidRole = true;
                    break;
                }
            }
            if (!hasValidRole)
            {
                context.Result = new HttpUnauthorizedResult();
                return;
            }
        }
    }
}
