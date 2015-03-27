//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace MVCWIFHelpers
{
    using System;
    using System.Collections.Generic;
    using System.Web.Mvc;
    using Microsoft.IdentityModel.Protocols.WSFederation;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public sealed class RequireWSFederationMessageAttribute : FilterAttribute, IAuthorizationFilter
    {
        // Lookup table for converting string actions to the associated flag
        private static readonly Dictionary<string, WSFederationMessageActions> _actionLookup = new Dictionary<string, WSFederationMessageActions>()
    {
      { WSFederationConstants.Actions.Attribute, WSFederationMessageActions.Attribute },
      { WSFederationConstants.Actions.Pseudonym, WSFederationMessageActions.Pseudonym },
      { WSFederationConstants.Actions.SignIn, WSFederationMessageActions.SignIn },
      { WSFederationConstants.Actions.SignOut, WSFederationMessageActions.SignOut },
      { WSFederationConstants.Actions.SignOutCleanup, WSFederationMessageActions.SignOutCleanup },
    };

        public WSFederationMessageActions AllowedActions { get; set; }

        private object _typeId = new object();
        public override object TypeId
        {
            get
            {
                return this._typeId;
            }
        }

        public RequireWSFederationMessageAttribute()
        {
            // Default to allowing all actions.
            this.AllowedActions = WSFederationMessageActions.All;
        }

        public bool IsAllowed(string action)
        {
            if (
              String.IsNullOrWhiteSpace(action) ||
              !_actionLookup.ContainsKey(action)
            )
            {
                return false;
            }
            var enumAction = _actionLookup[action];
            return (this.AllowedActions & enumAction) == enumAction;
        }

        public void OnAuthorization(System.Web.Mvc.AuthorizationContext filterContext)
        {
            if (filterContext == null)
            {
                throw new ArgumentNullException("filterContext");
            }

            // If you can't parse out a message or if the parsed message
            // isn't an allowed action, deny the request.
            WSFederationMessage message = null;
            if (!WSFederationMessage.TryCreateFromUri(filterContext.HttpContext.Request.Url, out message) ||
                !this.IsAllowed(message.Action))
            {
                filterContext.Result = new HttpUnauthorizedResult();
                return;
            }
        }
    }
}
