//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Threading;
using System.Xml;
using Microsoft.EMIC.Cloud.DataManagement;

namespace Microsoft.EMIC.Cloud.Storage.Azure
{
    /// <summary>
    /// A reference to a single file in Windows Azure blob storage. 
    /// </summary>
    [ArgumentExport(
        LocalName = LocalName,
        NamespaceURI = IArgumentExtensions.GenericWorkerNamespace,
        Type = typeof(AzureArgumentSingleReference))]
    public class AzureArgumentSingleReference : IReferenceArgument
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureArgumentSingleReference"/> class.
        /// </summary>
        public AzureArgumentSingleReference() { }

        internal const string LocalName = "AzureArgSingleReference";

        // AzureBlobReference azureBlobRefArg;
        SingleReference SingleRef { get; set; }

        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        /// <value>
        /// The connection string.
        /// </value>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the data address.
        /// </summary>
        /// <value>
        /// The data address.
        /// </value>
        public string DataAddress { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string LocalFileName = "";

        ArgumentRepository argumentRepository;

        #region IReferenceArgument Members

        /// <summary>
        /// Gets the cloud provider.
        /// </summary>
        public string CloudProvider { get { return "Azure"; } }

        /// <summary>
        /// Existses the data item.
        /// </summary>
        /// <returns></returns>
        public bool ExistsDataItem()
        {
            return SingleRef.ExistsDataItem();
        }

        /// <summary>
        /// Downloads the specified working directory.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="token">The token.</param>
        public void Download(string workingDirectory, CancellationTokenSource token)
        {
            SingleRef.Download(workingDirectory, token);
        }

        /// <summary>
        /// Uploads the specified working directory.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        public void Upload(string workingDirectory)
        {
            SingleRef.Upload(workingDirectory);
        }

        /// <summary>
        /// Gets the file location.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <returns></returns>
        public string GetFileLocation(string workingDirectory)
        {
            return SingleRef.GetFileLocation(workingDirectory);
        }

        #endregion

        #region IArgument Members

        /// <summary>
        /// Downloads the contents.
        /// </summary>
        /// <returns></returns>
        public byte[] DownloadContents()
        {
            return SingleRef.DownloadContents();
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Loads the contents.
        /// </summary>
        /// <param name="xmlElement">The XML element.</param>
        /// <param name="argumentRepository">The argument repository.</param>
        public void LoadContents(XmlElement xmlElement, ArgumentRepository argumentRepository) //is not used anymore: existing apps making use of AzureArgumentSingleReference need to be reinstalled
        {
            this.argumentRepository = argumentRepository;

            if (!string.Equals(this.CloudProvider, 
                xmlElement.Attributes["CloudProvider"].Value))
            {
                throw new ArgumentException(ExceptionMessages.NotAzure);
            }

            Name = xmlElement.Attributes["Name"].Value;
            ConnectionString = xmlElement.Attributes["ConnectionString"].Value;
            DataAddress = xmlElement.Attributes["DataAddress"].Value;  
        }

        /// <summary>
        /// Serializes the specified doc.
        /// </summary>
        /// <param name="doc">The doc.</param>
        /// <returns></returns>
        public XmlElement Serialize(XmlDocument doc)
        {
            var azureBlobRef = new AzureBlobReference
            {
                DataAddress = this.DataAddress,
                ConnectionString = this.ConnectionString
            };
            SingleRef = new SingleReference
            {
                Name = this.Name,
                Ref = new Reference(this.LocalFileName, azureBlobRef)
            };
            var e = SingleRef.Serialize(doc);
            return e;
        }

        #endregion
    }
}
