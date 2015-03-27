//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.Data.Services.Client;
using System.Linq;
using System.Collections.Generic;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Microsoft.EMIC.Cloud.Storage.Azure
{
    /// <summary>
    /// Catalogue table data context, inherits TableServiceContext
    /// </summary>
    public class CatalogueTableDataContext : TableServiceContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CatalogueTableDataContext"/> class.
        /// </summary>
        /// <param name="baseAddress">The Table service endpoint to use create the service context.</param>
        /// <param name="credentials">The account credentials.</param>
        public CatalogueTableDataContext(string baseAddress, StorageCredentials credentials) 
            : base(baseAddress, credentials)
        {
            //it is need to run GetCatalogueEntity method, otherwise it does not return null
            this.IgnoreResourceNotFoundException = true;
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly string TableName = "DataCatalogue";

        /// <summary>
        /// Gets the current entities.
        /// </summary>
        internal IQueryable<CatalogueTableEntity> CurrentEntities
        {
            get { return this.CreateQuery<CatalogueTableEntity>(TableName); }
        }

        /// <summary>
        /// Gets the catalogue entity.
        /// </summary>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="rowKey">The row key.</param>
        /// <returns></returns>
        internal CatalogueTableEntity GetCatalogueEntity(string partitionKey, string rowKey)
        {
            return CurrentEntities.Where(cat => cat.PartitionKey == partitionKey && cat.RowKey == rowKey).FirstOrDefault();
        }

        /// <summary>
        /// Gets all row keys.
        /// </summary>
        /// <param name="partitionKey">The partition key.</param>
        /// <returns></returns>
        internal List<string> GetAllRowKeys(string partitionKey)
        {
            return CurrentEntities.Where(cat => cat.PartitionKey == partitionKey).AsEnumerable().Select(cat => cat.RowKey).ToList();
        }

        internal void AddCatalogueEntity(CatalogueTableEntity newEntry)
        {
            var existingObject = GetCatalogueEntity(newEntry.PartitionKey,newEntry.RowKey);
                

            if (existingObject == null)
            {
                this.AddObject(TableName, newEntry);
            }
            else
            {
                existingObject.Value = newEntry.Value;

                this.UpdateObject(existingObject);
            }

            this.SaveChangesWithRetries(SaveChangesOptions.ReplaceOnUpdate);
        }

        internal void DeleteCatalogueEntity(CatalogueTableEntity existingObject)
        {
            if (existingObject != null)
            {
                this.DeleteObject(existingObject);
            }

            this.SaveChangesWithRetries(SaveChangesOptions.ReplaceOnUpdate);
        } 
    }
}
