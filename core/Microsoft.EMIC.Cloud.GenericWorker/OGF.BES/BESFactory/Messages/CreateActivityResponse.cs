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
    /// Wrapper for Create Activity Response
    /// </summary>
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.MessageContractAttribute(IsWrapped = false)]
    public partial class CreateActivityResponse
    {

        /// <summary>
        /// An instance of <see cref="CreateActivityResponseType"/> class associated with this request
        /// </summary>
        [System.ServiceModel.MessageBodyMemberAttribute(Name = "CreateActivityResponse", Namespace = "http://schemas.ggf.org/bes/2006/08/bes-factory", Order = 0)]
        public CreateActivityResponseType CreateActivityResponse1;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateActivityResponse"/> class.
        /// </summary>
        public CreateActivityResponse()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateActivityResponse"/> class.
        /// </summary>
        /// <param name="CreateActivityResponse1">The create activity response1.</param>
        public CreateActivityResponse(CreateActivityResponseType CreateActivityResponse1)
        {
            this.CreateActivityResponse1 = CreateActivityResponse1;
        }
    }
}
