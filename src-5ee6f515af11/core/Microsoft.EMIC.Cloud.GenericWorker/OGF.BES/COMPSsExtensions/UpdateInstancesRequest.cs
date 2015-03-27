//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OGF.BES.COMPSsExtensions
{
    /// <summary>
    /// The type used by COMPSs in order to scale up or down. It is not used in GW, it is only added for interoperability reasons.
    /// </summary>
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.MessageContractAttribute(IsWrapped = false)]
    public partial class UpdateInstancesRequest
    {
        /// <summary>
        /// COMPSs updateinstances type
        /// </summary>
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://schemas.ggf.org/bes/2006/08/bes-factory", Order = 0)]
        public UpdateInstancesType UpdateInstances;

        /// <summary>
        /// UpdateInstancesRequest public constructor
        /// </summary>
        public UpdateInstancesRequest()
        {
        }

        /// <summary>
        /// UpdateInstancesRequest public constructor
        /// </summary>
        /// <param name="UpdateInstances">Update Instances Type used by COMPSs</param>
        public UpdateInstancesRequest(UpdateInstancesType UpdateInstances)
        {
            this.UpdateInstances = UpdateInstances;
        }
    }
}
