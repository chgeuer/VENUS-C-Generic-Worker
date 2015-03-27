//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OGF.BES;

namespace OGF.BES
{
    /// <summary>
    /// Wrapper for Terminate Activities Response
    /// </summary>
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.MessageContractAttribute(IsWrapped = false)]
    public partial class TerminateActivitiesResponse
    {

        /// <summary>
        /// An instance of <see cref="TerminateActivitiesResponseType"/> class associated with this request
        /// </summary>
        [System.ServiceModel.MessageBodyMemberAttribute(Name = "TerminateActivitiesResponse", Namespace = "http://schemas.ggf.org/bes/2006/08/bes-factory", Order = 0)]
        public TerminateActivitiesResponseType TerminateActivitiesResponse1;

        /// <summary>
        /// Initializes a new instance of the <see cref="TerminateActivitiesResponse"/> class.
        /// </summary>
        public TerminateActivitiesResponse()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TerminateActivitiesResponse"/> class.
        /// </summary>
        /// <param name="TerminateActivitiesResponse1">The terminate activities response1.</param>
        public TerminateActivitiesResponse(TerminateActivitiesResponseType TerminateActivitiesResponse1)
        {
            this.TerminateActivitiesResponse1 = TerminateActivitiesResponse1;
        }
    }
}
