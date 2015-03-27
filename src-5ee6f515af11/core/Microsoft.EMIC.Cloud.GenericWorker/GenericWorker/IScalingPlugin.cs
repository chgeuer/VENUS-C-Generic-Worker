//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.EMIC.Cloud.GenericWorker
{
    /// <summary>
    /// Interface that has to be implemented by all scaling plugins
    /// </summary>
    public interface IScalingPlugin
    {
        /// <summary>
        /// Gets the cloud provider ID.
        /// </summary>
        string CloudProviderID { get; }

        /// <summary>
        /// Gets or sets the instance count for a deployment.
        /// </summary>
        /// <value>
        /// The instance count.
        /// </value>
        int InstanceCount { get; set; }
    }
}
