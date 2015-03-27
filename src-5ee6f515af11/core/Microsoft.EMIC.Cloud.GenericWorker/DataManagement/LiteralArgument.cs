//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.Xml;
using System;
using Microsoft.EMIC.Cloud.DataManagement;

namespace Microsoft.EMIC.Cloud.DataManagement
{
    /// <summary>
    /// The literal argument class is used for alphanumeric arguments
    /// </summary>
    [ArgumentExport(
      LocalName = LocalName,
      NamespaceURI = IArgumentExtensions.GenericWorkerNamespace,
      Type = typeof(LiteralArgument))]
    public class LiteralArgument : IArgument
    {
        internal const string LocalName = "LiteralArgument";

        /// <summary>
        /// The argument name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The literal value of the argument
        /// </summary>
        public string LiteralValue { get; set; }

        /// <summary>
        /// Deserializes the argument out of the given xml element.
        /// </summary>
        /// <param name="xmlElement">The XML element.</param>
        /// <param name="argumentRepository">The argument repository.</param>
        public void LoadContents(XmlElement xmlElement, ArgumentRepository argumentRepository)
        {
            Name = xmlElement.Attributes["Name"].Value;
            LiteralValue = xmlElement.Attributes["Value"].Value;
        }

        /// <summary>
        /// Serializes the argument to the specified xml document.
        /// </summary>
        /// <param name="doc">The xml document doc.</param>
        /// <returns></returns>
        public XmlElement Serialize(XmlDocument doc)
        {
            var e = doc.CreateElement(LocalName, IArgumentExtensions.GenericWorkerNamespace);

            Action<string, string> add = (name, value) =>
            {
                var a = doc.CreateAttribute(name);
                a.Value = value;
                e.Attributes.Append(a);
            };

            add("Name", Name);
            add("Value", LiteralValue);

            return e;
        }
    }
}