//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OGF.BES;

namespace OGF.BES.COMPSsExtensions
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "4.0.30319.1")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.ggf.org/bes/2006/08/bes-factory")]
    [System.Xml.Serialization.XmlRootAttribute("UpdateInstances", Namespace = "http://schemas.ggf.org/bes/2006/08/bes-factory", IsNullable = false)]
    public partial class UpdateInstancesType
    {

        private EndpointReferenceType[] activityIdentifierField;

        private string minVMsField;

        private string maxVMsField;

        private System.Xml.XmlElement[] anyField;

        private System.Xml.XmlAttribute[] anyAttrField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("ActivityIdentifier", Order = 0)]
        public EndpointReferenceType[] ActivityIdentifier
        {
            get
            {
                return this.activityIdentifierField;
            }
            set
            {
                this.activityIdentifierField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string minVMs
        {
            get
            {
                return this.minVMsField;
            }
            set
            {
                this.minVMsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string maxVMs
        {
            get
            {
                return this.maxVMsField;
            }
            set
            {
                this.maxVMsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute(Order = 3)]
        public System.Xml.XmlElement[] Any
        {
            get
            {
                return this.anyField;
            }
            set
            {
                this.anyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAnyAttributeAttribute()]
        public System.Xml.XmlAttribute[] AnyAttr
        {
            get
            {
                return this.anyAttrField;
            }
            set
            {
                this.anyAttrField = value;
            }
        }
    }
}
