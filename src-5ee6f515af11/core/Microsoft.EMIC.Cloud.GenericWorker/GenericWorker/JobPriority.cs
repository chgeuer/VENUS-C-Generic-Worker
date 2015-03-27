//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.EMIC.Cloud.GenericWorker
{
    /// <summary>
    /// Enum for job priorities -> influences job execution order
    /// </summary>
    public enum JobPriority
    {
        /// <summary>
        /// Lowest job priority: for absolutly non urgent jobs
        /// </summary>
        Lowest = 0,

        /// <summary>
        /// Low job priority: for non urgent jobs
        /// </summary>
        Low = 1,

        /// <summary>
        /// Standard job priority: this job priority is selected if you do not define a priority
        /// </summary>
        Default = 2,

        /// <summary>
        /// Low job priority: for non urgent jobs
        /// </summary>
        High = 3,

        /// <summary>
        /// Low job priority: for non urgent jobs
        /// </summary>
        Highest = 4
    }
}
