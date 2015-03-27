//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.EMIC.Cloud.GenericWorker;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;

namespace Microsoft.EMIC.Cloud.GenericWorker.AzureProvider
{
    /// <summary>
    /// A <see cref="TableServiceEntity"/> containing detail information about an <see cref="IJob"/>.
    /// </summary>
    public class JobTableEntity : TableServiceEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobTableEntity"/> class.
        /// </summary>
        public JobTableEntity()
        {
            SetStringPropertiesToEmpty();
            this.SetStatus(JobStatus.Submitted);
            this.LastChange = DateTime.UtcNow;
            this.ResetCounter = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JobTableEntity"/> class.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="internalJobID">The internal job ID.</param>
        public JobTableEntity(string owner, string internalJobID)
            : base(owner, internalJobID)
        {
            SetStringPropertiesToEmpty();
            this.SetStatus(JobStatus.Submitted);
            this.LastChange = DateTime.UtcNow;
        }

        #region Status

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        public string Status { get; set; }

        /// <summary>
        /// Sets the status.
        /// </summary>
        /// <param name="status">The status.</param>
        public void SetStatus(JobStatus status)
        {
            this.Status = Enum.GetName(typeof(JobStatus), status);
        }

        /// <summary>
        /// Determines whether a given jov is a group head and thus only a dummy job.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [is group head]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsGroupHead()
        {
            return JobID.IsGroupHead(this.CustomerJobID);
        }

        /// <summary>
        /// Gets the status.
        /// </summary>
        /// <returns></returns>
        public JobStatus GetStatus()
        {
            return (JobStatus)Enum.Parse(typeof(JobStatus), this.Status);
        }

        #endregion

        private void SetStringPropertiesToEmpty()
        {
            InstanceID = string.Empty;            
        }

        /// <summary>
        /// Gets or sets the customer job ID.
        /// </summary>
        /// <value>
        /// The customer job ID.
        /// </value>
        public string CustomerJobID { get; set; }
        
        /// <summary>
        /// Gets or sets the application identification URI.
        /// </summary>
        /// <value>
        /// The application identification URI.
        /// </value>
        public string ApplicationIdentificationURI { get; set; }
        
        /// <summary>
        /// Gets or sets the instance ID.
        /// </summary>
        /// <value>
        /// The instance ID.
        /// </value>
        public string InstanceID { get; set; }

        /// <summary>
        /// Gets or sets the submission time.
        /// </summary>
        /// <value>
        /// The submission time.
        /// </value>
        public DateTime? Submission { get; set; }
        
        /// <summary>
        /// Gets or sets the start.
        /// </summary>
        /// <value>
        /// The start.
        /// </value>
        public DateTime? Start { get; set; }
        
        /// <summary>
        /// Gets or sets the end.
        /// </summary>
        /// <value>
        /// The end.
        /// </value>
        public DateTime? End { get; set; }
        
        /// <summary>
        /// Gets or sets the last change.
        /// </summary>
        /// <value>
        /// The last change.
        /// </value>
        public DateTime? LastChange { get; set; }
        
        /// <summary>
        /// Gets or sets the data checked.
        /// </summary>
        /// <value>
        /// The data checked.
        /// </value>
        public DateTime? DataChecked { get; set; }
        
        /// <summary>
        /// Gets or sets the reset counter.
        /// </summary>
        /// <value>
        /// The reset counter.
        /// </value>
        public int ResetCounter { get; set; }

        /// <summary>
        /// Gets or sets the GUID of the parent.(Applicable to hierarchial jobs)
        /// </summary>
        /// <value>
        /// The ParentGUID
        /// </value>
        public string ParentGUID { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("Owner \"{0}\", Job \"{1}\", Customer job ID \"{2}\", Status \"{3}\", LastAccess {4}, Algo {5}",
                this.PartitionKey, this.RowKey, this.CustomerJobID, this.Status, 
                this.LastChange.Value.ToLocalTime().ToString(), 
                this.ApplicationIdentificationURI);
        }
    }
}