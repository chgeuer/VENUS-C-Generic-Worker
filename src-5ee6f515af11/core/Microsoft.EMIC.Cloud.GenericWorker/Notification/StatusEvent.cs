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
    /// This event indicates a status change of a job. 
    /// </summary>
    public class StatusEvent : IUsageEvent
    {
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
        /// Gets or sets the job.
        /// </summary>
        /// <value>
        /// The job.
        /// </value>
        public IJob Job { get; set; }
        /// <summary>
        /// Gets or sets the notification time.
        /// </summary>
        /// <value>
        /// The notification time.
        /// </value>
        public DateTime NotificationTime { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusEvent"/> class.
        /// </summary>
        public StatusEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusEvent"/> class.
        /// </summary>
        /// <param name="oldStatus">The old status.</param>
        /// <param name="newStatus">The new status.</param>
        /// <param name="job">The job.</param>
        /// <param name="notificationTime">The notification time.</param>
        public StatusEvent(JobStatus oldStatus, JobStatus newStatus, IJob job, DateTime notificationTime)
        {
            OldStatus = oldStatus;
            NewStatus = newStatus;
            Job = job;
            NotificationTime = notificationTime;
        }
    }
}
