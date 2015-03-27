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
    /// This event indicates the availability of new network usage information on a job.
    /// </summary>
    public class NetworkEvent : IUsageEvent
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
        /// Gets or sets the bytes received.
        /// </summary>
        /// <value>
        /// The bytes received.
        /// </value>
        public long BytesReceived { get; set; }

        /// <summary>
        /// Gets or sets the bytes sent.
        /// </summary>
        /// <value>
        /// The bytes sent.
        /// </value>
        public long BytesSent { get; set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkEvent"/> class.
        /// </summary>
        public NetworkEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkEvent"/> class.
        /// </summary>
        /// <param name="oldStatus">The old status.</param>
        /// <param name="newStatus">The new status.</param>
        /// <param name="bytesReceived">The bytes received.</param>
        /// <param name="bytesSent">The bytes sent.</param>
        /// <param name="job">The job.</param>
        /// <param name="notificationTime">The notification time.</param>
        public NetworkEvent(JobStatus oldStatus, JobStatus newStatus, long bytesReceived, long bytesSent, IJob job, DateTime notificationTime)
        {
            OldStatus = oldStatus;
            NewStatus = newStatus;
            BytesReceived = bytesReceived;
            BytesSent = bytesSent;
            Job = job;
            NotificationTime = notificationTime;
        }
    }
}
