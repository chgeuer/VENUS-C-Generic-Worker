//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Xml;
using System.Threading;
using Microsoft.EMIC.Cloud.DataManagement;

namespace Microsoft.EMIC.Cloud.Storage.Azure
{
    /// <summary>
    /// Azure argument catalogue reference class, implements IreferenceArgument
    /// </summary>
    [ArgumentExport(
        LocalName = LocalName,
        NamespaceURI = IArgumentExtensions.GenericWorkerNamespace,
        Type = typeof(AzureArgumentCatalogueReference))]
    public class AzureArgumentCatalogueReference : IReferenceArgument
    {
        internal const string LocalName = "AzureArgCatalogueReference";
        
        private ArgumentRepository argumentRepository;

        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        /// <value>
        /// The connection string.
        /// </value>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the name of the logical.
        /// </summary>
        /// <value>
        /// The name of the logical.
        /// </value>
        public string LogicalName { get; set; }

        #region IReferenceArgument Members

        private const string partitionKey = "MyPartition";

        /// <summary>
        /// Gets the cloud provider.
        /// </summary>
        public string CloudProvider { get { return "Azure"; } }

        private string fileLocalLocation = null;

        private IReferenceArgument singleReference = null;

        private IReferenceArgument GetSingleReference()
        {
            if (singleReference == null)
            {
                AzureCatalogueHandler handler = new AzureCatalogueHandler(this.ConnectionString, this.argumentRepository);
                singleReference = handler.Get(this.LogicalName);
            }
            return singleReference;
        }

        /// <summary>
        /// Gets the file location.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <returns></returns>
        public string GetFileLocation(string workingDirectory)
        {
            if (fileLocalLocation == null)
            {
                var sr = GetSingleReference();
                fileLocalLocation = sr.GetFileLocation(workingDirectory);
            }
            return fileLocalLocation;
        }

        /// <summary>
        /// Downloads the specified working directory.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="tokenSource">The token source.</param>
        public void Download(string workingDirectory, CancellationTokenSource tokenSource)
        {
            var sr = GetSingleReference();
            sr.Download(workingDirectory, tokenSource);
        }

        /// <summary>
        /// Uploads the specified working directory.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        public void Upload(string workingDirectory)
        {
            var sr = GetSingleReference();
            sr.Upload(workingDirectory);
        }

        /// <summary>
        /// Existses the data item.
        /// </summary>
        /// <returns></returns>
        public bool ExistsDataItem()
        {
            var sr = GetSingleReference();
            return sr.ExistsDataItem();
        }
        #endregion

        #region IArgument Members

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Loads the contents.
        /// </summary>
        /// <param name="xmlElement">The XML element.</param>
        /// <param name="argumentRepository">The argument repository.</param>
        public void LoadContents(XmlElement xmlElement, ArgumentRepository argumentRepository)
        {
            this.argumentRepository = argumentRepository;

            this.Name = xmlElement.Attributes["Name"].Value;
            this.ConnectionString = xmlElement.Attributes["ConnectionString"].Value;
            this.LogicalName = xmlElement.Attributes["LogicalName"].Value;  
        }

        /// <summary>
        /// Serializes the specified doc.
        /// </summary>
        /// <param name="doc">The doc.</param>
        /// <returns></returns>
        public XmlElement Serialize(XmlDocument doc)
        {
            var e = doc.CreateElement(LocalName, IArgumentExtensions.GenericWorkerNamespace);

            Action<string, string> add = (name, value) => { 
                var a = doc.CreateAttribute(name);
                a.Value = value;
                e.Attributes.Append(a);
            };

            add("Name", Name);
            add("ConnectionString", ConnectionString);
            add("LogicalName", LogicalName);

            return e;
        }

        #endregion
    }
}