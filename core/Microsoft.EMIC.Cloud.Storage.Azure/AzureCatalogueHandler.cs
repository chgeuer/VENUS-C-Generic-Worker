//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Linq;
using System.Xml;
using Microsoft.EMIC.Cloud.DataManagement;
using System.Collections.Generic;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.ComponentModel.Composition.Hosting;

namespace Microsoft.EMIC.Cloud.Storage.Azure
{
    /// <summary>
    /// Azure Catalogue Handler class, implements ICatalogueHandler
    /// </summary>
    public class AzureCatalogueHandler : ICatalogueHandler
    {
        private bool _isInitialized;
        private CloudTableClient _tableClient;
        private CatalogueTableDataContext _catalogueContext;
        private ArgumentRepository _argumentRepository;
        private const string PartitionKey = "MyPartition";
        private readonly CloudStorageAccount _account;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureCatalogueHandler"/> class.
        /// </summary>
        /// <param name="dataConnectionString">The data connection string.</param>
        public AzureCatalogueHandler(string dataConnectionString)
        {
            _account = CloudStorageAccount.Parse(dataConnectionString);

            var genericWorkerContainer = new CompositionContainer(new AggregateCatalog(
                new AssemblyCatalog(typeof(ArgumentRepository).Assembly),
                new AssemblyCatalog(typeof(AzureArgumentCatalogueReference).Assembly)
                ));
            var argumentRepository = genericWorkerContainer.GetExportedValue<ArgumentRepository>();
            Initialize(argumentRepository);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureCatalogueHandler"/> class.
        /// </summary>
        /// <param name="dataConnectionString">The data connection string.</param>
        /// <param name="argumentRepository">The argument repository.</param>
        public AzureCatalogueHandler(string dataConnectionString, ArgumentRepository argumentRepository)
        {
            _account = CloudStorageAccount.Parse(dataConnectionString);
            Initialize(argumentRepository);
        }

        #region ICatalogueHandler Members

        private void Initialize(ArgumentRepository argumentRepository)
        {
            //check first whether the table exist
            this._tableClient = _account.CreateCloudTableClient();

            this._tableClient.CreateTableIfNotExist(CatalogueTableDataContext.TableName);
            this._catalogueContext = new CatalogueTableDataContext(
                _account.TableEndpoint.AbsoluteUri, _account.Credentials);
            
            this._argumentRepository = argumentRepository;

            this._isInitialized = true;
        }

        /// <summary>
        /// Adds the specified logical name.
        /// </summary>
        /// <param name="logicalName">Name of the logical.</param>
        /// <param name="value">The value.</param>
        public void Add(string logicalName, IReferenceArgument value)
        {
            if (!_isInitialized)
            {
                throw new NotSupportedException(ExceptionMessages.AzureCatalogNotInitialized);
            }

            var val = value.Serialize(new XmlDocument()).OuterXml;

            _catalogueContext.AddCatalogueEntity(new CatalogueTableEntity(PartitionKey, logicalName)
            {
                Value = val
            });
        }

        /// <summary>
        /// Existses the specified logical name.
        /// </summary>
        /// <param name="logicalName">Name of the logical.</param>
        /// <returns></returns>
        public bool Exists(string logicalName)
        {
            return (_catalogueContext.GetCatalogueEntity(PartitionKey, logicalName) != null);
        }

        /// <summary>
        /// Gets the specified logical name.
        /// </summary>
        /// <param name="logicalName">Name of the logical.</param>
        /// <returns></returns>
        public IReferenceArgument Get(string logicalName)
        {
            if (!_isInitialized)
            {
                throw new NotSupportedException(ExceptionMessages.AzureCatalogNotInitialized);
            }

            var entity = _catalogueContext.GetCatalogueEntity(PartitionKey, logicalName);
            if (entity != null)
            {
                var doc = new XmlDocument();
                doc.LoadXml(entity.Value);

                return doc.ChildNodes.OfType<XmlElement>()
                            .Select(_argumentRepository.Load)
                            .FirstOrDefault() as IReferenceArgument;
            }

            return null;
        }

        /// <summary>
        /// Removes the specified logical name.
        /// </summary>
        /// <param name="logicalName">Name of the logical.</param>
        public void Remove(string logicalName)
        {
            var entity = _catalogueContext.GetCatalogueEntity(PartitionKey, logicalName);
            if (entity != null)
            {
                _catalogueContext.DeleteCatalogueEntity(entity);
            }
            else
            {
                throw new Exception(string.Format(
                    ExceptionMessages.ArgumentNotFound, 
                    PartitionKey, logicalName));
            }
        }

        /// <summary>
        /// Gets the logical names.
        /// </summary>
        /// <returns></returns>
        public List<string> GetLogicalNames() //GetRowKeys
        {
            return _catalogueContext.GetAllRowKeys(PartitionKey);
        }

        #endregion
    }
}