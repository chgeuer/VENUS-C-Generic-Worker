//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace MVCWIFHelpers
{
    using System;
    using System.Web;
    using System.Web.Util;
    using Microsoft.IdentityModel.Protocols.WSFederation;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class SampleRequestValidator : RequestValidator
    {
        // <httpRuntime requestValidationType="SampleRequestValidator, MVCWIFHelpers" />
        // <httpRuntime requestValidationMode="2.0" />


        // <httpRuntime requestValidationType="SampleRequestValidator" />
        protected override bool IsValidRequestString(HttpContext context, string value, RequestValidationSource requestValidationSource, string collectionKey, out int validationFailureIndex)
        {
            validationFailureIndex = 0;

            if (context.Items.Contains("CheckingWSFed"))
                return true;

            if (requestValidationSource == RequestValidationSource.Form &&
                collectionKey.Equals(WSFederationConstants.Parameters.Result, StringComparison.Ordinal))
            {
                context.Items.Add("CheckingWSFed", this);

                //var message = WSFederationMessage.CreateFromNameValueCollection(
                //       WSFederationMessageBinder.GetBaseUrl(context.Request.Url),
                //       context.Request.Form);
                //// SignInResponseMessage message = WSFederationMessage.CreateFromFormPost(context.Request) as SignInResponseMessage;
                //if (message != null)
                //{
                //    return true;
                //}

                return true;
            }
            else
            {
                return base.IsValidRequestString(context, value, requestValidationSource, collectionKey,
                                                 out validationFailureIndex);
            }
        }
    }
}
