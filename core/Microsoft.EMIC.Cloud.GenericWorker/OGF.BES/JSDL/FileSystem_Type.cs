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
    [System.Xml.Serialization.XmlRootAttribute("FileSystem", Namespace = "http://schemas.ggf.org/jsdl/2005/11/jsdl", IsNullable = false)]
    public partial class FileSystem_Type : object, System.ComponentModel.INotifyPropertyChanged
    {

        private FileSystemTypeEnumeration fileSystemTypeField;

        private bool fileSystemTypeFieldSpecified;

        private string descriptionField;

        private string mountPointField;

        private RangeValue_Type diskSpaceField;

        private List<System.Xml.XmlElement> anyField;

        private string nameField;

        private List<System.Xml.XmlAttribute> anyAttrField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public FileSystemTypeEnumeration FileSystemType
        {
            get
            {
                return this.fileSystemTypeField;
            }
            set
            {
                this.fileSystemTypeField = value;
                this.RaisePropertyChanged("FileSystemType");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool FileSystemTypeSpecified
        {
            get
            {
                return this.fileSystemTypeFieldSpecified;
            }
            set
            {
                this.fileSystemTypeFieldSpecified = value;
                this.RaisePropertyChanged("FileSystemTypeSpecified");
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
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string MountPoint
        {
            get
            {
                return this.mountPointField;
            }
            set
            {
                this.mountPointField = value;
                this.RaisePropertyChanged("MountPoint");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public RangeValue_Type DiskSpace
        {
            get
            {
                return this.diskSpaceField;
            }
            set
            {
                this.diskSpaceField = value;
                this.RaisePropertyChanged("DiskSpace");
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