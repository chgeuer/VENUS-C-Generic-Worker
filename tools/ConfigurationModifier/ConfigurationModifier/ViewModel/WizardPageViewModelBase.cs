//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using ConfigurationModifier.Models;

namespace ConfigurationModifier.ViewModel
{
    /// <summary>
    /// Abstract base class for all pages shown in the wizard.
    /// </summary>
    public abstract class WizardPageViewModelBase : INotifyPropertyChanged
    {
        #region Fields

        bool isCurrentPage;

        PropertyInfo pInfo;
        Variables variables;

        #endregion 

        #region Constructor

        protected WizardPageViewModelBase(PropertyInfo pInfo, Variables variables)
        {
            this.pInfo = pInfo;
            this.variables = variables;
        }

        #endregion 

        #region Properties

        public string VariableName
        {
            get { return pInfo.Name; }
        }

        public object VariableValue
        {
            get { return pInfo.GetValue(variables, null); }
            set
            {
                if (pInfo.PropertyType == typeof(int))
                {
                    int intValue = 0;
                    int.TryParse(value.ToString(), out intValue);
                    pInfo.SetValue(variables, intValue, null);
                }
                else
                {
                    pInfo.SetValue(variables, value, null);
                }
            }
        }

        public abstract string DisplayName { get; }

        public bool IsCurrentPage
        {
            get { return isCurrentPage; }
            set
            {
                if (value == isCurrentPage)
                    return;

                isCurrentPage = value;
                this.OnPropertyChanged("IsCurrentPage");
            }
        }

        #endregion 

        #region Methods

        /// <summary>
        /// Returns true if the user has filled in this page properly
        /// and the wizard should allow the user to progress to the 
        /// next page in the workflow.
        /// </summary>
        internal abstract bool IsValid();

        #endregion 

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion 
    }
}
