//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OGF.BES
{
    /// <summary>
    /// Wrapper for Activity Statuses Request
    /// </summary>
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.MessageContractAttribute(IsWrapped = false)]
    public partial class GetActivityStatusesRequest
    {

        /// <summary>
        /// An instance of <see cref="GetActivityStatusesType"/> class associated with this request
        /// </summary>
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://schemas.ggf.org/bes/2006/08/bes-factory", Order = 0)]
        public GetActivityStatusesType GetActivityStatuses;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetActivityStatusesRequest"/> class.
        /// </summary>
        public GetActivityStatusesRequest()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetActivityStatusesRequest"/> class.
        /// </summary>
        /// <param name="GetActivityStatuses">The get activity statuses.</param>
        public GetActivityStatusesRequest(GetActivityStatusesType GetActivityStatuses)
        {
            this.GetActivityStatuses = GetActivityStatuses;
        }
    }
}
