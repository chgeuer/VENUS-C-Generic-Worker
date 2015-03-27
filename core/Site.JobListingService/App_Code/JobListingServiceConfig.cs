//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Site.JobListingService
{
    using System;
    using System.ComponentModel.Composition.Hosting;
    using Microsoft.EMIC.Cloud;
    using Microsoft.EMIC.Cloud.AzureSettings;
    using Microsoft.EMIC.Cloud.GenericWorker;
    using Microsoft.EMIC.Cloud.GenericWorker.AzureProvider;

    public class JobListingServiceConfig
    {
        public static Type ServiceType()
        {
            return typeof(JobListingService); 
        }

        public static Type ServiceInterfaceType()
        {
            return typeof(IJobListingService);
        }

        public static CompositionContainer CreateCompositionContainer()
        {
            return new CompositionContainer(new AggregateCatalog(
                            new AssemblyCatalog(typeof(CompositionIdentifiers).Assembly), // Main assembly
                            new TypeCatalog(typeof(AzureSettingsProvider)), // run on Azure
                            new AssemblyCatalog(typeof(AzureGWRuntimeEnvironment).Assembly) 
                            ));
        } 
    }
}