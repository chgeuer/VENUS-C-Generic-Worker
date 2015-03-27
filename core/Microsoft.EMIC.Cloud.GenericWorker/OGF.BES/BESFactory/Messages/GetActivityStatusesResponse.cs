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
    /// Wrapper for Activity Statuses Response
    /// </summary>
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.MessageContractAttribute(IsWrapped = false)]
    public partial class GetActivityStatusesResponse
    {

        /// <summary>
        /// An instance of <see cref="GetActivityStatusesResponseType"/> class associated with this request
        /// </summary>
        [System.ServiceModel.MessageBodyMemberAttribute(Name = "GetActivityStatusesResponse", Namespace = "http://schemas.ggf.org/bes/2006/08/bes-factory", Order = 0)]
        public GetActivityStatusesResponseType GetActivityStatusesResponse1;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetActivityStatusesResponse"/> class.
        /// </summary>
        public GetActivityStatusesResponse()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetActivityStatusesResponse"/> class.
        /// </summary>
        /// <param name="GetActivityStatusesResponse1">The get activity statuses response1.</param>
        public GetActivityStatusesResponse(GetActivityStatusesResponseType GetActivityStatusesResponse1)
        {
            this.GetActivityStatusesResponse1 = GetActivityStatusesResponse1;
        }
    }
}
