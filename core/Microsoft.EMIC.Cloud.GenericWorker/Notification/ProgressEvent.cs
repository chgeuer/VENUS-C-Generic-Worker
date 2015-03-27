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
    /// This event indicates the availability of new progress information on a job.
    /// </summary>
    public class ProgressEvent : IUsageEvent
    {
        /// <summary>
        /// Gets or sets the progress value.
        /// </summary>
        /// <value>
        /// The progress value.
        /// </value>
        public int ProgressValue { get; set; }

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
        /// Initializes a new instance of the <see cref="ProgressEvent"/> class.
        /// </summary>
        public ProgressEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressEvent"/> class.
        /// </summary>
        /// <param name="progressValue">The progress value.</param>
        /// <param name="job">The job.</param>
        /// <param name="notificationTime">The notification time.</param>
        public ProgressEvent(int progressValue, IJob job, DateTime notificationTime)
        {
            ProgressValue = progressValue;
            Job = job;
            NotificationTime = notificationTime;
        }
    }
}
