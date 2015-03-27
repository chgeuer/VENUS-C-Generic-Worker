//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Microsoft.EMIC.Cloud.GenericWorker.AzureProvider
{
    /// <summary>
    /// The <see cref="TableServiceContext"/> to interact with the index table for submitted or running jobs. 
    /// </summary>
    public class CurrentJobTableDataContext : TableServiceContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentJobTableDataContext"/> class.
        /// </summary>
        /// <param name="baseAddress">The base address.</param>
        /// <param name="credentials">The credentials.</param>
        /// <param name="tableName">Name of the table.</param>
        public CurrentJobTableDataContext(string baseAddress, StorageCredentials credentials, string tableName)
            : base(baseAddress, credentials)
        {
            this.TableName = tableName;
            this.IgnoreResourceNotFoundException = true;
        }

        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        /// <value>
        /// The name of the table.
        /// </value>
        public string TableName { get; private set; }

        /// <summary>
        /// Gets the current jobs.
        /// </summary>
        public IQueryable<CurrentJobTableEntity> CurrentJobs
        {
            //get { return this.CreateQuery<CurrentJobTableEntity>(this.TableName); }
            // The old solution didn't support paging if there are two many result entities.
            get { return this.CreateQuery<CurrentJobTableEntity>(this.TableName).AsTableServiceQuery<CurrentJobTableEntity>(); }
        }

        /// <summary>
        /// Adds the index of the job.
        /// </summary>
        /// <param name="jobTableEntry">The job table entry.</param>
        internal void AddJobIndex(CurrentJobTableEntity jobTableEntry)
        {
            this.AddObject(this.TableName, jobTableEntry);
            this.SaveChangesWithRetries();
        }
    }
}