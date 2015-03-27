//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Site.SubmissionService
{
    using System;
    using System.ComponentModel.Composition.Hosting;
    using Microsoft.EMIC.Cloud.GenericWorker;
    using Microsoft.EMIC.Cloud.Utilities;

    public class SecureSubmissionServiceFactory : SecureVenusServiceFactoryBase<BESFactoryPortTypeImplService>
    {
        public override Type ServiceType()
        {
            return SubmissionServiceConfig.ServiceType();
        }

        public override Type ServiceInterfaceType()
        {
            return SubmissionServiceConfig.ServiceInterfaceType();
        }

        public override CompositionContainer CreateCompositionContainer()
        {
            return SubmissionServiceConfig.CreateCompositionContainer();
        }
    }
}