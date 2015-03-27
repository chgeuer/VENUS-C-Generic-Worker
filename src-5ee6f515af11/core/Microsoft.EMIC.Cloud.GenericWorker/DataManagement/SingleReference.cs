//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Threading;
using System.Xml;
using Microsoft.EMIC.Cloud.DataManagement;

namespace Microsoft.EMIC.Cloud.DataManagement
{
    /// <summary>
    /// This class represents a reference to a single input/output file.
    /// </summary>
    [ArgumentExport(
    LocalName = LocalName,
    NamespaceURI = IArgumentExtensions.GenericWorkerNamespace,
    Type = typeof(SingleReference))]
    public class SingleReference : IReferenceArgument
    {
        internal const string LocalName = "SingleReference";
        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the reference
        /// </summary>
        /// <value>
        /// The reference
        /// </value>
        public Reference Ref { get; set; }

        ArgumentRepository argumentRepository;

        /// <summary>
        /// Existses the data item.
        /// </summary>
        /// <returns></returns>
        public bool ExistsDataItem()
        {
            return Ref.ExistsDataItem();
        }
        /// <summary>
        /// Downloads the file to the specified working directory.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="tokenSource">The token source.</param>
        public void Download(string workingDirectory, CancellationTokenSource tokenSource)
        {
            Ref.Download(workingDirectory, tokenSource);
        }

        /// <summary>
        /// Uploads the file from the specified working directory.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        public void Upload(string workingDirectory)
        {
            Ref.Upload(workingDirectory);
        }

        /// <summary>
        /// Loads the state the of the object out of an xml source.
        /// </summary>
        /// <param name="xmlElement">The XML source.</param>
        /// <param name="argumentRepository">The argument repository.</param>
        public void LoadContents(XmlElement xmlElement, ArgumentRepository argumentRepository)
        {
            this.argumentRepository = argumentRepository;

            Name = xmlElement.Attributes["Name"].Value;
            XmlElement childElement = (XmlElement)xmlElement.GetElementsByTagName("Reference").Item(0);
            if (childElement == null)
                throw new Exception(ExceptionMessages.SingleReferenceChild);
            Ref = new Reference();
            Ref.LoadContents(childElement);
        }

        /// <summary>
        /// Serializes the object to the specified document.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <returns></returns>
        public XmlElement Serialize(XmlDocument doc)
        {
            var e = doc.CreateElement(LocalName,
                IArgumentExtensions.GenericWorkerNamespace);

            Action<string, string> add = (name, value) =>
            {
                var a = doc.CreateAttribute(name);
                a.Value = value;
                e.Attributes.Append(a);
            };

            add("Name", Name);
            var RefEl = Ref.Serialize(doc);
            e.AppendChild(RefEl);
            return e;
        }

        /// <summary>
        /// Downloads the contents.
        /// </summary>
        /// <returns></returns>
        public byte[] DownloadContents()
        {
            return Ref.DownloadContents();
        }

        /// <summary>
        /// Get a file location in the specified working directory.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <returns></returns>
        public string GetFileLocation(string workingDirectory)
        {
            return Ref.GetFileLocation(workingDirectory);
        }
    }

}
