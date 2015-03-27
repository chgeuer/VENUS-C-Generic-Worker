//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Microsoft.EMIC.Cloud.AzureSettings;
using Microsoft.EMIC.Cloud.GenericWorker.AzureProvider;
using Microsoft.EMIC.Cloud.GenericWorker;
using Microsoft.EMIC.Cloud.Storage.Azure;

namespace Microsoft.EMIC.Cloud
{
    class GenericWorkerAgent
    {
        static void Main()
        {
            var cts = new CancellationTokenSource();
            AppConfigFilename = @"C:\VENUS\D\OGF\Cloud\bin\Debug\Cloud.csx\roles\Cloud.WebRole\approot\bin\AdministratorHost\Microsoft.EMIC.Cloud.Administrator.Host.exe.config";

            var prog = new GenericWorkerAgent(cts);


            prog.Run();
        }

        private GenericWorkerDriver Driver { get; set; }

        private readonly CancellationTokenSource _cts;

        public GenericWorkerAgent(CancellationTokenSource cts)
        {
            this._cts = cts;

            var container = new CompositionContainer(new AggregateCatalog(
               new TypeCatalog(typeof(AzureSettingsProvider)),
               new TypeCatalog(typeof(GenericWorkerAgent)),
               new AssemblyCatalog(typeof(FilesystemMapper).Assembly),
               new AssemblyCatalog(typeof(AzureGWRuntimeEnvironment).Assembly),
               new AssemblyCatalog(typeof(AzureArgumentSingleReference).Assembly)
               ));

            this.Driver = container.GetExportedValue<GenericWorkerDriver>();
        }

        public void Run()
        {
            try
            {
                this.Driver.Run(this._cts.Token);
            }
            catch (Exception exception)
            {
                var errorMsg = new StringBuilder();
                for (var e = exception; e != null; e = e.InnerException)
                {
                    errorMsg.AppendLine(
                        string.Format("{0}: \"{1}\"",
                            e.GetType().Name, e.Message));
                }
                Trace.TraceError(errorMsg.ToString());

                _cts.Cancel();

                throw;
            }
        }


        public static string AppConfigFilename { get; set; }

        [Export(CompositionIdentifiers.ProcessIdentityFilename)]
        public static string ProcessIdentityFilename
        {
            get
            {
                // var appConfigFilename = @"AdministratorHost\Microsoft.EMIC.Cloud.Administrator.Host.exe.config";

                var configXml = File.ReadAllText(AppConfigFilename);
                var relPath = XDocument.Parse(configXml)
                    .Element("configuration")
                    .Element("appSettings")
                    .Elements("add")
                    .Where(e => e.Attribute("key").Value.Equals("identityFile"))
                    .First()
                    .Attribute("value").Value;

                var fileInfo = new FileInfo(Path.Combine(
                    new FileInfo(AppConfigFilename).Directory.FullName,
                    relPath));

                return fileInfo.FullName;
            }
        }
    }
}
