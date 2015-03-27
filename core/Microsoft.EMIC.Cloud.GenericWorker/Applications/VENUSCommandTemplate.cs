//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.EMIC.Cloud.ApplicationRepository
{
    /// <summary>
    /// VENUS command template contains all available arguments of an 
    /// application, the name of the executable and the working directory
    /// </summary>
    [DataContract]
    public class VENUSCommandTemplate
    {
        /// <summary>
        /// The package-relative path to an executable, such as ./bin/foo.exe
        /// </summary>
        [DataMember]
        public string Executable { get; set; }

        /// <summary>
        /// Gets or sets the commandline arguments
        /// </summary>
        /// <value>
        /// A list of all commandline arguments.
        /// </value>
        [DataMember]
        public List<CommandLineArgument> Args { get; set; }

        /// <summary>
        /// Gets or sets the working directory.
        /// </summary>
        [DataMember]
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// The 'Path' might have to be extended to include additional folders from the application, e.g. "bin;lib;thirdparty\\lib"
        /// </summary>
        [DataMember]
        public string Path { get; set; }
    }
}