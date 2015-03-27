//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Microsoft.EMIC.Cloud.GenericWorker;


namespace Microsoft.EMIC.Cloud.Notification
{
    /// <summary>
    /// This class provides constants to use to refer to a specific notification plugin.
    /// </summary>
    public static class JobStatusNotificationPluginNames
    {
        /// <summary>
        /// The plugin name for the queue notification plugin.
        /// </summary>
        public const string AZURE_QUEUE = "Azure_Queue";
        /// <summary>
        /// The plugin name for the HTTP Post notification plugin.
        /// </summary>
        public const string HTTP_POST = "HTTP_POST";
    }

    /// <summary>
    /// This class provides a constant to use to refer to the name setting for a specific notification plugin. Every plugin config has to provide the mandatory name setting.
    /// </summary>
    public static class PluginConfigMandatoryKeys
    {
        /// <summary>
        /// The key for the plugin name, that has to be specified in the plugin configuration.
        /// </summary>
        public const string NAME = "name";
    }

    /// <summary>
    /// This class implements the IUsageEventListener. It is notified by the event manager on usage events.
    /// All usage events but status events are ignored by this listener.
    /// Status events of a job are passed to subscribed notification plugings. 
    /// </summary>
    [Export(typeof(IUsageEventListener))]
    public class JobStatusListener : IUsageEventListener 
    {
        /// <summary>
        /// Gets or sets the list of registered notification plugins.
        /// </summary>
        /// <value>
        /// The notification plugins.
        /// </value>
        [ImportMany(typeof(INotificationPlugin))]
        public List<INotificationPlugin> NotificationPlugins { get; set; }

        /// <summary>
        /// Gets or sets the runtime environment.
        /// </summary>
        [Import(typeof(IGWRuntimeEnvironment), RequiredCreationPolicy = CreationPolicy.NonShared)]
        public IGWRuntimeEnvironment RuntimeEnvironment { get; set; }

        /// <summary>
        /// Notifies the specified usage event.
        /// </summary>
        /// <param name="usageEvent">The usage event.</param>
        public void Notify(IUsageEvent usageEvent)
        {
            var statusEvent = usageEvent as StatusEvent;
            if (statusEvent != null)
            {
                if (statusEvent.OldStatus != statusEvent.NewStatus) // we are only interested in status changes
                {                    
                    var job = statusEvent.Job;
                    var subs = this.RuntimeEnvironment.GetSubscriptions(job);
                    //retrieve the list of subscriptions that are registered for the current status of this job
                    var subsForNewStatus = subs.Where(s => s.Statuses.Contains(statusEvent.NewStatus)).Select(s=> s.PluginConfig).ToList();
                    //iterate through all notification plugins and pass the status event to each subscribed plugin
                    subsForNewStatus.ForEach(
                        sub => NotificationPlugins.Where(p=> p.Name==sub
                            .Where(kvp => kvp.Key==PluginConfigMandatoryKeys.NAME)
                            .Select(kvp => kvp.Value).First())
                            .ToList().ForEach(p => p.Notify(statusEvent.Job, sub)));

                    string groupName, jobName;
                    Func<IJob, bool> IsGroupFinished = (groupmember) =>
                    {
                        return RuntimeEnvironment.GetJobGroupByJob(groupmember).All(j => j.Status == JobStatus.Finished);
                    };

                    if (JobID.TryParseGroup(job.CustomerJobID, out groupName, out jobName)) //is the current job part of a group
                    {
                        //retrivee all subscriptions on the current status that are registerd for the job group
                        var groupHead = RuntimeEnvironment.GetGroupHeadByGroupName(job.Owner, groupName);
                        subs = this.RuntimeEnvironment.GetSubscriptions(groupHead);
                        subsForNewStatus = subs.Where(s => s.Statuses.Contains(statusEvent.NewStatus)).Select(s => s.PluginConfig).ToList();
                        if (subsForNewStatus.Count == 0)
                        {
                            return; //no group notifications for this status
                        }
                        //if the new status is failed or if the new status is finished and all group members are finished, then                        

                        if (statusEvent.NewStatus == JobStatus.Failed || (statusEvent.NewStatus == JobStatus.Finished && IsGroupFinished(job)))
                        {
                            //iterate through all notification plugins and pass the status event to each subscribed plugin
                            subsForNewStatus.ForEach(
                                sub => NotificationPlugins.Where(p => p.Name == sub
                                    .Where(kvp => kvp.Key == PluginConfigMandatoryKeys.NAME)
                                    .Select(kvp => kvp.Value).First())
                                    .ToList().ForEach(p => p.Notify(statusEvent.Job, sub)));
                            //remove subscriptions for the group
                            this.RuntimeEnvironment.UnSubscribe(groupHead); //the group cannot get any other state
                        }
                    }
                    // look up jobs internal job id in the according azure table
                    // retrieve the list of notificationPlugins subscribed for the job
                    // send out notification if the according plugin is available 
                }
            }
        }
    }
}
