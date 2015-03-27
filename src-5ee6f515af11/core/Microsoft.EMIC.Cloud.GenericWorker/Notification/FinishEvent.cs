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
    /// This event indicates the status change of a job to finished 
    /// </summary>
    public class FinishEvent : IUsageEvent
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
        /// Initializes a new instance of the <see cref="FinishEvent"/> class.
        /// </summary>
        public FinishEvent()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FinishEvent"/> class.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="notificationTime">The notification time.</param>
        public FinishEvent(IJob job, DateTime notificationTime)
        {
            Job = job;
            NotificationTime = notificationTime;
        }
    }
}
