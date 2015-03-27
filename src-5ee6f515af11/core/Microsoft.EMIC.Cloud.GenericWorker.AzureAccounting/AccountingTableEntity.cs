//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;

namespace Microsoft.EMIC.Cloud.GenericWorker.AzureAccounting
{
    public class AccountingTableEntity : TableServiceEntity
    {
        public AccountingTableEntity(string owner, string internalJobID)
            : base(owner, internalJobID)
        {

        }


        public AccountingTableEntity() { }

        /// <summary>
        /// Gets or sets the customer job ID.
        /// </summary>
        /// <value>
        /// The customer job ID.
        /// </value>
        public string CustomerJobID { get; set; }

        /// <summary>
        /// Gets or sets the instance ID.
        /// </summary>
        /// <value>
        /// The instance ID.
        /// </value>
        public string InstanceID { get; set; }

        /// <summary>
        /// Gets or sets the number of cores.
        /// </summary>
        /// <value>
        /// Number of Cores
        /// </value>
        public int NumberofCores { get; set; }


        /// <summary>
        /// Status of the job
        /// </summary>
        /// <value>
        /// Status of the job
        /// </value>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the start.
        /// </summary>
        /// <value>
        /// The start.
        /// </value>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end.
        /// </summary>
        /// <value>
        /// The end.
        /// </value>
        public DateTime? EndTime { get; set; }

        public int NumberOfInputFiles { get; set; }

        public long SizeofInputFiles { get; set; }

        public int NumberofOutputFiles { get; set; }

        public long SizeofOutputFiles { get; set; }

        public long ReceivedBytesStart { set; get; }

        public long SentBytesStart { set; get; }

        public long ReceivedBytesEnd { set; get; }

        public long SentBytesEnd { set; get; }

        public bool IsFinished { set; get; }

        public int TriesToPush { set; get; }

        public AccountingTableEntity Clone()
        {
            var accountingTE = new AccountingTableEntity(this.PartitionKey, this.RowKey);

            accountingTE.CustomerJobID = this.CustomerJobID;
            accountingTE.EndTime= this.EndTime;
            accountingTE.InstanceID = this.InstanceID;
            accountingTE.IsFinished = this.IsFinished;
            accountingTE.NumberofCores = this.NumberofCores;
            accountingTE.NumberOfInputFiles = this.NumberOfInputFiles;
            accountingTE.NumberofOutputFiles = this.NumberofOutputFiles;
            accountingTE.ReceivedBytesEnd = this.ReceivedBytesEnd;
            accountingTE.ReceivedBytesStart = this.ReceivedBytesStart;
            accountingTE.SentBytesEnd = this.SentBytesEnd;
            accountingTE.SentBytesStart = this.SentBytesStart;
            accountingTE.SizeofInputFiles = this.SizeofInputFiles;
            accountingTE.SizeofOutputFiles = this.SizeofOutputFiles;
            accountingTE.StartTime = this.StartTime;
            accountingTE.Status = this.Status;
            accountingTE.Timestamp = this.Timestamp;
            accountingTE.TriesToPush = this.TriesToPush;

            return accountingTE;
        }
    }
}
