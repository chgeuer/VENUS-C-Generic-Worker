//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.EMIC.Cloud.UserAdministration
{
    /// <summary>
    /// Settings class
    /// </summary>
    public static class Settings
    {
        /// <summary>
        /// Gets the binding.
        /// </summary>
        public static Binding Binding { get { return new NetNamedPipeBinding(); } }

        /// <summary>
        /// Gets the address.
        /// </summary>
        public static string Address { get { return "net.pipe://localhost/CreateUserPipe"; } }
    }
}
