//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Security.Cryptography;

namespace Microsoft.EMIC.Cloud.ApplicationRepository
{
    /// <summary>
    /// VENUS application description describes the proper call of an application,
    /// like the executable, and the available commandline arguments.
    /// </summary>
    [DataContract]
    public class VENUSApplicationDescription
    {
        /// <summary>
        /// Gets or sets the application identification URI.
        /// </summary>
        /// <value>
        /// The application identification URI.
        /// </value>
        [DataMember]
        public string ApplicationIdentificationURI { get; set; }
        
        /// <summary>
        /// Gets or sets the dependencies.
        /// </summary>
        /// <value>
        /// The dependencies.
        /// </value>
        [DataMember]
        public List<CloudApplicationDependencies> Dependencies { get; set; }

        /// <summary>
        /// Gets or sets the author.
        /// </summary>
        /// <value>
        /// The author.
        /// </value>
        [DataMember]
        public VENUSApplicationAuthor Author { get; set; }

        /// <summary>
        /// Gets or sets the command template.
        /// </summary>
        /// <value>
        /// The command template.
        /// </value>
        [DataMember]
        public VENUSCommandTemplate CommandTemplate { get; set; }

        private string _applicationID;
        /// <summary>
        /// Gets the application ID.
        /// </summary>
        public string ApplicationID
        {
            get
            {
                if (_applicationID == null)
                {
                    byte[] hash = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(this.ApplicationIdentificationURI));
                    var result = new StringBuilder();
                    foreach (var h in hash)
                        result.Append(string.Format("{0:x2}", h));

                    _applicationID = result.ToString();
                }

                return _applicationID;
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("Application metadata for application \"{0}\"", ApplicationIdentificationURI);
        }
    }
}