//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.Diagnostics;
using System.Threading;
using Microsoft.IdentityModel.Claims;

namespace MVCWIFHelpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Web.Mvc;
        
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ClaimsAuthorizeAttribute : FilterAttribute, IAuthorizationFilter
    {
        public ClaimsAuthorizeAttribute() { }
    
        public void OnAuthorization(AuthorizationContext filterContext)
        {
            if (filterContext == null)
            {
                throw new ArgumentNullException("filterContext");
            }

            if (!filterContext.RequestContext.HttpContext.User.Identity.IsAuthenticated)
            {
                filterContext.Result = new HttpUnauthorizedResult();
            }

            var ic = Thread.CurrentPrincipal.Identity as IClaimsIdentity;
            if (ic == null)
                throw new NotSupportedException("Need an IClaimsIdentity");

            ic.Claims.ToList().ForEach(claim => Trace.TraceInformation("claim " + claim));
        }
    }
}