//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.ComponentModel.Composition.Hosting;
using System.ServiceModel;
using System.ServiceModel.Description;
using Microsoft.EMIC.Cloud.Demo;
using Microsoft.EMIC.Cloud.ApplicationRepository;
using Microsoft.EMIC.Cloud.ApplicationRepository.AzureProvider;
using Microsoft.EMIC.Cloud.Utilities;
using Microsoft.IdentityModel.Tokens;

namespace AppStoreHost
{
    class Program
    {
        static ServiceHost appStoreHost;

        static void Main(string[] args)
        {
            Console.Title = "Application Repository";

            appStoreHost = new ServiceHost(typeof(AppStoreServiceImpl));
            var appStoreContainer = new CompositionContainer(new AggregateCatalog(
                new AssemblyCatalog(typeof(AppStoreServiceImpl).Assembly),
                new AssemblyCatalog(typeof(AppStoreOnAzureProvider).Assembly),
                new TypeCatalog(typeof(CloudSettings))
                ));
            appStoreHost.Description.Behaviors.Add(new MyServiceBehavior<AppStoreServiceImpl>(appStoreContainer));
            appStoreHost.Description.Behaviors.Find<ServiceDebugBehavior>().IncludeExceptionDetailInFaults = true;
            var smb = appStoreHost.Description.Behaviors.Find<ServiceMetadataBehavior>();
            if (smb == null)
            {
                smb = new ServiceMetadataBehavior();
                appStoreHost.Description.Behaviors.Add(smb);
            }
            smb.HttpGetEnabled = true;
            appStoreHost.AddServiceEndpoint(typeof(IAppStoreService), WCFUtils.CreateUnprotectedBinding(), CloudSettings.ApplicationStoreURL);
            FederatedServiceCredentials.ConfigureServiceHost(appStoreHost);
            appStoreHost.Open();

            Console.WriteLine("Host running. Press <Return> to close");
            Console.ReadLine();

            appStoreHost.Close();
        }
    }
}