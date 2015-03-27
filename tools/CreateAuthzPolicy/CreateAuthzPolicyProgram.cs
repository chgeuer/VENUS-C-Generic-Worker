//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace CreateAuthzPolicy
{
    using System;
    using Microsoft.EMIC.Cloud.Security;

    /// <summary>
    /// Creates a sample authorization policy
    /// </summary>
    class CreateAuthzPolicyProgram
    {
        static void Main(string[] args)
        {
            var x = VenusClaimsAuthorizationManager.DefaultPolicy.Serialize().ToString().Replace("\r\n", "").Replace("\t", "");
            Console.WriteLine(x);
        }
    }
}
