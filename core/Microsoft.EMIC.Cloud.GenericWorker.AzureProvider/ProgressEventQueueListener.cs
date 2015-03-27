//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Microsoft.EMIC.Cloud.Notification;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Microsoft.EMIC.Cloud.GenericWorker.AzureProvider
{
    
    /// <summary>
    /// This class is notified on all kind of usage events. It handles progress events and enqueues a message in the configured azure queue for progress events.
    /// </summary>
    [Export(typeof(IUsageEventListener))]
    public class ProgressEventQueueListener : IUsageEventListener, IPartImportsSatisfiedNotification
    {
        /// <summary>
        /// Gets or sets the generic worker cloud connection string.
        /// </summary>
        /// <value>
        /// The generic worker cloud connection string.
        /// </value>
        [Import(CompositionIdentifiers.GenericWorkerConnectionString)]
        public string GenericWorkerCloudConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the name of the generic worker progress queue.
        /// </summary>
        /// <value>
        /// The name of the generic worker progress queue.
        /// </value>
        [Import(CompositionIdentifiers.DevelopmentGenericWorkerProgressQueueName)]
        public string GenericWorkerProgressQueueName { get; set; }

        private CloudStorageAccount storageAccount;

        void IPartImportsSatisfiedNotification.OnImportsSatisfied()
        {
            this.storageAccount = CloudStorageAccount.Parse(this.GenericWorkerCloudConnectionString);
            var queueClient = this.storageAccount.CreateCloudQueueClient();

            var queue = queueClient.GetQueueReference(this.GenericWorkerProgressQueueName);
            queue.CreateIfNotExist();
        }

        /// <summary>
        /// This method is called by the EventDispatcher class to notify this listener about usage events.
        /// </summary>
        /// <param name="usageEvent">The usage event.</param>
        public void Notify(IUsageEvent usageEvent)
        {
            var progressEvent = usageEvent as ProgressEvent;
            if (progressEvent != null)
            {
                var newMsg = new CloudQueueMessage(String.Format("ProgressEvent: {0}, date={1}, value={2}", progressEvent.Job.InternalJobID, progressEvent.NotificationTime.ToString("yyyyMMdd HH:mm:ss.ffff"), progressEvent.ProgressValue));
                
                var queueClient = this.storageAccount.CreateCloudQueueClient();
                var ownersQueue = string.Format("{0}_{1}", this.GenericWorkerProgressQueueName, progressEvent.Job.Owner);
                var queue = queueClient.GetQueueReference(ownersQueue);
                queue.AddMessage(newMsg, TimeSpan.FromDays(1));
            }
        }
    }
}
