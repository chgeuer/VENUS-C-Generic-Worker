//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Site.ScalingService
{
    using System;
    using System.ComponentModel.Composition.Hosting;
    using Microsoft.EMIC.Cloud;
    using Microsoft.EMIC.Cloud.GenericWorker;
    using Microsoft.EMIC.Cloud.GenericWorker.AzureProvider;

    public class ScalingServiceConfig
    {
        public static Type ServiceType()
        {
            return typeof(ScalingServiceImpl); //later: ScalingServiceImpl :provides all Deployments MEF over ScalingPlugins/Provider
        }

        public static Type ServiceInterfaceType()
        {
            return typeof(IScalingService);
        }

        public static CompositionContainer CreateCompositionContainer()
        {
            return new CompositionContainer(new AggregateCatalog(
                            new AssemblyCatalog(typeof(CompositionIdentifiers).Assembly), // Main assembly
                            new TypeCatalog(typeof(OnPremisesSettings.OnPremisesSettings)), // run on premises
                            new AssemblyCatalog(typeof(AzureGWRuntimeEnvironment).Assembly) //Azure Scaling Plugin
                            ));
        } 
    }
}