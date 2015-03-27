//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConfigurationModifier.Command
{
    public class DialogEventArgs : EventArgs
    {
        public bool DialogResult { get; set; }

        public DialogEventArgs(bool dialogResultValue)
        {
            this.DialogResult = dialogResultValue;
        }
    }
}
