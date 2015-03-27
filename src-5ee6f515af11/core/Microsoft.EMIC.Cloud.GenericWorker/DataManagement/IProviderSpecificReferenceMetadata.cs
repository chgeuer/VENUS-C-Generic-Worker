//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;

namespace Microsoft.EMIC.Cloud.DataManagement
{

    /// <summary>
    /// All IProviderSpecificReference implementations need to export the meta data defined by this interface.
    /// </summary>
    public interface IProviderSpecificReferenceMetadata
    {
        /// <summary>
        /// Gets the local name.
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