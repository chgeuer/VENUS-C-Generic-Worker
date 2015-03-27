//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





using System;
using System.ComponentModel.Composition;

namespace Microsoft.EMIC.Cloud.DataManagement
{
    /// <summary>
    /// This class provides a export attribute for argument metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    [MetadataAttribute]
    public class ArgumentExportAttribute : ExportAttribute, IArgumentMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ArgumentExportAttribute"/> class.
        /// </summary>
        public ArgumentExportAttribute()
            : base(typeof(IArgument)) { }

        /// <summary>
        /// Gets the local name
        /// </summary>
        public string LocalName { get; set; }

        /// <summary>
        /// Gets the namespace URI.
        /// </summary>
        public string NamespaceURI { get; set; }

        /// <summary>
        /// Gets the type.
        /// </summary>
        public Type Type { get; set; }
    }

    /// <summary>
    /// This class provides a specific export attribute for provider specific reference metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    [MetadataAttribute]
    public class ProviderSpecificArgumentExportAttribute : ExportAttribute, IProviderSpecificReferenceMetadata
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="ProviderSpecificArgumentExportAttribute"/> class.
        /// </summary>
        public ProviderSpecificArgumentExportAttribute()
            : base(typeof(IProviderSpecificReference)) { }

        /// <summary>
        /// Gets the local name.
        /// </summary>
        public string LocalName { get; set; }

        /// <summary>
        /// Gets the namespace URI.
        /// </summary>
        public string NamespaceURI { get; set; }

        /// <summary>
        /// Gets the type.
        /// </summary>
        public Type Type { get; set; }
    }
}
