//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.Runtime.Serialization;

namespace Microsoft.EMIC.Cloud.ApplicationRepository
{
    /// <summary>
    /// Enum for all commandline argument types
    /// </summary>
    [DataContract]
    public enum CommandLineArgType
    {
        /// <summary>
        /// Represents a literal value that is passed as it is to the command line, such as a parameter. Could be "-Temperature 3" or so...
        /// </summary>
        [EnumMember]
        SingleLiteralArgument = 0,

        /// <summary>
        /// A command line switch is either there, or not. The value would be a boolean about absence or presence. 
        /// </summary>
        [EnumMember]
        Switch = 1,

        /// <summary>
        /// Refers to a single data file in blob store or so...
        /// </summary>
        [EnumMember]
        SingleReferenceInputArgument = 2,

        /// <summary>
        /// Refers to a single output data file in blob store or so...
        /// </summary>
        [EnumMember]
        SingleReferenceOutputArgument = 3,

         /// <summary>
        /// Refers to a single  input in catalogue...
        /// </summary>
        [EnumMember]
        CatalogueReferenceInputArgument = 4,

        /// <summary>
        /// Refers to a single  output in catalogue...
        /// </summary>
        [EnumMember]
        CatalogueReferenceOutputArgument = 5,

        /// <summary>
        /// Refers to a list of input data files in blob store or so...
        /// </summary>
        [EnumMember]
        MultipleReferenceInputArgument = 6,

        /// <summary>
        /// Refers to a list of output data files in blob store or so...
        /// </summary>
        [EnumMember]
        MultipleReferenceOutputArgument = 7
    }
}