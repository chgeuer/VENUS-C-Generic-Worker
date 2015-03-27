//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.IO;
using System.Xml.Linq;
using Microsoft.EMIC.Cloud;

namespace AzureVMRoleSettings
{
    public class VMRoleSettings
    {
        [Export(CompositionIdentifiers.ProcessIdentityFilename)]
        public static string ProcessIdentityFilename
        {
            get
            {
                var appConfigFilename = @"AdministratorHost\Microsoft.EMIC.Cloud.Administrator.Host.exe.config";

                var configXml = File.ReadAllText(appConfigFilename);
                var relPath = XDocument.Parse(configXml)
                    .Element("configuration")
                    .Element("appSettings")
                    .Elements("add")
                    .Where(e => e.Attribute("key").Value.Equals("identityFile"))
                    .First()
                    .Attribute("value").Value;

                var fileInfo = new FileInfo(Path.Combine(
                    new FileInfo(appConfigFilename).Directory.FullName,
                    relPath));

                return fileInfo.FullName;
            }
        }
    }
}
