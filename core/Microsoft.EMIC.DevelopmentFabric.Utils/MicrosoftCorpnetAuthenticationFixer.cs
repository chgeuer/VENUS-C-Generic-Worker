//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿
namespace Microsoft.EMIC.DevelopmentFabric.Utils
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using Microsoft.Web.Administration;
    using Microsoft.Win32;
    using Microsoft.WindowsAzure.ServiceRuntime;

    /// <summary>
    /// Utility for developing/testing in the local development fabric. 
    /// 
    /// Sets the Windows identity of the IIS application pools in the local development fabric to a specified user identity. 
    /// </summary>
    /// <para>
    /// This is necessary to ensure that code running locally can access production storage, 
    /// when the local network is protected by a firewall such as TMG. 
    /// </para>
    public class MicrosoftCorpnetAuthenticationFixer
    {
        // How to determine whether we run in the "real" Windows Azure data center or on a developer laptop? 
        // Win7SP1... Fragile, but it works...
        internal static bool IsDevMachine
        {
            get { return string.Equals("Microsoft Windows NT 6.1.7601 Service Pack 1", Environment.OSVersion.VersionString); }
        }

        internal static bool InFabric
        {
            get { return RoleEnvironment.IsAvailable; }
        }

        //// private static bool InRealCloud { get { return !IsDevMachine && InFabric; } }
        internal static bool InDevFabric
        {
            get { return IsDevMachine && InFabric; }
        }

        private static string GetRegistryValue(string key)
        {
            var regKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft EMIC\Cloud\VENUS-C");
            if (regKey != null)
            {
                var regVal = regKey.GetValue(key) as string;
                if (!string.IsNullOrEmpty(regVal))
                    return regVal;
            }

            return null;
        }

        // Windows Registry Editor Version 5.00
        //
        // [HKEY_CURRENT_USER\Software\Microsoft EMIC\Cloud\VENUS-C]
        // "DomainUserName"="EUROPE\\chgeuer"
        // "DomainPassword"="PASSWORD"
        /// <summary>
        /// Sets the Windows identity of the IIS application pools in the local development fabric to a specified user identity. 
        /// </summary>
        /// <para>
        /// This is necessary to ensure that code running locally can access production storage, 
        /// when the local network is protected by a firewall such as TMG. 
        /// </para>
        [DebuggerNonUserCode]
        public static void TweakIdentityWhenRunningInCorpnet()
        {
            if (!InDevFabric)
            {
                Trace.TraceInformation(
                    "MicrosoftCorpnetAuthenticationFixer: Not running in development fabric, no fix necessary");
                return;
            }

            var appPoolUser = GetRegistryValue("DomainUserName");
            var appPoolPass = GetRegistryValue("DomainPassword");

            if (string.IsNullOrEmpty(appPoolUser) || string.IsNullOrEmpty(appPoolPass))
            {
                Trace.TraceInformation("MicrosoftCorpnetAuthenticationFixer: No credentials to fix");
                return;
            }

            Action<ServerManager> updateIdentity = (serverManager) =>
            {
                var sitename = RoleEnvironment.CurrentRoleInstance.Id + "_Web";
                var appPoolNames = serverManager.Sites[sitename].Applications.Select(app => app.ApplicationPoolName).ToList();

                foreach (var appPoolName in appPoolNames)
                {
                    var pool = serverManager.ApplicationPools[appPoolName];

                    pool.ProcessModel.IdentityType = ProcessModelIdentityType.SpecificUser;
                    pool.ProcessModel.UserName = appPoolUser;
                    pool.ProcessModel.Password = appPoolPass;
                }
                serverManager.CommitChanges();
            };

            ApplyServerManagerActions(updateIdentity);

            Trace.TraceInformation(string.Format("Instance {0} updated application pools", RoleEnvironment.CurrentRoleInstance.Id));
        }

        /// <summary>
        /// Applies the server manager actions specified in the input parameter
        /// </summary>
        /// <param name="serverManagerActions">The server manager actions.</param>
        [DebuggerNonUserCode]        
        public static void ApplyServerManagerActions(Action<ServerManager> serverManagerActions)
        {
            #region Barrier to have all instances wait for their peers to be at the same spot.

            Func<string, string> escapeMutexName = instanceId => instanceId.Replace("(", ".").Replace(")", ".").Replace(".", "");
            var currentMutexName = escapeMutexName(RoleEnvironment.CurrentRoleInstance.Id);
            var peerMutexNames = DevelopmentFabricIdentifiers.PeerRoles.Select(escapeMutexName);
            var cpb = CrossProcessBarrier.GetInstance(currentMutexName, peerMutexNames, TimeSpan.FromSeconds(3));
            cpb.Wait();

            Trace.TraceInformation(string.Format("Barrier passed at {0}", DateTime.UtcNow.ToLongTimeString()));

            #endregion

            #region One development fabric role instance at a time can modify app pool now

            // The global mutex ensures that only one instance at a time attempts to define appPool identities.  
            var mutex = new Mutex(initiallyOwned: false, name: typeof(MicrosoftCorpnetAuthenticationFixer).FullName);
            try
            {
                mutex.WaitOne();

                // ServerManager in %WinDir%\System32\InetSrv\Microsoft.Web.Administration.dll
                using (var sm = new ServerManager())
                {
                    serverManagerActions(sm);
                }
            }
            finally
            {
                mutex.ReleaseMutex();
            }

            #endregion
        }
    }
}
