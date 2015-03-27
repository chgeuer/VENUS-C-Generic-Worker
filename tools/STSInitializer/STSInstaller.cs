//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Configuration;


namespace STSInitializer
{
    [RunInstaller(true)]
    public partial class STSInstaller : Installer
    {
        public STSInstaller()
        {
            InitializeComponent();
        }

        public override void Install(System.Collections.IDictionary stateSaver)
        {
            //System.Diagnostics.Debugger.Launch();
            base.Install(stateSaver);
 
            string targetDirectory = Context.Parameters["targetdir"];
 
            string param1 = Context.Parameters["Param1"];
 
            string param2 = Context.Parameters["Param2"];
            
            //System.Diagnostics.Debugger.Break();

            string exePath = string.Format("{0}\\STSInitializer.exe", targetDirectory);
 
            //System.Diagnostics.Debugger.Break();

            Configuration config = ConfigurationManager.OpenExeConfiguration(exePath);
 
            config.AppSettings.Settings["STSOnAzureConnectionString"].Value = param1;
 
            config.AppSettings.Settings["DevelopmentSecurityTokenServiceUserTableName"].Value = param2;
 
            config.Save();
        }
    }
}
