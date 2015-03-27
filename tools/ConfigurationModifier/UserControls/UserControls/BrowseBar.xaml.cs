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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using Microsoft.Win32;

namespace UserControls
{
    /// <summary>
    /// Interaction logic for BrowseBar.xaml
    /// </summary>
    public partial class BrowseBar : UserControl
    {
        public static RoutedCommand BrowseCmd = new RoutedCommand();
        public static RoutedCommand ExecuteCmd = new RoutedCommand();

        private CancellationTokenSource cancellationTokenSource;

        public BrowseBar()
        {
            InitializeComponent();
        }

        private void Exectue(string fileName)
        {
            if (browseFunction != null)
            {
                try
                {
                    cancellationTokenSource = browseFunction(fileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    cancellationTokenSource = null;
                }
            }
        }

        private void ExecuteCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Exectue(textBoxPath.Text);
        }

        private void ExecuteCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            TextBox textbox = e.Source as TextBox;
            if (textbox != null && textbox.Name == textBoxPath.Name)
            {
                e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }
        }

        private void BrowseCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = filter,
                Multiselect = multiSelect
            };

            if (dlg.ShowDialog() == true)
            {
                textBoxPath.Text = dlg.FileNames.FirstOrDefault();
                Exectue(dlg.FileNames.FirstOrDefault());
            }
            else
            {
                cancellationTokenSource = null;
            }
        }

        private void BrowseCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }


        #region BrowseButton

        private string filter = "All files|*.*";
        public string Filter
        {
            get
            {
                return this.filter;
            }
            set
            {
                this.filter = value;
            }
        }

        private bool multiSelect = false;
        public bool MultiSelect
        {
            get
            {
                return this.multiSelect;
            }
            set
            {
                this.multiSelect = value;
            }
        }

        private string text = "Browse";
        public string Text
        {
            get
            {
                return this.text;
            }
            set
            {
                this.text = value;
                textBoxPath.Text = text;
            }
        }


        private Func<string, CancellationTokenSource> browseFunction;
        public Func<string, CancellationTokenSource> BrowseFunction
        {
            get
            {
                return this.browseFunction;
            }
            set
            {
                this.browseFunction = value;
            }
        }

        private void browseBtn_Click(object sender, RoutedEventArgs e)
        {

        }
        #endregion

        #region StopButton

        private void buttonStop_Click(object sender, RoutedEventArgs e)
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }
        }

        #endregion

        # region TextBox



        #endregion

    }
}
