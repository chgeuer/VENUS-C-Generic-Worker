//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Site.SubmissionService
{
    using System;
    using System.ComponentModel.Composition.Hosting;
    using KTH.GenericWorker.CDMI;
    using Microsoft.EMIC.Cloud;
    using Microsoft.EMIC.Cloud.AzureSettings;
    using Microsoft.EMIC.Cloud.GenericWorker;
    using Microsoft.EMIC.Cloud.GenericWorker.AzureProvider;
    using Microsoft.EMIC.Cloud.Storage.Azure;
    using OGF.BES;

    public class SubmissionServiceConfig
    {
        public static Type ServiceType()
        {
            return typeof(BESFactoryPortTypeImplService);
        }

        public static Type ServiceInterfaceType()
        {
            return typeof(BESFactoryPortType);
        }

        public static CompositionContainer CreateCompositionContainer()
        {
            return new CompositionContainer(new AggregateCatalog(
                            new AssemblyCatalog(typeof(CompositionIdentifiers).Assembly), // Main assembly
                            new TypeCatalog(typeof(AzureSettingsProvider)), // run on Azure
                            new AssemblyCatalog(typeof(AzureGWRuntimeEnvironment).Assembly), // run on Azure
                            new TypeCatalog(typeof(SubmissionServiceConfig)),
                            new AssemblyCatalog(typeof(AzureBlobReference).Assembly), // Azure storage support
                            new AssemblyCatalog(typeof(CDMIBlobReference).Assembly) // CDMI storage support
                            ));
        }

        [System.ComponentModel.Composition.Export(CompositionIdentifiers.OGFWsdlPortName)]
        public static string PortName
        {
            get
            {
                string returnValue = null;
                CompositionIdentifiers.Constants.TryGetValue(CompositionIdentifiers.OGFWsdlPortName, out returnValue);
                return returnValue;
            }
        }
    }
}