//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using Microsoft.WindowsAzure.StorageClient;

namespace Microsoft.EMIC.Cloud.Storage.Azure
{
    /// <summary>
    /// Catalogue table entity class, inherits TableServiceEntity
    /// </summary>
    public class CatalogueTableEntity : TableServiceEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CatalogueTableEntity"/> class.
        /// </summary>
        public CatalogueTableEntity() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CatalogueTableEntity"/> class.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="keyword">The keyword.</param>
        public CatalogueTableEntity(string owner, string keyword)
            : base(owner, keyword) { }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("User \"{0}\", Internal Job ID \"{1}\"", this.PartitionKey, this.RowKey);
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public string Value { get; set; }
    }
}
