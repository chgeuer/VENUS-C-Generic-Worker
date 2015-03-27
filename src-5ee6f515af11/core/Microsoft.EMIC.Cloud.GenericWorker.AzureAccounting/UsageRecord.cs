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
    /// Represents a usage record.
    /// </summary>
    public class UsageRecord
    {
        public string ID { get; set; }
        public string ConsumerID { get; set; }
        public DateTime CreateTime { get; set; }
        public string CreatorID { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string ResourceType { get; set; }
        public string ResourceOwner { get; set; }
        public Dictionary<string, string> ResourceSpecificProperties { get; set; }
        public Dictionary<string, string> CustomProperties { get; set; }

        public UsageRecord()
        {
            ResourceSpecificProperties = new Dictionary<string, string>();
            CustomProperties = new Dictionary<string, string>();
        }
    }
}
