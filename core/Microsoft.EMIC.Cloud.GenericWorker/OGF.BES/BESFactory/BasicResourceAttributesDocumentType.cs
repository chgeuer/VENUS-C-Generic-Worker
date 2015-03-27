//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





using OGF.JDSL;
using System.Collections.Generic;

namespace OGF.BES
{

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "4.0.30319.1")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.ggf.org/bes/2006/08/bes-factory")]
    [System.Xml.Serialization.XmlRootAttribute("BasicResourceAttributesDocument", Namespace = "http://schemas.ggf.org/bes/2006/08/bes-factory", IsNullable = false)]
    public partial class BasicResourceAttributesDocumentType : object, System.ComponentModel.INotifyPropertyChanged
    {

        private string resourceNameField;

        private OperatingSystem_Type operatingSystemField;

        private CPUArchitecture_Type cPUArchitectureField;

        private double cPUCountField;

        private bool cPUCountFieldSpecified;

        private double cPUSpeedField;

        private bool cPUSpeedFieldSpecified;

        private double physicalMemoryField;

        private bool physicalMemoryFieldSpecified;

        private double virtualMemoryField;

        private bool virtualMemoryFieldSpecified;

        private List<System.Xml.XmlElement> anyField;

        private List<System.Xml.XmlAttribute> anyAttrField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string ResourceName
        {
            get
            {
                return this.resourceNameField;
            }
            set
            {
                this.resourceNameField = value;
                this.RaisePropertyChanged("ResourceName");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public OperatingSystem_Type OperatingSystem
        {
            get
            {
                return this.operatingSystemField;
            }
            set
            {
                this.operatingSystemField = value;
                this.RaisePropertyChanged("OperatingSystem");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public CPUArchitecture_Type CPUArchitecture
        {
            get
            {
                return this.cPUArchitectureField;
            }
            set
            {
                this.cPUArchitectureField = value;
                this.RaisePropertyChanged("CPUArchitecture");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public double CPUCount
        {
            get
            {
                return this.cPUCountField;
            }
            set
            {
                this.cPUCountField = value;
                this.RaisePropertyChanged("CPUCount");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool CPUCountSpecified
        {
            get
            {
                return this.cPUCountFieldSpecified;
            }
            set
            {
                this.cPUCountFieldSpecified = value;
                this.RaisePropertyChanged("CPUCountSpecified");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
        public double CPUSpeed
        {
            get
            {
                return this.cPUSpeedField;
            }
            set
            {
                this.cPUSpeedField = value;
                this.RaisePropertyChanged("CPUSpeed");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool CPUSpeedSpecified
        {
            get
            {
                return this.cPUSpeedFieldSpecified;
            }
            set
            {
                this.cPUSpeedFieldSpecified = value;
                this.RaisePropertyChanged("CPUSpeedSpecified");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 5)]
        public double PhysicalMemory
        {
            get
            {
                return this.physicalMemoryField;
            }
            set
            {
                this.physicalMemoryField = value;
                this.RaisePropertyChanged("PhysicalMemory");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool PhysicalMemorySpecified
        {
            get
            {
                return this.physicalMemoryFieldSpecified;
            }
            set
            {
                this.physicalMemoryFieldSpecified = value;
                this.RaisePropertyChanged("PhysicalMemorySpecified");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 6)]
        public double VirtualMemory
        {
            get
            {
                return this.virtualMemoryField;
            }
            set
            {
                this.virtualMemoryField = value;
                this.RaisePropertyChanged("VirtualMemory");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool VirtualMemorySpecified
        {
            get
            {
                return this.virtualMemoryFieldSpecified;
            }
            set
            {
                this.virtualMemoryFieldSpecified = value;
                this.RaisePropertyChanged("VirtualMemorySpecified");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute(Order = 7)]
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