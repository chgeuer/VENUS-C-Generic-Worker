//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure;
using System.ComponentModel.Composition;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.EMIC.Cloud.Notification;
using System.Globalization;

namespace Microsoft.EMIC.Cloud.GenericWorker.SampleNotifications
{
    [Export(typeof(INotificationPlugin))]
    public class AzureQueueJobStatusNotificationPlugin : INotificationPlugin
    {
        public const string QUEUE_NAME = "queuename";
        public const string MESSAGE = "msg";
        public const string CONNECTION_STRING = "connectionstring";

        private static CloudStorageAccount account;

        public void Notify(IJob job, List<SerializableKeyValuePair<string, string>> pluginConfig)
        {
            var connstr = pluginConfig.Where(kvp => kvp.Key == AzureQueueJobStatusNotificationPlugin.CONNECTION_STRING).Select(kvp => kvp.Value).FirstOrDefault();
            if (connstr == null)
                throw new ArgumentException(ExceptionMessages.SubscriptionConnStr);

            account = CloudStorageAccount.Parse(connstr); //throws format exception if connstr is not valid
            var queueClient = account.CreateCloudQueueClient();
            var queueName = pluginConfig.Where(kvp => kvp.Key == AzureQueueJobStatusNotificationPlugin.QUEUE_NAME).Select(kvp => kvp.Value).FirstOrDefault();
            if (queueName == null)
                throw new ArgumentException(ExceptionMessages.SubscriptionQueueName);

            var queue = queueClient.GetQueueReference(queueName);
            queue.CreateIfNotExist();
            queue.AddMessage(
                new CloudQueueMessage(
                    String.Format(CultureInfo.CurrentCulture,"msg for job {0} with status {1} and config {2}",job.InternalJobID,job.Status,pluginConfig
                    .Where(kvp => kvp.Key == AzureQueueJobStatusNotificationPlugin.MESSAGE)
                    .Select(kvp =>kvp.Value)
                    .FirstOrDefault()
                    )
                )
            );
        }

        string INotificationPlugin.Name
        {
            get { return JobStatusNotificationPluginNames.AZURE_QUEUE; }
        }
    }
}
