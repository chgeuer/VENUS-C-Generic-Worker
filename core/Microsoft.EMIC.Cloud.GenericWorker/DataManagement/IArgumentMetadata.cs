//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;

namespace Microsoft.EMIC.Cloud.DataManagement
{
    /// <summary>
    /// Interface for argument metadata
    /// </summary>
    public interface IArgumentMetadata
    {
        /// <summary>
        /// Gets the local name
        /// </summary>
        string LocalName { get; }

        /// <summary>
        /// Gets the namespace URI.
        /// </summary>
        string NamespaceURI { get; }

        /// <summary>
        /// Gets the type.
        /// </summary>
        Type Type { get; }
    }
}
