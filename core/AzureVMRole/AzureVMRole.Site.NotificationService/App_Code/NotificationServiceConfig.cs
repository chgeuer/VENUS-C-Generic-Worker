//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Site.NotificationService
{
    using System;
    using System.ComponentModel.Composition.Hosting;
    using Microsoft.EMIC.Cloud;
    using Microsoft.EMIC.Cloud.AzureSettings;
    using Microsoft.EMIC.Cloud.GenericWorker;
    using Microsoft.EMIC.Cloud.GenericWorker.AzureProvider;

    public class NotificationServiceConfig
    {
        public static Type ServiceType()
        {
            return typeof(NotificationService); 
        }

        public static Type ServiceInterfaceType()
        {
            return typeof(INotificationService);
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