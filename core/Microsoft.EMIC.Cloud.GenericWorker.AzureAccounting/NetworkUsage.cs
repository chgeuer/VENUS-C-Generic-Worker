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
    /// Represents a Network usage record.
    /// </summary>
    public class NetworkUsage : UsageRecord
    {
        public string PeriodNetworkIn { get; set; }
        public string PeriodNetworkOut { get; set; }
        public string OverallNetworkIn { get; set; }
        public string OverallNetworkOut { get; set; }
        public string RefVM { get; set; }
    }
}
