//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OGF.BES.Managememt
{
    /// <summary>
    /// Response type returned by the <see cref="BESManagementService.StopAcceptingNewActivities"/> method
    /// </summary>
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(IsWrapped = false)]
    public partial class StopAcceptingNewActivitiesResponse
    {

        /// <summary>
        /// An instance of <see cref="StopAcceptingNewActivitiesResponseType"/> class associated with this request
        /// </summary>
        [System.ServiceModel.MessageBodyMemberAttribute(Name = "StopAcceptingNewActivitiesResponse", Namespace = "http://schemas.ggf.org/bes/2006/08/bes-management", Order = 0)]
        public StopAcceptingNewActivitiesResponseType StopAcceptingNewActivitiesResponse1;

        /// <summary>
        /// Initializes a new instance of the <see cref="StopAcceptingNewActivitiesResponse"/> class.
        /// </summary>
        public StopAcceptingNewActivitiesResponse()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StopAcceptingNewActivitiesResponse"/> class.
        /// </summary>
        /// <param name="StopAcceptingNewActivitiesResponse1">The stop accepting new activities response1.</param>
        public StopAcceptingNewActivitiesResponse(StopAcceptingNewActivitiesResponseType StopAcceptingNewActivitiesResponse1)
        {
            this.StopAcceptingNewActivitiesResponse1 = StopAcceptingNewActivitiesResponse1;
        }
    }
}
