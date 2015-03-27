//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.Runtime.Serialization;

namespace Microsoft.EMIC.Cloud.UserAdministration
{
    /// <summary>
    /// Profile data class
    /// </summary>
    [DataContract]
    public class ProfileData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileData"/> class.
        /// </summary>
        public ProfileData() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileData"/> class.
        /// </summary>
        /// <param name="domain">The domain.</param>
        /// <param name="userName">Name of the user.</param>
        /// <param name="password">The password.</param>
        /// <param name="homeDirectory">The home directory.</param>
        public ProfileData(string domain, string userName, string password, string homeDirectory)
        {
            this.Domain = domain;
            this.UserName = userName;
            this.Password = password;
            this.HomeDirectory = homeDirectory;
        }

        /// <summary>
        /// Gets or sets the domain.
        /// </summary>
        /// <value>
        /// The domain.
        /// </value>
        [DataMember]
        public string Domain { get; set; }

        /// <summary>
        /// Gets or sets the name of the user.
        /// </summary>
        /// <value>
        /// The name of the user.
        /// </value>
        [DataMember]
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        /// <value>
        /// The password.
        /// </value>
        [DataMember]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the home directory.
        /// </summary>
        /// <value>
        /// The home directory.
        /// </value>
        [DataMember]
        public string HomeDirectory { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("Username and password for {0}\\{1}", Domain, UserName);
        }
    }
}