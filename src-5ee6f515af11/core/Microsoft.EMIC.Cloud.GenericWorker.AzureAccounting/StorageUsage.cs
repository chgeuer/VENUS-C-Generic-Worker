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
    /// Represents a Storage usage record.
    /// </summary>
    public class StorageUsage : UsageRecord
    {
        public string ItemCount { get; set; }
        public string StorageTransactions { get; set; }
        public string StorageVolume { get; set; }
    }
}
