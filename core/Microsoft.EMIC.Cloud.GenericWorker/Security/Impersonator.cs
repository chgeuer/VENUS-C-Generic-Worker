//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Microsoft.EMIC.Cloud.Security
{
    /// <summary>
    /// An <see cref="System.IDisposable"/> which impersonates a given user account.  
    /// </summary>
    /// 
    /// 
    /// <example>
    /// The code impersonates a given user, executes code under the given identity and reverts back when leaving the using() statement.  
    /// 
    /// <code>
    /// var domain = "EUROPE";
    /// var username = "joe";
    /// var password = "secret123!";
    /// 
    /// using (new Impersonator(domain, username, password, impersonateImmediately: true))
    /// {
    ///     // This code is executed under the identity of the selected user 
    ///     Console.WriteLine("I'm now {0}", WindowsIdentity.GetCurrent().ToString()); 
    /// }
    /// 
    /// Console.WriteLine("I'm {0} again", WindowsIdentity.GetCurrent().ToString()); // This code is executed under the original user. 
    /// </code>
    /// </example>
    public class Impersonator : IDisposable
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool LogonUser(String lpszUsername, String lpszDomain, String lpszPassword,
            int dwLogonType, int dwLogonProvider, ref IntPtr phToken);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private extern static bool CloseHandle(IntPtr handle);

        private const int LOGON32_PROVIDER_DEFAULT = 0;

        /// <summary>
        /// This parameter causes LogonUser to create a primary token.
        /// </summary>
        private const int LOGON32_LOGON_INTERACTIVE = 2;

        private readonly IntPtr _tokenHandle;
        private readonly bool _success;
        private readonly WindowsIdentity _newId;
        private WindowsImpersonationContext _impersonatedUser;

        /// <summary>
        /// The domain of the impersonated user.
        /// </summary>
        public string Domain { get; private set; }

        /// <summary>
        /// The username of the impersonated user. 
        /// </summary>
        public string UserName { get; private set; }

        private Impersonator() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Impersonator"/> class.
        /// </summary>
        /// <param name="domain">The domain of the user. </param>
        /// <param name="userName">The username of the user.</param>
        /// <param name="password">The password of the user.</param>
        /// <param name="impersonateImmediately">if set to <c>true</c>, the <see cref="Impersonator"/> impersonates immediately. If set to <c>false</c>, you need to call <see cref="Impersonate()"/> yourself. </param>
        public Impersonator(string domain, string userName, string password, bool impersonateImmediately)
        {
            _tokenHandle = IntPtr.Zero; 

            _success = LogonUser(userName, domain, password,
                        LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT,
                        ref _tokenHandle);
            if (!_success)
                throw new NotSupportedException(ExceptionMessages.ImpersonationFailed);
            
            _newId = new WindowsIdentity(_tokenHandle);
            UserName = userName;
            Domain = domain;

            if (impersonateImmediately)
                Impersonate();
        }

        /// <summary>
        /// Impersonates the given user.
        /// </summary>
        public void Impersonate()
        {
            if (_alreadyDisposed)
                throw new ObjectDisposedException(typeof(Impersonator).Name, string.Format(ExceptionMessages.AlreadyDisposed));

            _impersonatedUser = _newId.Impersonate();
        }

        /// <summary>
        /// Undoes the impersonation.
        /// </summary>
        public void UndoImpersonation()
        {
            if (_alreadyDisposed)
                throw new ObjectDisposedException(typeof(Impersonator).Name, string.Format(ExceptionMessages.AlreadyDisposed));
            if (_alreadyUndoneImpersonation)
                throw new NotSupportedException(ExceptionMessages.UndoTwice);

            _impersonatedUser.Undo();
            _alreadyUndoneImpersonation = true;
        }

        #region IDisposable Members

        private bool _alreadyDisposed = false;

        private bool _alreadyUndoneImpersonation = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool isDisposing)
        {
            if (_alreadyDisposed)
                return;

            if (isDisposing)
            {
                // freeing managed resources
            }

            if (!_alreadyUndoneImpersonation)
            {
                this.UndoImpersonation();
            }

            // freeing unmanaged resources
            if (_tokenHandle != IntPtr.Zero)
                CloseHandle(_tokenHandle);

            _alreadyDisposed = true;
        }

        #endregion
    }
}
