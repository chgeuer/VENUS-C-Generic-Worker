//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace MVCWIFHelpers
{
    using System;
    using System.Web.Mvc;
    using Microsoft.IdentityModel.Protocols.WSFederation;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class WSFederationMessageBinder : IModelBinder
    {
        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            if (controllerContext == null)
            {
                throw new ArgumentNullException("controllerContext");
            }
            if (bindingContext == null)
            {
                throw new ArgumentNullException("bindingContext");
            }

            try
            {
                if (controllerContext.HttpContext.Request.RequestType == "GET")
                {
                    var message = WSFederationMessage.CreateFromUri(controllerContext.HttpContext.Request.Url);
                    if (!bindingContext.ModelType.IsAssignableFrom(message.GetType()))
                    {
                        throw new WSFederationMessageException();
                    }
                    return message;
                }
                else if (controllerContext.HttpContext.Request.RequestType == "POST")
                {
                    var message = WSFederationMessage.CreateFromNameValueCollection(
                        GetBaseUrl(controllerContext.HttpContext.Request.Url),
                        controllerContext.HttpContext.Request.Form);

                    if (message == null)
                    {
                        message =
                            WSFederationMessage.CreateFromUri(controllerContext.HttpContext.Request.Url);
                    }

                    //var message = WSFederationMessage.CreateFromNameValueCollection(
                    //    controllerContext.HttpContext.Request.Url,
                    //    controllerContext.HttpContext.Request.Params);

                    if (!bindingContext.ModelType.IsAssignableFrom(message.GetType()))
                    {
                        throw new WSFederationMessageException();
                    }
                    return message;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            catch (WSFederationMessageException ex)
            {
                bindingContext.ModelState.AddModelError("", ex);

                return null;
            }
        }

        private static Uri GetBaseUrl(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }
            string absoluteUri = uri.AbsoluteUri;
            int length = absoluteUri.IndexOf("?", 0, StringComparison.Ordinal);
            if (length > -1)
            {
                absoluteUri = absoluteUri.Substring(0, length);
            }
            return new Uri(absoluteUri);
        }

        public static void Register()
        {
            var binder = new WSFederationMessageBinder();

            System.Web.Mvc.ModelBinders.Binders[typeof(WSFederationMessage)] = binder;
            System.Web.Mvc.ModelBinders.Binders[typeof(AttributeRequestMessage)] = binder;
            System.Web.Mvc.ModelBinders.Binders[typeof(PseudonymRequestMessage)] = binder;
            System.Web.Mvc.ModelBinders.Binders[typeof(SignInRequestMessage)] = binder;
            System.Web.Mvc.ModelBinders.Binders[typeof(SignOutRequestMessage)] = binder;
            System.Web.Mvc.ModelBinders.Binders[typeof(SignOutCleanupRequestMessage)] = binder;
        }
    }
}
