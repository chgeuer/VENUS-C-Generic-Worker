//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading;
using System.IO;

namespace Microsoft.EMIC.Cloud.DataManagement
{
    /// <summary>
    /// A wrapper around a provider specific reference type.
    /// </summary>
    public class Reference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Reference"/> class.
        /// </summary>
        public Reference() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Reference"/> class.
        /// </summary>
        /// <param name="localFileName">Name of the local file.</param>
        /// <param name="providerReference">The provider specific reference.</param>
        public Reference(string localFileName, IProviderSpecificReference providerReference) 
        {
            this.ProviderSpecificReference = providerReference;
            this.LocalFileName = localFileName;            
            this.ProviderSpecificReference.FileLocation = localFileName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Reference"/> class.
        /// </summary>
        /// <param name="providerReference">The provider specific reference.</param>
        public Reference(IProviderSpecificReference providerReference)
        {
            this.ProviderSpecificReference = providerReference;
        }
        private string localFileName;

        /// <summary>
        /// Gets or sets the name of the local file.
        /// </summary>
        /// <value>
        /// The name of the local file.
        /// </value>
        public string LocalFileName 
        {
            get { return localFileName; }
            private set { localFileName = value; if (this.ProviderSpecificReference!=null) this.ProviderSpecificReference.FileLocation = value; } 
        }

        /// <summary>
        /// Gets or sets the provider specific reference.
        /// </summary>
        /// <value>
        /// The provider specific reference.
        /// </value>
        public IProviderSpecificReference ProviderSpecificReference { get; set; }

        /// <summary>
        /// Checks whether the data item exists.
        /// </summary>
        /// <returns></returns>
        public bool ExistsDataItem()
        {
            return ProviderSpecificReference.ExistsDataItem();
        }

        /// <summary>
        /// Downloads the file to the specified working directory.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="tokenSource">The token source.</param>
        public void Download(string workingDirectory, CancellationTokenSource tokenSource)
        {
            var fileLoc = ProviderSpecificReference.GetFileLocation(workingDirectory);
            var fileDir = new FileInfo(fileLoc).DirectoryName;
            Directory.CreateDirectory(fileDir);
            ProviderSpecificReference.Download(workingDirectory, tokenSource);
        }

        /// <summary>
        /// Uploads the file from the specified working directory.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        public void Upload(string workingDirectory)
        {
            var fileLoc = ProviderSpecificReference.GetFileLocation(workingDirectory);
            if (!File.Exists(fileLoc))
            {
                throw new FileNotFoundException(string.Format(ExceptionMessages.FileDoesNotExist, fileLoc));
            }
            ProviderSpecificReference.Upload(workingDirectory);
        }

        /// <summary>
        /// Downloads the file content.
        /// </summary>
        /// <returns></returns>
        public byte[] DownloadContents()
        {
            return ProviderSpecificReference.DownloadContents();
        }

        /// <summary>
        /// Loads the state the of the object out of an xml source.
        /// </summary>
        /// <param name="xmlElement">The XML source.</param>
        public void LoadContents(XmlElement xmlElement)
        {
            if (xmlElement.HasAttribute("LocalFileName"))
                LocalFileName = xmlElement.Attributes["LocalFileName"].Value;
            else
                LocalFileName = "";

            XmlElement childElement = (XmlElement)xmlElement.FirstChild;
            if (childElement == null)
                throw new Exception(ExceptionMessages.ProviderSpecificReference);
        }

        /// <summary>
        /// Serializes the object to the specified document.
        /// </summary>
        /// <param name="doc">The document</param>
        /// <returns></returns>
        public XmlElement Serialize(XmlDocument doc)
        {
            var e = doc.CreateElement("Reference",
                IArgumentExtensions.GenericWorkerNamespace);

            Action<string, string> add = (name, value) =>
            {
                var a = doc.CreateAttribute(name);
                a.Value = value;
                e.Attributes.Append(a);
            };
            if (!string.IsNullOrEmpty(this.LocalFileName))
            {
                add("LocalFileName", LocalFileName);
            }
            var ProviderSpecificRefElement = ProviderSpecificReference.Serialize(doc);
            e.AppendChild(ProviderSpecificRefElement);

            return e;
        }

        /// <summary>
        /// Returns a file location in the specified working directory.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <returns></returns>
        public string GetFileLocation(string workingDirectory)
        {
            if (!string.IsNullOrWhiteSpace(LocalFileName))
            {
                this.ProviderSpecificReference.FileLocation = LocalFileName;
            }
            return ProviderSpecificReference.GetFileLocation(workingDirectory);
        }
    }
}
