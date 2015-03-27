//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.Runtime.Serialization;

namespace Microsoft.EMIC.Cloud.ApplicationRepository
{
    /// <summary>
    /// Represents an commandline argument, whether it is required, how it is called, 
    /// and what kind of commandline argument it is (Switch, Input, Ouput, etc.)
    /// </summary>
    [DataContract]
    public class CommandLineArgument
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the required.
        /// </summary>
        /// <value>
        /// The required.
        /// </value>
        [DataMember]
        public Required Required { get; set; }

        /// <summary>
        /// Gets or sets the type of the command line arg.
        /// </summary>
        /// <value>
        /// The type of the command line arg.
        /// </value>
        [DataMember]
        public CommandLineArgType CommandLineArgType { get; set; }

        /// <summary>
        /// Gets or sets the format string.
        /// </summary>
        /// <value>
        /// The format string.
        /// </value>
        [DataMember]
        public string FormatString { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0} ({1})", this.Name, this.Required.ToString());
        }
    }
}
