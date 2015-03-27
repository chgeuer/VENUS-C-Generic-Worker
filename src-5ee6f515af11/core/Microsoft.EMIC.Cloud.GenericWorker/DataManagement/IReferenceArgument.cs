//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;

namespace Microsoft.EMIC.Cloud.DataManagement
{
    /// <summary>
    /// The common interface for all kinds of reference arguments.
    /// </summary>
    public interface IReferenceArgument : IArgument
    {
        /// <summary>
        /// Downloads the file to the specified working directory.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="tokenSource">The token source.</param>
        void Download(string workingDirectory, CancellationTokenSource tokenSource);

        /// <summary>
        /// Uploads the file out of the specified working directory.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        void Upload(string workingDirectory);

        /// <summary>
        /// Gets a file location in the specified working directory.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <returns></returns>
        string GetFileLocation(string workingDirectory);

        /// <summary>
        /// Checks whether the data item exists.
        /// </summary>
        /// <returns></returns>
        bool ExistsDataItem();
    }

}