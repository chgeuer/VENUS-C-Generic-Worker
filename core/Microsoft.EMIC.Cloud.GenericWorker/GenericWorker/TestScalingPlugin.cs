//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;

namespace Microsoft.EMIC.Cloud.GenericWorker
{
    /// <summary>
    /// This scaling plugin only serves as a mock for testing purposes
    /// </summary>
    [Export(typeof(IScalingPlugin))]
    public class TestScalingPlugin : IScalingPlugin
    {
        const string CloudProviderID = "TestCloudProvider";
        int m_instanceCount = 2;

        string IScalingPlugin.CloudProviderID
        {
            get
            {
                return CloudProviderID;
            }
        }

        int IScalingPlugin.InstanceCount
        {
            get
            {
                return m_instanceCount;
            }
            set
            {
                m_instanceCount = value;
            }
        }
    }
}
