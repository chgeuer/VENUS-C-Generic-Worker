//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OGF.JSDL_HPCPA
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "4.0.30319.1")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.ggf.org/jsdl/2006/07/jsdl-hpcpa")]
    [System.Xml.Serialization.XmlRootAttribute("HPCProfileApplication", Namespace = "http://schemas.ggf.org/jsdl/2006/07/jsdl-hpcpa", IsNullable = false)]
    public partial class HPCProfileApplication_Type : object, System.ComponentModel.INotifyPropertyChanged
    {

        private string executableField;

        private List<string> argumentField;

        private string inputField;

        private string outputField;

        private string errorField;

        private string workingDirectoryField;

        private List<Environment_Type> environmentField;

        private string userNameField;

        private string nameField;

        private List<System.Xml.XmlAttribute> anyAttrField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string Executable
        {
            get
            {
                return this.executableField;
            }
            set
            {
                this.executableField = value;
                this.RaisePropertyChanged("Executable");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Argument", Order = 1)]
        public List<string> Argument
        {
            get
            {
                return this.argumentField;
            }
            set
            {
                this.argumentField = value;
                this.RaisePropertyChanged("Argument");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 2)]
        public string Input
        {
            get
            {
                return this.inputField;
            }
            set
            {
                this.inputField = value;
                this.RaisePropertyChanged("Input");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public string Output
        {
            get
            {
                return this.outputField;
            }
            set
            {
                this.outputField = value;
                this.RaisePropertyChanged("Output");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 4)]
        public string Error
        {
            get
            {
                return this.errorField;
            }
            set
            {
                this.errorField = value;
                this.RaisePropertyChanged("Error");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 5)]
        public string WorkingDirectory
        {
            get
            {
                return this.workingDirectoryField;
            }
            set
            {
                this.workingDirectoryField = value;
                this.RaisePropertyChanged("WorkingDirectory");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Environment", Order = 6)]
        public List<Environment_Type> Environment
        {
            get
            {
                return this.environmentField;
            }
            set
            {
                this.environmentField = value;
                this.RaisePropertyChanged("Environment");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order = 7)]
        public string UserName
        {
            get
            {
                return this.userNameField;
            }
            set
            {
                this.userNameField = value;
                this.RaisePropertyChanged("UserName");
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