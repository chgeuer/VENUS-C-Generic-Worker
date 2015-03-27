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
using ConfigurationModifier.ViewModel;
using System.ComponentModel.Composition.Hosting;
using ConfigurationModifier.Data;

namespace ConfigurationModifier
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CompositionContainer _container = null;

        public MainWindow()
        {
            this.InitializeComponent();
            _container = new CompositionContainer(new AggregateCatalog(
                new AssemblyCatalog(typeof(XMLHandler).Assembly)
               ));

            this.DataContext = _container.GetExportedValue<MainViewModel>();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            App.Current.Shutdown();
        }
    }
}
