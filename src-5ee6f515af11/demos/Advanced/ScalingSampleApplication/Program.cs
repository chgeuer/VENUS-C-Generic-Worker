//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.EMIC.Cloud.GenericWorker;
using System.Configuration;
using System.ServiceModel;
using Microsoft.EMIC.Cloud.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.EMIC.Cloud.Utilities;

namespace ScalingSampleApplication
{
    class Program
    {
        enum Choice { No, Yes }

        static Choice AskUser(string question)
        {
            Console.Write(question);

            Predicate<char> isYes = c => c == 'y' || c == 'Y';
            Predicate<char> isNo = c => c == 'n' || c == 'N';
            Predicate<char> isValidChoice = c => isYes(c) || isNo(c);

            char choice = ' ';
            while (!isValidChoice(choice))
                choice = Console.ReadKey().KeyChar;

            Console.WriteLine();
            if (isYes(choice))
                return Choice.Yes;
            else
                return Choice.No;
        }

        static void Main(string[] args)
        {
            Action<ConsoleColor, string> print = (color, str) =>
            {
                Console.ForegroundColor = color;
                Console.Out.WriteLine(str);
                Console.ResetColor();
            };

            var authThumprint = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.AuthCert.Thumbprint"];
            var dnsEnpointId = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.AuthCert.DnsEndpointId"];

            Func<ScalingServiceClient> CreateSecureClient = () =>
            {
                var secScalingSvcUrl = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.SecureScalingService.URL"];
                print(ConsoleColor.Red, string.Format("Using ScalingService at: {0}", secScalingSvcUrl));
                var issuerAddress = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.STS.URL"];
                print(ConsoleColor.Red, string.Format("fetching tokens from {0}", issuerAddress));

                // In the development setting (when talking to localhost), we need to override WCF security checks
                // with the DnsEndpointIdentity.
                //

                return ScalingServiceClient.CreateSecureClient(
                    address: new EndpointAddress(new Uri(secScalingSvcUrl), new DnsEndpointIdentity(dnsEnpointId)),
                    issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                    username: "Administrator",
                    password: "secret",
                    serviceCert: X509Helper.GetX509Certificate2(
                        StoreLocation.LocalMachine, StoreName.My,
                        authThumprint,
                        X509FindType.FindByThumbprint));
            };
            Func<ScalingServiceClient> CreateUnprotectedClient = () =>
            {
                var unsecScalingSvcUrl = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.ScalingService.URL"];
                return ScalingServiceClient.CreateUnprotectedClient(unsecScalingSvcUrl);
            };

            Func<ScalingServiceClient> CreateUsernamePasswordSecuredScalingClient = () =>
            {
                var secureGenericWorkerURL = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.UsernamePasswordSecuredScalingService.URL"];
                ScalingServiceClient scalingPortal = new ScalingServiceClient(
                    WCFUtils.CreateUsernamePasswordSecurityTokenServiceBinding(),
                    new EndpointAddress(new Uri(secureGenericWorkerURL), new DnsEndpointIdentity(dnsEnpointId)));

                scalingPortal.ClientCredentials.UserName.UserName = "Hakan";
                scalingPortal.ClientCredentials.UserName.Password = "test123!";

                var serviceCert = X509Helper.LookupCertificate(
                    StoreLocation.LocalMachine, StoreName.My.ToString(),
                    authThumprint, X509FindType.FindByThumbprint);

                scalingPortal.ClientCredentials.ServiceCertificate.DefaultCertificate = serviceCert;
                return scalingPortal;
            };
            
            var scalingClient = CreateSecureClient();

            Action<List<DeploymentSize>> printDeps = (runningDeployments) =>
            {
                foreach (var dep in runningDeployments)
                {
                    Console.WriteLine("{0} : {1}", dep.CloudProviderID, dep.InstanceCount);
                }
            };

            var deployments = scalingClient.ListDeployments();
            printDeps(deployments);

            Console.WriteLine("---------------------------------------------------");
            Console.WriteLine("Updating..............");
            scalingClient.UpdateDeployment(new DeploymentSize() { CloudProviderID = "Azure", InstanceCount = 5 }); //This should have no effect if the InstanceCount was already set to 5
            scalingClient.UpdateDeployment(new DeploymentSize() { CloudProviderID = "TestCloudProvider", InstanceCount = 4 });

            Console.Write("Press enter to see the result of the update");
            Console.ReadLine();
            deployments = scalingClient.ListDeployments();
            printDeps(deployments);

            Console.WriteLine("Updating..............");
            scalingClient.UpdateDeployment(new DeploymentSize() { CloudProviderID = "Azure", InstanceCount = 3 }); //This should now initiate an update request00000000
            scalingClient.UpdateDeployment(new DeploymentSize() { CloudProviderID = "TestCloudProvider", InstanceCount = 8 });
            Console.Write("Press enter to see the result of the update");
            Console.ReadLine();
            deployments = scalingClient.ListDeployments();
            printDeps(deployments);

            Console.Write("Press enter to quit the program");
            Console.ReadLine();
            scalingClient.Close();
        }
    }
}
