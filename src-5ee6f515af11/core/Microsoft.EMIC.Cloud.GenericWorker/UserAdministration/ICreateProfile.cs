//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.ServiceModel;

namespace Microsoft.EMIC.Cloud.UserAdministration
{
    /// <summary>
    /// Interface for creating profiles
    /// </summary>
    [ServiceContract]
    public interface ICreateProfile
    {
        /// <summary>
        /// Gets the profile.
        /// </summary>
        /// <param name="nameClaim">The name claim.</param>
        /// <param name="parentDirectory">The parent directory.</param>
        /// <returns></returns>
        [OperationContract]
        ProfileData GetProfile(string nameClaim, string parentDirectory);

        /// <summary>
        /// Gets the owner.
        /// </summary>
        /// <param name="localUserName">Name of the local user.</param>
        /// <returns></returns>
        [OperationContract]
        string GetOwner(string localUserName);

        /// <summary>
        /// For unit testing only...
        /// </summary>
        [OperationContract]
        void ShutdownService();
    }
}
