//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.ComponentModel.Composition.Hosting;
using System.Threading;
using Microsoft.EMIC.Cloud.Demo;
using Microsoft.EMIC.Cloud.GenericWorker;
using Microsoft.EMIC.Cloud.GenericWorker.AzureProvider;
using Microsoft.EMIC.Cloud.Storage.Azure;

namespace WorkerHost
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Generic Worker for Windows Server";

            var cts = new CancellationTokenSource();

            // bring up one worker task
            var containerGW = new CompositionContainer(new AggregateCatalog(
                new TypeCatalog(typeof(CloudSettings)),
                new AssemblyCatalog(typeof(FilesystemMapper).Assembly),
                new AssemblyCatalog(typeof(AzureGWRuntimeEnvironment).Assembly),
                new AssemblyCatalog(typeof(AzureArgumentSingleReference).Assembly)
                ));
            var driver = containerGW.GetExportedValue<GenericWorkerDriver>();

            var submissionPoint = containerGW.GetExportedValue<BESFactoryPortTypeImplService>();
            if (submissionPoint == null)
            {
                throw new Exception("genericworkersubmission service cannot be extracted from the container");
            }
            
            var localSubmissionHost = new LocalJobSubmissionHost(containerGW);
            localSubmissionHost.Open();

            Console.WriteLine("Starting driver now...");
            driver.Run(cts.Token);

            Console.WriteLine("Press <return> to close");
            Console.ReadLine();
            localSubmissionHost.Close();
            cts.Cancel();
        }
    }
}
