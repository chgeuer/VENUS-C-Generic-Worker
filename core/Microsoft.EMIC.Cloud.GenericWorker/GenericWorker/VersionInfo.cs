//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Microsoft.EMIC.Cloud.GenericWorker
{
    using Microsoft.EMIC.AutomatedBuild;

    /// <summary>
    /// Version Info class
    /// </summary>
    public static class VersionInfo
    {
        /// <summary>
        /// Gets the version.
        /// </summary>
        public static string Version { get { return BuildInfo.Version; } }
    }
}
