//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Xml;

namespace Microsoft.EMIC.Cloud.DataManagement
{

    /// <summary>
    /// This class represents a switch argument in the commandline
    /// </summary>
    [ArgumentExport(
      LocalName = LocalName,
      NamespaceURI = IArgumentExtensions.GenericWorkerNamespace,
      Type = typeof(SwitchArgument))]
    public class SwitchArgument: IArgument
    {
        internal const string LocalName = "ArgSwitch";

        /// <summary>
        /// Gets or sets a value
        /// </summary>
        public bool Value { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Loads the state the of the object out of an xml source.
        /// </summary>
        /// <param name="xmlElement">The XML source.</param>
        /// <param name="argumentRepository">The argument repository.</param>
        public void LoadContents(XmlElement xmlElement, ArgumentRepository argumentRepository)
        {
            Name = xmlElement.Attributes["Name"].Value;
            Value = Boolean.Parse(xmlElement.Attributes["Value"].Value);
        }

        /// <summary>
        /// Serializes the object to the specified document.
        /// </summary>
        /// <param name="doc">The document.</param>
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
            add("Value", Value.ToString());

            return e;
        }
    }
}