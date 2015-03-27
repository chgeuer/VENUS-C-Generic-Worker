//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using Microsoft.EMIC.Cloud.Demo;
using Microsoft.EMIC.Cloud.GenericWorker.AzureProvider;
using Microsoft.EMIC.Cloud.GenericWorker;
using Microsoft.EMIC.Cloud.DataManagement;
using Microsoft.EMIC.Cloud.Storage.Azure;
using Microsoft.EMIC.Cloud.Utilities;
using Microsoft.IdentityModel.Tokens;
using OGF.BES;
using System.ServiceModel.Channels;

namespace ProgrammingModelEnactmentServiceHost
{
    class ProgrammingModelEnactmentServiceHost
    {
        static void Main(string[] args)
        {
            Console.Title = "Job Submission Service";

            ServiceHost genericWorkerHost = new ServiceHost(typeof(BESFactoryPortTypeImplService));
            var m_container = new CompositionContainer(new AggregateCatalog(
                new TypeCatalog(typeof(CloudSettings)),
                new AssemblyCatalog(typeof(BESFactoryPortTypeImplService).Assembly),
                new AssemblyCatalog(typeof(AzureGWRuntimeEnvironment).Assembly),
                new AssemblyCatalog(typeof(AzureArgumentSingleReference).Assembly)
                ));
            var d = m_container.GetExportedValue<GenericWorkerDriver>();
            var d2 = m_container.GetExportedValue<ArgumentRepository>()
                .Arguments.Select(i => i.Metadata.Type.Name).ToList();

            genericWorkerHost.Description.Behaviors.Add(new MyServiceBehavior<BESFactoryPortTypeImplService>(m_container));

            //HACK Security is Off
            //genericWorkerHost.AddServiceEndpoint(
            //    typeof(IBESFactoryPortType), 
            //    new WS2007FederationHttpBinding("submissionServiceBinding"), 
            //    CloudSettings.GenericWorkerUrl);

            genericWorkerHost.AddServiceEndpoint(
                typeof(BESFactoryPortType),
                new WS2007HttpBinding(SecurityMode.None, reliableSessionEnabled: false),
                CloudSettings.GenericWorkerUrl);

            genericWorkerHost.Description.Behaviors.Find<ServiceDebugBehavior>().IncludeExceptionDetailInFaults = true;
            FederatedServiceCredentials.ConfigureServiceHost(genericWorkerHost); //, new ServiceConfiguration() { IssuerNameRegistry = ir });
            genericWorkerHost.Open();

            Console.Write("ProgrammingModelEnactmentServiceHost is running");
            Console.ReadLine();

            genericWorkerHost.Close();
        }
    }
}