//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Site.JobListingService
{
    using System;
    using System.ComponentModel.Composition.Hosting;
    using Microsoft.EMIC.Cloud.GenericWorker;
    using Microsoft.EMIC.Cloud.Utilities;
    using Microsoft.EMIC.Cloud.GenericWorker.AzureProvider;

    public class UnprotectedScalingServiceFactory : UnprotectedVenusServiceFactoryBase<ScalingServiceImpl>
    {
        public override Type ServiceType() 
        {
            return JobListingServiceConfig.ServiceType();
        }

        public override Type ServiceInterfaceType()
        {
            return JobListingServiceConfig.ServiceInterfaceType();
        }

        public override CompositionContainer CreateCompositionContainer()
        {
            return JobListingServiceConfig.CreateCompositionContainer();
        }
    }
}