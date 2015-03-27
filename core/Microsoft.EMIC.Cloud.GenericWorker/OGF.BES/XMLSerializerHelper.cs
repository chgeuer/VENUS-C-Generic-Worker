//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Xml.Serialization;

namespace OGF.BES
{
    /// <summary>
    /// Helper class for XML Serialization
    /// </summary>
    public class XMLSerializerHelper
    {
        /// <summary>
        /// Serialize given object into XmlElement.
        /// </summary>
        /// <param name="transformObject">Input object for serialization.</param>
        /// <returns>Returns serialized XmlElement.</returns>
        #region Serialize given object into stream.
        public static XmlElement Serialize(object transformObject)
        {
            XmlElement serializedElement = null;
            try
            {
                MemoryStream memStream = new MemoryStream();
                XmlSerializer serializer = new XmlSerializer(transformObject.GetType());
                serializer.Serialize(memStream, transformObject);
                memStream.Position = 0;
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(memStream);
                serializedElement = xmlDoc.DocumentElement;
            }
            catch (Exception)
            {
            }
            return serializedElement;
        }
        #endregion // End - Serialize given object into stream.



        /// <summary>
        /// Deserialize given XmlElement into object.
        /// </summary>
        /// <param name="xmlElement">xmlElement to deserialize.</param>
        /// <param name="tp">Type of resultant deserialized object.</param>
        /// <returns>Returns deserialized object.</returns>
        #region Deserialize given string into object.
        public static object Deserialize(XmlElement xmlElement, System.Type tp)
        {
            Object transformedObject = null;
            try
            {
                Stream memStream = StringToStream(xmlElement.OuterXml);
                XmlSerializer serializer = new XmlSerializer(tp);
                transformedObject = serializer.Deserialize(memStream);
            }
            catch (Exception)
            {

            }
            return transformedObject;
        }
        #endregion // End - Deserialize given string into object.

        /// <summary>
        /// Conversion from string to stream.
        /// </summary>
        /// <param name="str">Input string.</param>
        /// <returns>Returns stream.</returns>
        #region Conversion from string to stream.
        public static Stream StringToStream(String str)
        {
            MemoryStream memStream = null;
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(str);//new byte[str.Length];
                memStream = new MemoryStream(buffer);
            }
            catch (Exception)
            {
            }
            finally
            {
                memStream.Position = 0;
            }

            return memStream;
        }
        #endregion // End - Conversion from string to stream.


    }
}
