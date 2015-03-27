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
    [System.Xml.Serialization.XmlRootAttribute("DiskSpace", Namespace = "http://schemas.ggf.org/jsdl/2005/11/jsdl", IsNullable = false)]
    public partial class RangeValue_Type : object, System.ComponentModel.INotifyPropertyChanged
    {

        private Boundary_Type upperBoundedRangeField;

        private Boundary_Type lowerBoundedRangeField;

        private List<Exact_Type> exactField;

        private List<Range_Type> rangeField;

        private List<System.Xml.XmlAttribute> anyAttrField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public Boundary_Type UpperBoundedRange
        {
            get
            {
                return this.upperBoundedRangeField;
            }
            set
            {
                this.upperBoundedRangeField = value;
                this.RaisePropertyChanged("UpperBoundedRange");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
        public Boundary_Type LowerBoundedRange
        {
            get
            {
                return this.lowerBoundedRangeField;
            }
            set
            {
                this.lowerBoundedRangeField = value;
                this.RaisePropertyChanged("LowerBoundedRange");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Exact", Order = 2)]
        public List<Exact_Type> Exact
        {
            get
            {
                return this.exactField;
            }
            set
            {
                this.exactField = value;
                this.RaisePropertyChanged("Exact");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Range", Order = 3)]
        public List<Range_Type> Range
        {
            get
            {
                return this.rangeField;
            }
            set
            {
                this.rangeField = value;
                this.RaisePropertyChanged("Range");
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