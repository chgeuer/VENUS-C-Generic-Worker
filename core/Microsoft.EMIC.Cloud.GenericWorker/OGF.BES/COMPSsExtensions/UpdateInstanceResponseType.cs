//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OGF.BES.Faults;
using OGF.BES;

namespace OGF.BES.COMPSsExtensions
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "4.0.30319.1")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.ggf.org/bes/2006/08/bes-factory")]
    [System.Xml.Serialization.XmlRootAttribute("UpdateInstanceResponse", Namespace = "http://schemas.ggf.org/bes/2006/08/bes-factory", IsNullable = false)]
    public partial class UpdateInstanceResponseType : object, System.ComponentModel.INotifyPropertyChanged
    {

        private EndpointReferenceType activityIdentifierField;

        private bool updatedField;

        private bool updatedFieldSpecified;

        private Fault faultField;

        private List<System.Xml.XmlElement> anyField;

        private List<System.Xml.XmlAttribute> anyAttrField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public EndpointReferenceType ActivityIdentifier
        {
            get
            {
                return this.activityIdentifierField;
            }
            set
            {
                this.activityIdentifierField = value;
                this.RaisePropertyChanged("ActivityIdentifier");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public bool Updated
        {
            get
            {
                return this.updatedField;
            }
            set
            {
                this.updatedField = value;
                this.RaisePropertyChanged("Updated");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool UpdatedSpecified
        {
            get
            {
                return this.updatedFieldSpecified;
            }
            set
            {
                this.updatedFieldSpecified = value;
                this.RaisePropertyChanged("UpdatedSpecified");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public Fault Fault
        {
            get
            {
                return this.faultField;
            }
            set
            {
                this.faultField = value;
                this.RaisePropertyChanged("Fault");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute(Order = 3)]
        public List<System.Xml.XmlElement> Any
        {
            get
            {
                return this.anyField;
            }
            set
            {
                this.anyField = value;
                this.RaisePropertyChanged("Any");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAnyAttributeAttribute()]
        public List<System.Xml.XmlAttribute> AnyAttr
        {
            get
            {
                return this.anyAttrField;
            }
            set
            {
                this.anyAttrField = value;
                this.RaisePropertyChanged("AnyAttr");
            }
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the property changed.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        protected void RaisePropertyChanged(string propertyName)
        {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null))
            {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
