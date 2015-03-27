//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EMIC.Cloud.GenericWorker;

namespace Microsoft.EMIC.Cloud.Notification
{
    /// <summary>
    /// This event indicates the availability of new storage usage information on a job.
    /// </summary>
    public class StorageEvent : IUsageEvent
    {
        /// <summary>
        /// Gets or sets the notification time.
        /// </summary>
        /// <value>
        /// The notification time.
        /// </value>
        public DateTime NotificationTime { get; set; }

        /// <summary>
        /// Gets or sets the job.
        /// </summary>
        /// <value>
        /// The job.
        /// </value>
        public IJob Job { get; set; }

        /// <summary>
        /// Gets or sets the old status.
        /// </summary>
        /// <value>
        /// The old status.
        /// </value>
        public JobStatus OldStatus { get; set; }

        /// <summary>
        /// Gets or sets the new status.
        /// </summary>
        /// <value>
        /// The new status.
        /// </value>
        public JobStatus NewStatus { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is download.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is download; otherwise, <c>false</c>.
        /// </value>
        public bool IsDownload { get; set; }

        /// <summary>
        /// Gets or sets the numberof documents.
        /// </summary>
        /// <value>
        /// The numberof documents.
        /// </value>
        public int NumberofDocuments { get; set; }

        /// <summary>
        /// Gets or sets the bytes transferred.
        /// </summary>
        /// <value>
        /// The bytes transferred.
        /// </value>
        public long BytesTransferred { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageEvent"/> class.
        /// </summary>
        public StorageEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageEvent"/> class.
        /// </summary>
        /// <param name="oldStatus">The old status.</param>
        /// <param name="newStatus">The new status.</param>
        /// <param name="isDownload">if set to <c>true</c> [is download].</param>
        /// <param name="numberofDocuments">The numberof documents.</param>
        /// <param name="bytesTransferred">The bytes transferred.</param>
        /// <param name="job">The job.</param>
        /// <param name="notificationTime">The notification time.</param>
        public StorageEvent(JobStatus oldStatus, JobStatus newStatus, bool isDownload, int numberofDocuments, long bytesTransferred, IJob job, DateTime notificationTime)
        {
            OldStatus = oldStatus;
            NewStatus = newStatus;
            IsDownload = isDownload;
            NumberofDocuments = numberofDocuments;
            BytesTransferred = bytesTransferred;
            Job = job;
            NotificationTime = notificationTime;
        }
    }
}
