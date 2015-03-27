//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





using OGF.BES.Faults;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using System.Collections.Generic;
namespace OGF.BES
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "4.0.30319.1")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.ggf.org/bes/2006/08/bes-factory")]
    [System.Xml.Serialization.XmlRootAttribute("CreateActivityResponse", Namespace = "http://schemas.ggf.org/bes/2006/08/bes-factory", IsNullable = false)]
    public partial class CreateActivityResponseType : object, System.ComponentModel.INotifyPropertyChanged
    {

        private EndpointReferenceType activityIdentifierField;

        private ActivityDocumentType activityDocumentField;

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
        public ActivityDocumentType ActivityDocument
        {
            get
            {
                return this.activityDocumentField;
            }
            set
            {
                this.activityDocumentField = value;
                this.RaisePropertyChanged("ActivityDocument");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute(Order = 2)]
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