//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using ConfigurationModifier.Models;

namespace ConfigurationModifier.ViewModel
{
    public class BooleanPageViewModel : WizardPageViewModelBase
    {
        public BooleanPageViewModel(PropertyInfo pInfo, Variables variables)
            : base(pInfo, variables)
        {

        }

        public override string DisplayName
        {
            get { return VariableName; }
        }

        internal override bool IsValid()
        {
            return true;
        }
    }
}
