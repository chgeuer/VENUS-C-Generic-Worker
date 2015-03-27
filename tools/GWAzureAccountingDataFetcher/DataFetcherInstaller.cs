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

namespace GWAzureAccountingDataFetcher
{
    [RunInstaller(true)]
    public partial class DataFetcherInstaller : System.Configuration.Install.Installer
    {
        public DataFetcherInstaller()
        {
            InitializeComponent();
        }

        public override void Install(System.Collections.IDictionary stateSaver)
        {
           // System.Diagnostics.Debugger.Launch();
            base.Install(stateSaver);

            string targetDirectory = Context.Parameters["targetdir"];

            string param1 = Context.Parameters["Param1"];

            string param2 = Context.Parameters["Param2"];

            string param3 = Context.Parameters["Param3"];
            //System.Diagnostics.Debugger.Break();

            string exePath = string.Format("{0}\\GWAzureAccountingDataFetcher.exe", targetDirectory);

            //System.Diagnostics.Debugger.Break();

            Configuration config = ConfigurationManager.OpenExeConfiguration(exePath);

            ConfigurationSectionGroup group = config.SectionGroups[@"userSettings"];

            ClientSettingsSection clientSection = group.Sections["AccountingDataFetcher.Properties.Settings"] as ClientSettingsSection;
           
            foreach (SettingElement s in clientSection.Settings)
            {
                if (s.Name == "GenericWorkerConnectionString")
                {
                    s.Value.ValueXml.InnerText = param1;
                    
                }
                else if (s.Name == "GenericWorkerAccountingTableName")
                {
                    s.Value.ValueXml.InnerText = param2;
                }
                else if (s.Name == "VmPostUrl")
                {
                    s.Value.ValueXml.InnerText = param3 + "/usage-tracker/rest/usagerecords/vm/";
                }
                else if (s.Name == "StoragePostUrl")
                {
                    s.Value.ValueXml.InnerText = param3 + "/usage-tracker/rest/usagerecords/storage/";
                }
                else if (s.Name == "NetworkPostUrl")
                {
                    s.Value.ValueXml.InnerText = param3 + "/usage-tracker/rest/usagerecords/network/";
                }
                else if (s.Name == "JobPostUrl")
                {
                    s.Value.ValueXml.InnerText = param3 + "/usage-tracker/rest/usagerecords/job/";
                }

                config.Save(ConfigurationSaveMode.Minimal, true);
            }

            config.Save();
        }
    }
}
