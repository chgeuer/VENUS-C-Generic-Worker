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

    public class JobResourceUsage : UsageRecord
    {
        public int CPUDuration { get; set; }
        public int Disk { get; set; }
        public int NumberOfInputFiles { get; set; }
        public long SizeOfInputFiles { get; set; }
        public DateTime JobEndTime { get; set; }
        public DateTime JobStartTime { get; set; }
        public string JobID { get; set; }
        public string JobName { get; set; }
        public string refVM { get; set; }
        public int NumberOfOutputFiles { get; set; }
        public long NetworkIn { get; set; }
        public long NetworkOut { get; set; }
        public long SizeOfOutputFiles { get; set; }
        public string Status { get; set; }
        public int Memory { get; set; }
        public int NumberofCores { get; set; }
        public int WallDuration { get; set; }
        public int Processors { get; set; }
    }
}
