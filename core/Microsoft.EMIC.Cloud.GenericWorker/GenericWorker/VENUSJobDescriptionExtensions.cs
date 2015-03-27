//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Microsoft.EMIC.Cloud.GenericWorker
{
    /// <summary>
    /// Extension methods for <see cref="VENUSJobDescription"/>. 
    /// </summary>
    public static class VENUSJobDescriptionExtensions
    {
        private static readonly XmlDocument Doc = new XmlDocument();

        private static XmlElement GetContainerElement(IEnumerable<XmlElement> any, string localName, string namespaceURI)
        {
            Func<XmlElement, bool> filter = e => e.LocalName.Equals(localName) && e.NamespaceURI.Equals(namespaceURI);
            var elem = any.Where(filter).FirstOrDefault();
            if (elem == null || elem.ChildNodes.Count != 1 || elem.FirstChild.NodeType != XmlNodeType.Text) { return null; }
            return elem;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="any">Any.</param>
        /// <param name="localName">Name of the local.</param>
        /// <param name="namespaceURI">The namespace URI.</param>
        /// <returns></returns>
        internal static string GetValue(this List<XmlElement> any, string localName, string namespaceURI)
        {
            var elem = GetContainerElement(any, localName, namespaceURI);
            return elem == null ? null : elem.FirstChild.Value;
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="any">Any.</param>
        /// <param name="qualifiedName">Name of the qualified.</param>
        /// <param name="namespaceURI">The namespace URI.</param>
        /// <param name="value">The value.</param>
        internal static void SetValue(this List<XmlElement> any, string qualifiedName, string namespaceURI, string value)
        {
            var e = Doc.CreateElement(qualifiedName, namespaceURI);
            e.AppendChild(Doc.CreateTextNode(value));

            var e2 = GetContainerElement(any, e.LocalName, e.NamespaceURI);
            if (e2 != null)
            {
                any.Remove(e2);
            }

            any.Add(e);
        }

        private static XmlAttribute GetContainerAttribute(IEnumerable<XmlAttribute> anyAttr, string localName, string namespaceURI)
        {
            Func<XmlAttribute, bool> filter = e => e.LocalName.Equals(localName) && e.NamespaceURI.Equals(namespaceURI);
            var attr = anyAttr.Where(filter).FirstOrDefault();
            return attr;
        }

        /// <summary>
        /// Gets the attribute value.
        /// </summary>
        /// <param name="anyAttr">Any attr.</param>
        /// <param name="localName">Name of the local.</param>
        /// <param name="namespaceURI">The namespace URI.</param>
        /// <returns></returns>
        internal static string GetAttributeValue(this List<XmlAttribute> anyAttr, string localName, string namespaceURI)
        {
            if (anyAttr == null) { throw new ArgumentNullException("anyAttr"); }
            var attr = GetContainerAttribute(anyAttr, localName, namespaceURI);
            return attr.Value;
        }

        /// <summary>
        /// Sets the attribute value.
        /// </summary>
        /// <param name="anyAttr">Any attr.</param>
        /// <param name="qualifiedName">Name of the qualified.</param>
        /// <param name="namespaceURI">The namespace URI.</param>
        /// <param name="value">The value.</param>
        internal static void SetAttributeValue(this List<XmlAttribute> anyAttr, string qualifiedName, string namespaceURI, string value)
        {
            if (anyAttr == null) { throw new ArgumentNullException("anyAttr"); }
            var a = Doc.CreateAttribute(qualifiedName, namespaceURI);
            a.Value = value;
            anyAttr.Add(a);
        }
    }
}