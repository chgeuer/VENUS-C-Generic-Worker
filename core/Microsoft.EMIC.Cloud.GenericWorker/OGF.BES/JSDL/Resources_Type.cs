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
    [System.Xml.Serialization.XmlRootAttribute("Resources", Namespace = "http://schemas.ggf.org/jsdl/2005/11/jsdl", IsNullable = false)]
    public partial class Resources_Type : object, System.ComponentModel.INotifyPropertyChanged
    {

        private List<string> candidateHostsField;

        private List<FileSystem_Type> fileSystemField;

        private bool exclusiveExecutionField;

        private bool exclusiveExecutionFieldSpecified;

        private OperatingSystem_Type operatingSystemField;

        private CPUArchitecture_Type cPUArchitectureField;

        private RangeValue_Type individualCPUSpeedField;

        private RangeValue_Type individualCPUTimeField;

        private RangeValue_Type individualCPUCountField;

        private RangeValue_Type individualNetworkBandwidthField;

        private RangeValue_Type individualPhysicalMemoryField;

        private RangeValue_Type individualVirtualMemoryField;

        private RangeValue_Type individualDiskSpaceField;

        private RangeValue_Type totalCPUTimeField;

        private RangeValue_Type totalCPUCountField;

        private RangeValue_Type totalPhysicalMemoryField;

        private RangeValue_Type totalVirtualMemoryField;

        private RangeValue_Type totalDiskSpaceField;

        private RangeValue_Type totalResourceCountField;

        private List<System.Xml.XmlElement> anyField;

        private List<System.Xml.XmlAttribute> anyAttrField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order = 0)]
        [System.Xml.Serialization.XmlArrayItemAttribute("HostName", IsNullable = false)]
        public List<string> CandidateHosts
        {
            get
            {
                return this.candidateHostsField;
            }
            set
            {
                this.candidateHostsField = value;
                this.RaisePropertyChanged("CandidateHosts");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("FileSystem", Order = 1)]
        public List<FileSystem_Type> FileSystem
        {
            get
            {
                return this.fileSystemField;
            }
            set
            {
                this.fileSystemField = value;
                this.RaisePropertyChanged("FileSystem");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public bool ExclusiveExecution
        {
            get
            {
                return this.exclusiveExecutionField;
            }
            set
            {
                this.exclusiveExecutionField = value;
                this.RaisePropertyChanged("ExclusiveExecution");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool ExclusiveExecutionSpecified
        {
            get
            {
                return this.exclusiveExecutionFieldSpecified;
            }
            set
            {
                this.exclusiveExecutionFieldSpecified = value;
                this.RaisePropertyChanged("ExclusiveExecutionSpecified");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
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
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
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
        [System.Xml.Serialization.XmlElementAttribute(Order = 5)]
        public RangeValue_Type IndividualCPUSpeed
        {
            get
            {
                return this.individualCPUSpeedField;
            }
            set
            {
                this.individualCPUSpeedField = value;
                this.RaisePropertyChanged("IndividualCPUSpeed");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 6)]
        public RangeValue_Type IndividualCPUTime
        {
            get
            {
                return this.individualCPUTimeField;
            }
            set
            {
                this.individualCPUTimeField = value;
                this.RaisePropertyChanged("IndividualCPUTime");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 7)]
        public RangeValue_Type IndividualCPUCount
        {
            get
            {
                return this.individualCPUCountField;
            }
            set
            {
                this.individualCPUCountField = value;
                this.RaisePropertyChanged("IndividualCPUCount");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 8)]
        public RangeValue_Type IndividualNetworkBandwidth
        {
            get
            {
                return this.individualNetworkBandwidthField;
            }
            set
            {
                this.individualNetworkBandwidthField = value;
                this.RaisePropertyChanged("IndividualNetworkBandwidth");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 9)]
        public RangeValue_Type IndividualPhysicalMemory
        {
            get
            {
                return this.individualPhysicalMemoryField;
            }
            set
            {
                this.individualPhysicalMemoryField = value;
                this.RaisePropertyChanged("IndividualPhysicalMemory");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 10)]
        public RangeValue_Type IndividualVirtualMemory
        {
            get
            {
                return this.individualVirtualMemoryField;
            }
            set
            {
                this.individualVirtualMemoryField = value;
                this.RaisePropertyChanged("IndividualVirtualMemory");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 11)]
        public RangeValue_Type IndividualDiskSpace
        {
            get
            {
                return this.individualDiskSpaceField;
            }
            set
            {
                this.individualDiskSpaceField = value;
                this.RaisePropertyChanged("IndividualDiskSpace");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 12)]
        public RangeValue_Type TotalCPUTime
        {
            get
            {
                return this.totalCPUTimeField;
            }
            set
            {
                this.totalCPUTimeField = value;
                this.RaisePropertyChanged("TotalCPUTime");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 13)]
        public RangeValue_Type TotalCPUCount
        {
            get
            {
                return this.totalCPUCountField;
            }
            set
            {
                this.totalCPUCountField = value;
                this.RaisePropertyChanged("TotalCPUCount");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 14)]
        public RangeValue_Type TotalPhysicalMemory
        {
            get
            {
                return this.totalPhysicalMemoryField;
            }
            set
            {
                this.totalPhysicalMemoryField = value;
                this.RaisePropertyChanged("TotalPhysicalMemory");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 15)]
        public RangeValue_Type TotalVirtualMemory
        {
            get
            {
                return this.totalVirtualMemoryField;
            }
            set
            {
                this.totalVirtualMemoryField = value;
                this.RaisePropertyChanged("TotalVirtualMemory");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 16)]
        public RangeValue_Type TotalDiskSpace
        {
            get
            {
                return this.totalDiskSpaceField;
            }
            set
            {
                this.totalDiskSpaceField = value;
                this.RaisePropertyChanged("TotalDiskSpace");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 17)]
        public RangeValue_Type TotalResourceCount
        {
            get
            {
                return this.totalResourceCountField;
            }
            set
            {
                this.totalResourceCountField = value;
                this.RaisePropertyChanged("TotalResourceCount");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute(Order = 18)]
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