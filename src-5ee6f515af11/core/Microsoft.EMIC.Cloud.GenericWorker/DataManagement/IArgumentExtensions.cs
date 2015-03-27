//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.Xml;

namespace Microsoft.EMIC.Cloud.DataManagement
{
    /// <summary>
    /// Interface for argument extensions
    /// </summary>
    public static class IArgumentExtensions
    {
        /// <summary>
        /// Constant string of the generic worker namespace
        /// </summary>
        public const string GenericWorkerNamespace = 
            "http://www.microsoft.com/emic/cloud/GW";


        /// <summary>
        /// This static methods uses the ArgumentRepository to deserialize an Argument which is provided as XmlElement.
        /// </summary>
        /// <param name="e">The argument as XmlElement</param>
        /// <param name="repo">The ArgumentRepository.</param>
        /// <returns></returns>
        public static IArgument AsArgument(this XmlElement e, ArgumentRepository repo)
        {
            return repo.Load(e);
        }
    }
}