//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





using System.Collections.Generic;
namespace OGF.JDSL
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "4.0.30319.1")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.ggf.org/jsdl/2005/11/jsdl")]
    [System.Xml.Serialization.XmlRootAttribute("JobIdentification", Namespace = "http://schemas.ggf.org/jsdl/2005/11/jsdl", IsNullable = false)]
    public partial class JobIdentification_Type : object, System.ComponentModel.INotifyPropertyChanged
    {

        private string jobNameField;

        private string descriptionField;

        private List<string> jobAnnotationField;

        private List<string> jobProjectField;

        private List<System.Xml.XmlElement> anyField;

        private List<System.Xml.XmlAttribute> anyAttrField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string JobName
        {
            get
            {
                return this.jobNameField;
            }
            set
            {
                this.jobNameField = value;
                this.RaisePropertyChanged("JobName");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public string Description
        {
            get
            {
                return this.descriptionField;
            }
            set
            {
                this.descriptionField = value;
                this.RaisePropertyChanged("Description");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("JobAnnotation", Order = 2)]
        public List<string> JobAnnotation
        {
            get
            {
                return this.jobAnnotationField;
            }
            set
            {
                this.jobAnnotationField = value;
                this.RaisePropertyChanged("JobAnnotation");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("JobProject", Order = 3)]
        public List<string> JobProject
        {
            get
            {
                return this.jobProjectField;
            }
            set
            {
                this.jobProjectField = value;
                this.RaisePropertyChanged("JobProject");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute(Order = 4)]
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