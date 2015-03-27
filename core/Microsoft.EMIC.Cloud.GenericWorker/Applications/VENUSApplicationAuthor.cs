//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.Runtime.Serialization;

namespace Microsoft.EMIC.Cloud.ApplicationRepository
{
    /// <summary>
    /// The VENUS Application author represents the author of the application
    /// </summary>
    [DataContract]
    public class VENUSApplicationAuthor
    {
        /// <summary>
        /// Gets or sets the E mail.
        /// </summary>
        /// <value>
        /// The E mail.
        /// </value>
        [DataMember]
        public string EMail { get; set; }

        /// <summary>
        /// Gets or sets the company.
        /// </summary>
        /// <value>
        /// The company.
        /// </value>
        [DataMember]
        public string Company { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("Author: {0} working for {1}", this.EMail, this.Company);
        }
    }
}
