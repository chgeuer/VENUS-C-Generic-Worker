//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.EMIC.Cloud.GenericWorker.AzureAccounting
{
    /// <summary>
    /// Represents a VM usage record.
    /// </summary>
    public class VmUsage : UsageRecord
    {
        public string RefHost { get; set; }
        public string RefVM { get; set; }
        public DateTime UsageStart { get; set; }
        public DateTime UsageEnd { get; set; }
        //public string UsageTime { get; set; }
        //public string UsagePhase { get; set; }
    }
}
