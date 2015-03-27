//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.Runtime.Serialization;

namespace Microsoft.EMIC.Cloud.ApplicationRepository
{
    /// <summary>
    /// Specifices the dependiencies of the application which will run on cloud
    /// </summary>
    [DataContract]
    public class CloudApplicationDependencies
    {
        // Operating system dependencies (Windows, Linux)
        // Environment dependencies (Azure storage)
        // Software prerequisites (Java JRE, Perl interprested, .NET 4.0, R, Mathematica)

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudApplicationDependencies"/> class.
        /// </summary>
        public CloudApplicationDependencies() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudApplicationDependencies"/> class.
        /// </summary>
        /// <param name="applicationName">Name of the application.</param>
        /// <param name="version">The version.</param>
        public CloudApplicationDependencies(string applicationName, string version)
        {
            this.ApplicationName = applicationName;
            this.Version = version;
        }

        /// <summary>
        /// Gets or sets the name of the application.
        /// </summary>
        /// <value>
        /// The name of the application.
        /// </value>
        [DataMember]
        public string ApplicationName { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>
        /// The version.
        /// </value>
        [DataMember]
        public string Version { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0} {1}", this.ApplicationName, this.Version);
        }
    }
}