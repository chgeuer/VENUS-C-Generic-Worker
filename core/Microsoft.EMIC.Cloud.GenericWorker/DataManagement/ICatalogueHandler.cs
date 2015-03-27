//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.Collections;
using System.Collections.Generic;

namespace Microsoft.EMIC.Cloud.DataManagement
{
    /// <summary>
    /// Interface for the catalogue handler
    /// </summary>
    public interface ICatalogueHandler
    {
        /// <summary>
        /// Adds the specified logical name with the referencing argument.
        /// </summary>
        /// <param name="logicalName">The logical name.</param>
        /// <param name="referenceArgument">The reference argument.</param>
        void Add(string logicalName, IReferenceArgument referenceArgument);

        /// <summary>
        /// Gets the referencing argument to the specified logical name.
        /// </summary>
        /// <param name="logicalName">The logical name.</param>
        /// <returns></returns>
        IReferenceArgument Get(string logicalName);

        /// <summary>
        /// Checks whether there is a referencing argument for the specified logical name.
        /// </summary>
        /// <param name="logicalName">The logical name.</param>
        /// <returns></returns>
        bool Exists(string logicalName);

        /// <summary>
        /// Removes the referencing arugment by the specified logical name.
        /// </summary>
        /// <param name="logicalName">The logical name.</param>
        void Remove(string logicalName);

        /// <summary>
        /// Get a list of the logical names.
        /// </summary>
        /// <returns></returns>
        List<string> GetLogicalNames();
    }
}