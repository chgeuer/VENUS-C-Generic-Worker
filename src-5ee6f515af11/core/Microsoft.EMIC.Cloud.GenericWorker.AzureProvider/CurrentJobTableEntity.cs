//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using Microsoft.WindowsAzure.StorageClient;

namespace Microsoft.EMIC.Cloud.GenericWorker.AzureProvider
{
    /// <summary>
    /// A <see cref="TableServiceEntity"/> containing index information about submitted and running <see cref="IJob"/>s.
    /// </summary>
    public class CurrentJobTableEntity : TableServiceEntity
    {

        /// <summary>
        /// Gets or sets the job prio.
        /// </summary>
        /// <value>
        /// The job prio.
        /// </value>
        public string JobPrio { get; set; }

        /// <summary>
        /// Sets the job prio.
        /// </summary>
        /// <value>
        /// The job prio.
        /// </value>
        public void SetPrio(JobPriority priority)
        {
            this.JobPrio = Enum.GetName(typeof(JobPriority), priority);
        }

        /// <summary>
        /// Gets the status.
        /// </summary>
        /// <returns></returns>
        public JobPriority GetPrio()
        {
            return (JobPriority)Enum.Parse(typeof(JobPriority), this.JobPrio);
        }

        /// <summary>
        /// Gets or sets the last change.
        /// </summary>
        /// <value>
        /// The last change.
        /// </value>
        public DateTime? LastChange { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentJobTableEntity"/> class.
        /// </summary>
        public CurrentJobTableEntity() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentJobTableEntity"/> class.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="internalJobID">The internal job ID.</param>
        public CurrentJobTableEntity(string owner, string internalJobID)
            : base(owner, internalJobID) 
        {
                this.LastChange = DateTime.UtcNow;
                SetPrio(JobPriority.Default);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("User \"{0}\", Internal Job ID \"{1}\", LastAccess \"{2}\", Priority \"{3}\" ", this.PartitionKey, this.RowKey, this.LastChange.Value.ToLocalTime().ToString(),System.Enum.GetName(typeof(JobPriority),this.JobPrio));
        }
    }
}
