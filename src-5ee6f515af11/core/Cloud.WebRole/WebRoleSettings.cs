//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Xml.Linq;
using Microsoft.EMIC.Cloud;
using Microsoft.EMIC.Cloud.ApplicationRepository;
using Microsoft.EMIC.Cloud.DataManagement;

namespace Cloud.WebRole
{
    public class WebRoleSettings
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
}