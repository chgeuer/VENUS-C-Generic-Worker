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
    [System.Xml.Serialization.XmlRootAttribute("DataStaging", Namespace = "http://schemas.ggf.org/jsdl/2005/11/jsdl", IsNullable = false)]
    public partial class DataStaging_Type : object, System.ComponentModel.INotifyPropertyChanged
    {

        private string fileNameField;

        private string filesystemNameField;

        //private CreationFlagEnumeration creationFlagField;

        private bool deleteOnTerminationField;

        private bool deleteOnTerminationFieldSpecified;

        private SourceTarget_Type sourceField;

        private SourceTarget_Type targetField;

        private List<System.Xml.XmlElement> anyField;

        private string nameField;

        private List<System.Xml.XmlAttribute> anyAttrField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string FileName
        {
            get
            {
                return this.fileNameField;
            }
            set
            {
                this.fileNameField = value;
                this.RaisePropertyChanged("FileName");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "NCName", Order = 1)]
        public string FilesystemName
        {
            get
            {
                return this.filesystemNameField;
            }
            set
            {
                this.filesystemNameField = value;
                this.RaisePropertyChanged("FilesystemName");
            }
        }

        ///// <remarks/>
        //[System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        //public CreationFlagEnumeration CreationFlag
        //{
        //    get
        //    {
        //        return this.creationFlagField;
        //    }
        //    set
        //    {
        //        this.creationFlagField = value;
        //        this.RaisePropertyChanged("CreationFlag");
        //    }
        //}

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public bool DeleteOnTermination
        {
            get
            {
                return this.deleteOnTerminationField;
            }
            set
            {
                this.deleteOnTerminationField = value;
                this.RaisePropertyChanged("DeleteOnTermination");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool DeleteOnTerminationSpecified
        {
            get
            {
                return this.deleteOnTerminationFieldSpecified;
            }
            set
            {
                this.deleteOnTerminationFieldSpecified = value;
                this.RaisePropertyChanged("DeleteOnTerminationSpecified");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
        public SourceTarget_Type Source
        {
            get
            {
                return this.sourceField;
            }
            set
            {
                this.sourceField = value;
                this.RaisePropertyChanged("Source");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 5)]
        public SourceTarget_Type Target
        {
            get
            {
                return this.targetField;
            }
            set
            {
                this.targetField = value;
                this.RaisePropertyChanged("Target");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute(Order = 6)]
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
        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "NCName")]
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
                this.RaisePropertyChanged("name");
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