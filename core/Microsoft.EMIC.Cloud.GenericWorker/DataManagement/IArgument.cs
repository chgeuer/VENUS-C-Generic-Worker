//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.Xml;

namespace Microsoft.EMIC.Cloud.DataManagement
{
    /// <summary>
    /// Interface for arguments
    /// </summary>
    public interface IArgument
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Loads the contents out of the xml element.
        /// </summary>
        /// <param name="xmlElement">The XML element.</param>
        /// <param name="argumentRepository">The argument repository.</param>
        void LoadContents(XmlElement xmlElement, ArgumentRepository argumentRepository);

        /// <summary>
        /// Serializes the argument to the specified XML document.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <returns></returns>
        XmlElement Serialize(XmlDocument doc);
    }
}


