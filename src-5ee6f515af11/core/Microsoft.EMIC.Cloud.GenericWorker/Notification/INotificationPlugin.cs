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
    /// Interface that a notification plugin has to implement
    /// </summary>
    public interface INotificationPlugin
    {
        /// <summary>
        /// Gets the name of the notification plugin.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// This method is called if there is a statuschange on a job for which this plugin had a subscription.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="pluginConfig">The plugin config which is read from the subscription blob and passed as is to the notification plugin.</param>
        void Notify(IJob job, List<SerializableKeyValuePair<string, string>> pluginConfig);
    }
}
