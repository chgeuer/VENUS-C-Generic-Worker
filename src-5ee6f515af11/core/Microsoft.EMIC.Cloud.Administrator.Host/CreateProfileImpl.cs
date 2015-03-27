//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.IO;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Linq;
using Microsoft.EMIC.Cloud.UserAdministration;

namespace Microsoft.EMIC.Cloud.Administrator.Host
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class CreateProfileImpl : ICreateProfile
    {
        public CreateProfileImpl(string gwUserAccountFile, CancellationTokenSource cts)
        {
            this.GenericWorkerUserAccountFile = gwUserAccountFile;
            this._cts = cts;
            (new Random()).NextBytes(_secret);
        }

        public string GenericWorkerUserAccountFile { get; private set; }

        private readonly CancellationTokenSource _cts;
        private string _accountName;
        private readonly object _locker = new object();
        private readonly ConcurrentDictionary<string, ProfileData> _dict = new ConcurrentDictionary<string, ProfileData>();
        private readonly byte[] _secret = new byte[64];
        private int _userCounter = 0;
        readonly object _lockObject = new object();
        
        public string GenericWorkerAccountName
        {
            get
            {
                if (_accountName == null)
                {
                    lock (_locker)
                    {
                        if (_accountName == null)
                        {
                            _accountName = CreateProfileClient.ReadIdentityFromFile(GenericWorkerUserAccountFile);
                        }
                    }
                }

                return _accountName;
            }
        }

        private void EnsureAuthorizedCaller()
        {
            if (!this.GenericWorkerAccountName.Equals(ServiceSecurityContext.Current.WindowsIdentity.Name))
            {
                throw new NotSupportedException(string.Format(ExceptionMessages.IllegalCaller,
                    ServiceSecurityContext.Current.WindowsIdentity.Name, this.GenericWorkerAccountName));
            }
        }

        public void ShutdownService()
        {
            EnsureAuthorizedCaller();

            // Shutdown the outer host
            _cts.Cancel();
        }

        public ProfileData GetProfile(string nameClaim, string parentDirectory)
        {
            EnsureAuthorizedCaller();

            if (!_dict.ContainsKey(nameClaim))
            {
                lock (_lockObject)
                {
                    if (!_dict.ContainsKey(nameClaim))
                    {
                        string userName = string.Format("gw{0:d6}", ++_userCounter);

                        var hmac = HMACSHA256.Create();
                        hmac.Key = _secret;
                        string password = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(userName))).Substring(0, 12) + "123.-";

                        string userFolder = new DirectoryInfo(Path.Combine(parentDirectory, userName)).FullName;

                        var profile = new ProfileData(Environment.MachineName, userName, password, userFolder);

                        CreateUser(userName, password, nameClaim, userFolder);
                        CreateDirectoryAndSetPrivileges(profile);

                        _dict[nameClaim] = profile;
                    }
                }
            }

            var usr = _dict[nameClaim];

            return usr;
        }

        public string GetOwner(string localUserName)
        {
            EnsureAuthorizedCaller();

            var profile = _dict.Values.Where(pr => pr.UserName == localUserName).FirstOrDefault();

            if (profile != null)
            {
                return profile.UserName;
            }

            return null;
        }


        private static void CreateUser(string username, string password, string nameClaim, string homeDirectory)
        {
            var adRoot = new DirectoryEntry("WinNT://" + Environment.MachineName + ",computer");

            DirectoryEntry newUser = null;
            try
            {
                newUser = adRoot.Children.Find(username, "user");
            }
            catch (Exception) { }

            var description = string.Format("Local Generic Worker account for {0}", nameClaim);
            if (newUser == null)
            {
                newUser = adRoot.Children.Add(username, "user");
                newUser.Invoke("Put", new object[] { "Description", description });
                newUser.Invoke("Put", new object[] { "HomeDirectory", homeDirectory });
                newUser.Invoke("Put", new object[] { "Profile", homeDirectory });
            }
            newUser.Invoke("SetPassword", password);
            newUser.CommitChanges();
            
            DirectoryEntry usersGroup = adRoot.Children.Find(UsersGroupName, "group");

            //Get the Users Group information using WMI
            if (usersGroup != null)
            {
                if (!((bool)usersGroup.Invoke("IsMember", new object[] { newUser.Path })))
                {
                    usersGroup.Invoke("Add", new object[] { newUser.Path });
                }
            }

            Trace.TraceInformation(string.Format("Created user {0}", username));
            newUser.Close();
        }

        private static string UsersGroupName
        {
            get
            {
                //NOT OPTIMAL BUT A lazy solution for localization problem
                var usersGroup = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null); // "S-1-5-32-545" "BUILTIN\Users", 
                string usersGroupName = usersGroup.Translate(typeof(NTAccount)).Value;
                var result = usersGroupName.Split(new[] { '\\' })[1];
                return result;
            }
        }

        private void CreateDirectoryAndSetPrivileges(ProfileData profile)
        {
            var dir = new DirectoryInfo(profile.HomeDirectory);

            if (dir.Exists)
                return;

            dir.Create();
            DirectorySecurity ds = dir.GetAccessControl();

            ds.SetAccessRuleProtection(
                isProtected: true,
                preserveInheritance: false);

            var usersWithFullControl = new List<string>
                                           { 
                @"BUILTIN\Administrators", 
                @"SYSTEM", 
                GenericWorkerAccountName, 
                new NTAccount(profile.Domain, profile.UserName).Value 
            };

            usersWithFullControl.ForEach(user => ds.AddAccessRule(
                new FileSystemAccessRule(user,
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow)));

            dir.SetAccessControl(ds);
        }
    }
}