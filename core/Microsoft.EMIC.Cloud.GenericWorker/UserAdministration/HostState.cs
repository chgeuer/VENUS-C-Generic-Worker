//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.ServiceModel;

namespace Microsoft.EMIC.Cloud.UserAdministration
{
    /// <summary>
    /// HostState class
    /// </summary>
    public class HostState
    {
        private ServiceHost _serviceHost;

        /// <summary>
        /// Sets the service host.
        /// </summary>
        /// <param name="host">The host.</param>
        public void SetServiceHost(ServiceHost host)
        {
            _serviceHost = host;
        }

        /// <summary>
        /// Gets the status.
        /// </summary>
        public CommunicationState Status
        {
            get {
                return _serviceHost != null ? _serviceHost.State : CommunicationState.Closed;
            }
        }
    }
}