//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.Runtime.Serialization;

namespace Microsoft.EMIC.Cloud.ApplicationRepository
{
    /// <summary>
    /// Enum Required: 0 = Optional, 1 = Mandatory
    /// </summary>
    [DataContract]
    public enum Required
    {
        /// <summary>
        /// Optional
        /// </summary>
        [EnumMember]
        Optional = 0,

        /// <summary>
        /// Mandatory
        /// </summary>
        [EnumMember]
        Mandatory = 1
    }
}
