//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using Microsoft.EMIC.Cloud.UserAdministration;

namespace Microsoft.EMIC.Cloud.UserAdministration
{
    /// <summary>
    /// Create Profile client class, implements ClientBase ICreateProfile and ICreateProfile
    /// </summary>
    public class CreateProfileClient : ClientBase<ICreateProfile>, ICreateProfile
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateProfileClient"/> class.
        /// </summary>
        public CreateProfileClient() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateProfileClient"/> class.
        /// </summary>
        /// <param name="callbackInstance">The callback instance.</param>
        public CreateProfileClient(InstanceContext callbackInstance) : base(callbackInstance) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateProfileClient"/> class.
        /// </summary>
        /// <param name="endpointConfigurationName">Name of the endpoint configuration.</param>
        public CreateProfileClient(string endpointConfigurationName) : base(endpointConfigurationName) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateProfileClient"/> class.
        /// </summary>
        /// <param name="binding">The binding.</param>
        /// <param name="remoteAddress">The remote address.</param>
        public CreateProfileClient(Binding binding, EndpointAddress remoteAddress) : base(binding, remoteAddress) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateProfileClient"/> class.
        /// </summary>
        /// <param name="callbackInstance">The callback instance.</param>
        /// <param name="endpointConfigurationName">Name of the endpoint configuration.</param>
        public CreateProfileClient(InstanceContext callbackInstance, string endpointConfigurationName) : base(callbackInstance, endpointConfigurationName) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateProfileClient"/> class.
        /// </summary>
        /// <param name="endpointConfigurationName">Name of the endpoint configuration.</param>
        /// <param name="remoteAddress">The remote address.</param>
        public CreateProfileClient(string endpointConfigurationName, EndpointAddress remoteAddress) : base(endpointConfigurationName, remoteAddress) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateProfileClient"/> class.
        /// </summary>
        /// <param name="endpointConfigurationName">Name of the endpoint configuration.</param>
        /// <param name="remoteAddress">The remote address.</param>
        public CreateProfileClient(string endpointConfigurationName, string remoteAddress) : base(endpointConfigurationName, remoteAddress) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateProfileClient"/> class.
        /// </summary>
        /// <param name="callbackInstance">The callback instance.</param>
        /// <param name="binding">The binding.</param>
        /// <param name="remoteAddress">The remote address.</param>
        public CreateProfileClient(InstanceContext callbackInstance, Binding binding, EndpointAddress remoteAddress) : base(callbackInstance, binding, remoteAddress) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateProfileClient"/> class.
        /// </summary>
        /// <param name="callbackInstance">The callback instance.</param>
        /// <param name="endpointConfigurationName">Name of the endpoint configuration.</param>
        /// <param name="remoteAddress">The remote address.</param>
        public CreateProfileClient(InstanceContext callbackInstance, string endpointConfigurationName, EndpointAddress remoteAddress) : base(callbackInstance, endpointConfigurationName, remoteAddress) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateProfileClient"/> class.
        /// </summary>
        /// <param name="callbackInstance">The callback instance.</param>
        /// <param name="endpointConfigurationName">Name of the endpoint configuration.</param>
        /// <param name="remoteAddress">The remote address.</param>
        public CreateProfileClient(InstanceContext callbackInstance, string endpointConfigurationName, string remoteAddress) : base(callbackInstance, endpointConfigurationName, remoteAddress) { }

        #endregion

        #region ICreateProfile Members

        /// <summary>
        /// Gets the profile.
        /// </summary>
        /// <param name="nameClaim">The name claim.</param>
        /// <param name="parentDirectory">The parent directory.</param>
        /// <returns></returns>
        public ProfileData GetProfile(string nameClaim, string parentDirectory)
        {
            return this.Channel.GetProfile(nameClaim, parentDirectory);
        }

        /// <summary>
        /// Gets the owner.
        /// </summary>
        /// <param name="localUserName">Name of the local user.</param>
        /// <returns></returns>
        public string GetOwner(string localUserName)
        {
            return this.Channel.GetOwner(localUserName);
        }

        /// <summary>
        /// For unit testing only...
        /// </summary>
        public void ShutdownService()
        {
            this.Channel.ShutdownService();
        }

        #endregion

        /// <summary>
        /// Creates the client identity file for service.
        /// </summary>
        /// <param name="filename">The filename.</param>
        public static void CreateClientIdentityFileForService(string filename)
        {
            var fileInfo = new FileInfo(filename);
            if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
            {
                fileInfo.Directory.Create();
            }

            var userid = WindowsIdentity.GetCurrent().Name;
            var content = Encoding.UTF8.GetBytes(userid);
            var mutex = OpenMutex(typeof(CreateProfileClient).FullName.Replace(".", ""));
            try
            {
                mutex.WaitOne();
                using (var stream = File.OpenWrite(fileInfo.FullName))
                {
                    stream.Write(content, 0, content.Length);
                }
                Trace.TraceInformation(string.Format(
                    "Wrote process identity to \"{0}\"", fileInfo.FullName));
            }
            catch (IOException)
            {
                Trace.TraceInformation(string.Format(
                    "Failed writing process identity to \"{0}\"", fileInfo.FullName));
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        private static Mutex OpenMutex(string name)
        {
            try
            {
                return Mutex.OpenExisting(name);
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                try
                {
                    return new Mutex(false, name);
                }
                catch (WaitHandleCannotBeOpenedException)
                {
                    return Mutex.OpenExisting(name);
                }
            }
        }
        /// <summary>
        /// Reads the identity from file.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns></returns>
        public static string ReadIdentityFromFile(string filename)
        {
            var fileInfo = new FileInfo(filename);
            var ms = new MemoryStream();
            using (var stream = File.OpenRead(fileInfo.FullName))
            {
                stream.Pump(ms);
            }

            var userid = Encoding.UTF8.GetString(ms.ToArray());

            return userid;
        }

        /// <summary>
        /// Creates the client.
        /// </summary>
        /// <returns></returns>
        public static CreateProfileClient CreateClient()
        {
            return new CreateProfileClient(Settings.Binding,
                new EndpointAddress(new Uri(Settings.Address)));
        }

        /// <summary>
        /// Gets the local user.
        /// </summary>
        /// <param name="identity">The identity.</param>
        /// <param name="parentDirectory">The parent directory.</param>
        /// <returns></returns>
        public static ProfileData GetLocalUser(string identity, string parentDirectory)
        {
            var c = CreateClient();
            return c.GetProfile(identity, parentDirectory);
        }

        /// <summary>
        /// Gets the actual job owner.
        /// </summary>
        /// <param name="localUserName">Name of the local user.</param>
        /// <returns></returns>
        public static string GetActualJobOwner(string localUserName)
        {
            var c = CreateClient();
            return c.GetOwner(localUserName);
        }


        /// <summary>
        /// Shutdowns this instance.
        /// </summary>
        public static void Shutdown()
        {
            CreateClient().ShutdownService();
        }
    }
}