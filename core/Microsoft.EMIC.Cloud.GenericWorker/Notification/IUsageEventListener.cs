//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.EMIC.Cloud.Notification
{
    /// <summary>
    /// All event listeners have to implement this interface
    /// </summary>
    public interface IUsageEventListener
    {
        /// <summary>
        /// Notifies the specified usage event.
        /// </summary>
        /// <param name="usageEvent">The usage event.</param>
        void Notify(IUsageEvent usageEvent);
    }
}
