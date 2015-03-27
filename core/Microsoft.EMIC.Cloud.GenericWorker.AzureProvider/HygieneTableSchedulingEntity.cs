//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using Microsoft.WindowsAzure.StorageClient;

namespace Microsoft.EMIC.Cloud.GenericWorker.AzureProvider
{
    /// <summary>
    /// Hygiene table scheduling entity
    /// </summary>
    public class HygieneTableSchedulingEntity : TableServiceEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HygieneTableSchedulingEntity"/> class.
        /// </summary>
        public HygieneTableSchedulingEntity() { }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is running.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is running; otherwise, <c>false</c>.
        /// </value>
        public bool IsRunning { get; set; }

        /// <summary>
        /// Gets or sets the last run.
        /// </summary>
        /// <value>
        /// The last run.
        /// </value>
        public DateTime LastRun { get; set; }
    }
}
