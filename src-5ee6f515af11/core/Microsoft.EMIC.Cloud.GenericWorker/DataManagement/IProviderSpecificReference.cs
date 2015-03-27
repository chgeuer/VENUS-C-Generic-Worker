//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.Threading;
using System.Xml;
using System;

namespace Microsoft.EMIC.Cloud.DataManagement
{    
    /// <summary>
    /// Common interface for al provider specific references ( AzureBlobReference, CDMIBlobReference, HttpGetReference ... ).
    /// </summary>
     public interface IProviderSpecificReference
    {
        /// <summary>
        /// Downloads the file to the specified working directory.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="tokenSource">The cancellation token.</param>
        void Download(string workingDirectory, CancellationTokenSource tokenSource);

        /// <summary>
        /// Uploads the file from the specified working directory.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        void Upload(string workingDirectory);

        /// <summary>
        /// Checks whether the data item exists.
        /// </summary>
        /// <returns></returns>
        bool ExistsDataItem();

        /// <summary>
        /// Deserializes the content out of the XML element.
        /// </summary>
        /// <param name="xmlElement">The XML element.</param>
        void LoadContents(XmlElement xmlElement);

        /// <summary>
        /// Serializes the content to the specified XML document.
        /// </summary>
        /// <param name="doc">The xml document</param>
        /// <returns></returns>
        XmlElement Serialize(XmlDocument doc);

        /// <summary>
        /// Gets a file location in the specified working directory.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <returns></returns>
        string GetFileLocation(string workingDirectory);

        /// <summary>
        /// Sets the file location.
        /// </summary>
        /// <value>
        /// The file location.
        /// </value>
        string FileLocation { set; }

        /// <summary>
        /// Downloads the content of the file.
        /// </summary>
        /// <returns></returns>
        byte[] DownloadContents();

        /// <summary>
        /// Gets the last modification date time of the referenced blob.
        /// </summary>
        /// <returns></returns>
        DateTime GetLastModificationDateTime();

     }
}
