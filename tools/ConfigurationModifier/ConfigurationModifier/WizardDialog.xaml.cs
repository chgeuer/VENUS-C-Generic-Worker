//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ConfigurationModifier.Command;
using ConfigurationModifier.ViewModel;
using System.ComponentModel.Composition;

namespace ConfigurationModifier
{
    /// <summary>
    /// Interaction logic for WizardDialog.xaml
    /// </summary>
    [Export(typeof(WizardDialog))]
    public partial class WizardDialog : Window, IPartImportsSatisfiedNotification
    {
#pragma warning disable 0649
        [Import(typeof(WizardViewModel))]
        WizardViewModel wizardViewModel;
#pragma warning restore 0649

        public bool Result { get; set; }

        public WizardDialog()
        {
            InitializeComponent();
        }

        public void OnImportsSatisfied()
        {
            wizardViewModel.RequestClose += this.OnViewModelRequestClose;
            base.DataContext = wizardViewModel;
        }

        void OnViewModelRequestClose(object sender, EventArgs e)
        {
            DialogEventArgs dlgAgs = e as DialogEventArgs;
            if (dlgAgs != null)
            {
                this.Result = dlgAgs.DialogResult;
            }
            else
            {
                this.Result = false;
            }
            base.Visibility = System.Windows.Visibility.Hidden;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            base.Visibility = System.Windows.Visibility.Hidden;
        }
    }
}
