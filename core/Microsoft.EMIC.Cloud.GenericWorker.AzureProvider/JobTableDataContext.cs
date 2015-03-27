//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.Net;
using System.Data.Services.Client;

namespace Microsoft.EMIC.Cloud.GenericWorker.AzureProvider
{

    /// <summary>
    /// this class should be deprecated as soon as projection is supported by the azure sdk
    /// </summary>
    public class TableServiceContextV2 : TableServiceContext
    {
        private const string StorageVersionHeader = "x-ms-version";
        private const string August2011Version = "2011-08-18";

        /// <summary>
        /// Initializes a new instance of the <see cref="TableServiceContextV2"/> class.
        /// This class is needed to use projection on Azure tables
        /// </summary>
        /// <param name="baseAddress">The Table service endpoint to use create the service context.</param>
        /// <param name="credentials">The account credentials.</param>
        public TableServiceContextV2(string baseAddress, StorageCredentials credentials) :
            base(baseAddress, credentials)
        {
            this.SendingRequest += SendingRequestWithNewVersion;
        }

        private void SendingRequestWithNewVersion(object sender, SendingRequestEventArgs e)
        {
            HttpWebRequest request = e.Request as HttpWebRequest;

            // Apply the new storage version as a header value
            request.Headers[StorageVersionHeader] = August2011Version;
        }
    }

    /// <summary>
    /// The <see cref="TableServiceContext"/> to interact with the detailled table for all jobs. 
    /// </summary>
    public class JobTableDataContext : TableServiceContextV2
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobTableDataContext"/> class.
        /// </summary>
        /// <param name="baseAddress">The base address.</param>
        /// <param name="credentials">The credentials.</param>
        /// <param name="tableName">Name of the table.</param>
        public JobTableDataContext(string baseAddress, StorageCredentials credentials, string tableName)
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
        /// Gets all jobs.
        /// </summary>
        public IQueryable<JobTableEntity> AllJobs
        {
            //get { return this.CreateQuery<JobTableEntity>(this.TableName); }
            // The old solution didn't support paging if there are two many result entities.
            get { return this.CreateQuery<JobTableEntity>(this.TableName).AsTableServiceQuery<JobTableEntity>(); }
        }

        /// <summary>
        /// Filters the list of jobs by the job owner
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <returns></returns>
        public IQueryable<JobTableEntity> AllJobsOfOwner(string owner)
        {
            return this.AllJobs.Where(j => j.PartitionKey == owner).AsQueryable<JobTableEntity>().AsTableServiceQuery<JobTableEntity>();
        }

        internal void AddJob(JobTableEntity jobTableEntry)
        {
            this.AddObject(this.TableName, jobTableEntry);
            this.SaveChangesWithRetries();
        }

        /// <summary>
        /// Gets the <see cref="JobStatus"/>.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="internalJobID">The internal job ID.</param>
        /// <returns></returns>
        public JobStatus GetStatus(string owner, string internalJobID)
        {
            var status = this.AllJobs
                .Where(x => x.PartitionKey == owner && x.RowKey == internalJobID)
                .FirstOrDefault();

            if (status == null)
                return JobStatus.Failed;
            
            return (JobStatus)Enum.Parse(typeof(JobStatus), status.Status);
        }
    }
}