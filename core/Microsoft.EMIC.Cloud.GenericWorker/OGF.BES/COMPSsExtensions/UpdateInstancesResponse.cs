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
    public partial class UpdateInstancesResponse
    {
        /// <summary>
        /// COMPSs updateInstancesResponse type
        /// </summary>
        [System.ServiceModel.MessageBodyMemberAttribute(Name = "UpdateInstancesResponse", Namespace = "http://schemas.ggf.org/bes/2006/08/bes-factory", Order = 0)]
        public UpdateInstancesResponseType UpdateInstancesResponse1;

        /// <summary>
        /// UpdateInstancesResponse public constructor
        /// </summary>
        public UpdateInstancesResponse()
        {
        }

        /// <summary>
        /// UpdateInstancesResponse public constructor
        /// </summary>
        /// <param name="UpdateInstancesResponse1">Update Instances Response Type used by COMPSs</param>
        public UpdateInstancesResponse(UpdateInstancesResponseType UpdateInstancesResponse1)
        {
            this.UpdateInstancesResponse1 = UpdateInstancesResponse1;
        }
    }
}
