//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





using System.Collections.Generic;
namespace OGF.BES
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "4.0.30319.1")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.ggf.org/bes/2006/08/bes-factory")]
    [System.Xml.Serialization.XmlRootAttribute("FactoryResourceAttributesDocument", Namespace = "http://schemas.ggf.org/bes/2006/08/bes-factory", IsNullable = false)]
    public partial class FactoryResourceAttributesDocumentType : object, System.ComponentModel.INotifyPropertyChanged
    {

        private BasicResourceAttributesDocumentType basicResourceAttributesDocumentField;

        private bool isAcceptingNewActivitiesField;

        private string commonNameField;

        private string longDescriptionField;

        private long totalNumberOfActivitiesField;

        private List<EndpointReferenceType> activityReferenceField;

        private long totalNumberOfContainedResourcesField;

        private List<object> containedResourceField;

        private List<string> namingProfileField;

        private List<string> bESExtensionField;

        private string localResourceManagerTypeField;

        private List<System.Xml.XmlElement> anyField;

        private List<System.Xml.XmlAttribute> anyAttrField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public BasicResourceAttributesDocumentType BasicResourceAttributesDocument
        {
            get
            {
                return this.basicResourceAttributesDocumentField;
            }
            set
            {
                this.basicResourceAttributesDocumentField = value;
                this.RaisePropertyChanged("BasicResourceAttributesDocument");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public bool IsAcceptingNewActivities
        {
            get
            {
                return this.isAcceptingNewActivitiesField;
            }
            set
            {
                this.isAcceptingNewActivitiesField = value;
                this.RaisePropertyChanged("IsAcceptingNewActivities");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string CommonName
        {
            get
            {
                return this.commonNameField;
            }
            set
            {
                this.commonNameField = value;
                this.RaisePropertyChanged("CommonName");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public string LongDescription
        {
            get
            {
                return this.longDescriptionField;
            }
            set
            {
                this.longDescriptionField = value;
                this.RaisePropertyChanged("LongDescription");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
        public long TotalNumberOfActivities
        {
            get
            {
                return this.totalNumberOfActivitiesField;
            }
            set
            {
                this.totalNumberOfActivitiesField = value;
                this.RaisePropertyChanged("TotalNumberOfActivities");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("ActivityReference", Order = 5)]
        public List<EndpointReferenceType> ActivityReference
        {
            get
            {
                return this.activityReferenceField;
            }
            set
            {
                this.activityReferenceField = value;
                this.RaisePropertyChanged("ActivityReference");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 6)]
        public long TotalNumberOfContainedResources
        {
            get
            {
                return this.totalNumberOfContainedResourcesField;
            }
            set
            {
                this.totalNumberOfContainedResourcesField = value;
                this.RaisePropertyChanged("TotalNumberOfContainedResources");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("ContainedResource", Order = 7)]
        public List<object> ContainedResource
        {
            get
            {
                return this.containedResourceField;
            }
            set
            {
                this.containedResourceField = value;
                this.RaisePropertyChanged("ContainedResource");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("NamingProfile", DataType = "anyURI", Order = 8)]
        public List<string> NamingProfile
        {
            get
            {
                return this.namingProfileField;
            }
            set
            {
                this.namingProfileField = value;
                this.RaisePropertyChanged("NamingProfile");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("BESExtension", DataType = "anyURI", Order = 9)]
        public List<string> BESExtension
        {
            get
            {
                return this.bESExtensionField;
            }
            set
            {
                this.bESExtensionField = value;
                this.RaisePropertyChanged("BESExtension");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "anyURI", Order = 10)]
        public string LocalResourceManagerType
        {
            get
            {
                return this.localResourceManagerTypeField;
            }
            set
            {
                this.localResourceManagerTypeField = value;
                this.RaisePropertyChanged("LocalResourceManagerType");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute(Order = 11)]
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