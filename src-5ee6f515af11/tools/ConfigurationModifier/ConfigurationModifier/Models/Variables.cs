//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConfigurationModifier.Models
{
    public class Variables
    {
        public int InstanceCount { get; set; }
        public string ConnectionString { get; set; }
        public string STSThumbprint { get; set; }
        public string ManagementThumbprint { get; set; }
        public string DeploymentURL { get; set; }
        public string RemoteAccountUserName { get; set; }
        public string EncryptedPassword { get; set; }
        public bool AllowInsecure { get; set; }
        public bool EnableAccounting { get; set; }
        public string ServiceName { get; set; }
        public string SubscriptionId { get; set; }
    }
}
