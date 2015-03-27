//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace MVCWIFHelpers
{
    using System;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [Flags]
    public enum WSFederationMessageActions
    {
        All = WSFederationMessageActions.Attribute | WSFederationMessageActions.Pseudonym | WSFederationMessageActions.SignIn | WSFederationMessageActions.SignOut | WSFederationMessageActions.SignOutCleanup,
        Attribute = 1,
        Pseudonym = 2,
        SignIn = 4,
        SignOut = 8,
        SignOutCleanup = 16
    }
}
