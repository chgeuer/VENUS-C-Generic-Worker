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
    /// Wrapper for Activity Documents Response
    /// </summary>
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.MessageContractAttribute(IsWrapped = false)]
    public partial class GetActivityDocumentsResponse
    {

        /// <summary>
        /// An instance of <see cref="GetActivityDocumentsResponseType"/> class associated with this request
        /// </summary>
        [System.ServiceModel.MessageBodyMemberAttribute(Name = "GetActivityDocumentsResponse", Namespace = "http://schemas.ggf.org/bes/2006/08/bes-factory", Order = 0)]
        public GetActivityDocumentsResponseType GetActivityDocumentsResponse1;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetActivityDocumentsResponse"/> class.
        /// </summary>
        public GetActivityDocumentsResponse()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetActivityDocumentsResponse"/> class.
        /// </summary>
        /// <param name="GetActivityDocumentsResponse1">The get activity documents response1.</param>
        public GetActivityDocumentsResponse(GetActivityDocumentsResponseType GetActivityDocumentsResponse1)
        {
            this.GetActivityDocumentsResponse1 = GetActivityDocumentsResponse1;
        }
    }
}
