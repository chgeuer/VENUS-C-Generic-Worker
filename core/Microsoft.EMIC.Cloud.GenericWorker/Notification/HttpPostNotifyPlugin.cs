//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EMIC.Cloud.GenericWorker;
using System.ComponentModel.Composition;

namespace Microsoft.EMIC.Cloud.Notification
{
    /// <summary>
    /// This is a mock notification plugin class, just to be used in tests.
    /// </summary>
    [Export(typeof(INotificationPlugin))]
    public class HttpPostNotifyPlugin : INotificationPlugin
    {
        /// <summary>
        /// Triggers notification for the given job and the given pluginconfig.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="pluginConfig">The plugin config.</param>
        public void Notify(IJob job, List<SerializableKeyValuePair<string, string>> pluginConfig)
        {
            return; 
        }

        /// <summary>
        /// Gets the name of the notification plugin.
        /// </summary>
        public string Name
        {
            get { return JobStatusNotificationPluginNames.HTTP_POST; }
        }
    }
}
