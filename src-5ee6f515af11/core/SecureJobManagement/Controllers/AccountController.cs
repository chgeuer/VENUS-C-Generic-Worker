//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using SecureJobManagement.Models;
using Microsoft.EMIC.Cloud.GenericWorker.AzureSecurity;
using Microsoft.WindowsAzure;
using System.ComponentModel.Composition.Hosting;
using Microsoft.EMIC.Cloud;
using ManagementWeb;

namespace SecureJobManagement.Controllers
{
    public class AccountController : Controller
    {
        internal UserTableTableDataContext CreateUserTableContext(CloudStorageAccount account, string userTableTableName)
        {
            return new UserTableTableDataContext(account.TableEndpoint.AbsoluteUri, account.Credentials,
                                                 userTableTableName);
        }

        //
        // GET: /Account/LogOn

        public ActionResult LogOn()
        {
            return View();
        }

        //
        // POST: /Account/LogOn

        [HttpPost]
        public ActionResult LogOn(LogOnModel model, string returnUrl)
        {
            var CompositionContainer = AppStart_MefContribMVC3.Container;
            var userTableConnectionString = CompositionContainer.GetExportedValue<string>(CompositionIdentifiers.STSOnAzureConnectionString);
            var userTableTableName = CompositionContainer.GetExportedValue<string>(CompositionIdentifiers.DevelopmentSecurityTokenServiceUserTableName);
            var account = CloudStorageAccount.Parse(userTableConnectionString);

            var InSecureAccessAllowed = CompositionContainer.GetExportedValue<bool>(CompositionIdentifiers.SecurityAllowInsecureAccess);
            if (ModelState.IsValid)
            {
                var ctx = CreateUserTableContext(account, userTableTableName);
                var authenticatedAsAnonymous = (InSecureAccessAllowed && model.UserName == "anonymous");
                if (authenticatedAsAnonymous || (!string.IsNullOrWhiteSpace(model.Password) && ctx.AreCredentialsValid(model.UserName, model.Password)) )
                {
                    FormsAuthentication.SetAuthCookie(model.UserName, model.RememberMe);
                    if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                        && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "The user name or password provided is incorrect.");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/LogOff

        public ActionResult LogOff()
        {
            FormsAuthentication.SignOut();

            return RedirectToAction("Index", "Home");
        }


    }
}
