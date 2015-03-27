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
    /// Wrapper for Factory Attributes Document Response
    /// </summary>
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.MessageContractAttribute(IsWrapped = false)]
    public partial class GetFactoryAttributesDocumentResponse
    {

        /// <summary>
        /// An instance of <see cref="GetFactoryAttributesDocumentResponseType"/> class associated with this request
        /// </summary>
        [System.ServiceModel.MessageBodyMemberAttribute(Name = "GetFactoryAttributesDocumentResponse", Namespace = "http://schemas.ggf.org/bes/2006/08/bes-factory", Order = 0)]
        public GetFactoryAttributesDocumentResponseType GetFactoryAttributesDocumentResponse1;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetFactoryAttributesDocumentResponse"/> class.
        /// </summary>
        public GetFactoryAttributesDocumentResponse()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetFactoryAttributesDocumentResponse"/> class.
        /// </summary>
        /// <param name="GetFactoryAttributesDocumentResponse1">The get factory attributes document response1.</param>
        public GetFactoryAttributesDocumentResponse(GetFactoryAttributesDocumentResponseType GetFactoryAttributesDocumentResponse1)
        {
            this.GetFactoryAttributesDocumentResponse1 = GetFactoryAttributesDocumentResponse1;
        }
    }
}
