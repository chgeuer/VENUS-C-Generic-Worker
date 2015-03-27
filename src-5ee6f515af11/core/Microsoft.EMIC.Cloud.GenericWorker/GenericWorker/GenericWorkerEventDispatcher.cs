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

namespace Microsoft.EMIC.Cloud.GenericWorker
{
    /// <summary>
    /// This class uses MEF to import all UsageEventListeners, and dispatches incoming usage events to the imported listeners 
    /// </summary>
    [Export(typeof(GenericWorkerEventDispatcher))]
    public class GenericWorkerEventDispatcher
    {
        /// <summary>
        /// Gets or sets the event listeners.
        /// </summary>
        /// <value>
        /// The event listeners.
        /// </value>
        [ImportMany(typeof(IUsageEventListener))]
        public List<IUsageEventListener> EventListeners { get; set; }

        //TODO: Use TPL here
        /// <summary>
        /// Notifies all registered eventlisteners about the specified usage event.
        /// </summary>
        /// <param name="usageEvent">The usage event.</param>
        public void Notify(IUsageEvent usageEvent)
        {
            EventListeners.ForEach(l => l.Notify(usageEvent));
        }
    }
}
