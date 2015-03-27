//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EMIC.Cloud.GenericWorker;
using System.Configuration;
using System.ServiceModel;
using Microsoft.EMIC.Cloud.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.EMIC.Cloud.Utilities;

namespace JobListingSampleApplication
{
    class Program
    {
        /// <summary>
        /// This demo application demonstrates how all jobs or jobs of a specific user are listed. 
        /// According to the security rules, only Administrator users are allowed to get all jobs and normal users can only get their jobs. 
        /// This security rules are also demonstrated in this demo application. 
        /// The user table and the corresponding data should be created before executing this demo. 
        /// </summary>
        /// <param name="args">no input parameters</param>
        static void Main(string[] args)
        {
            // configuration parameters are taken from the configuration file
            var issuerAddress = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.STS.URL"];
            var authThumprint = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.AuthCert.Thumbprint"];
            var dnsEnpointId = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.AuthCert.DnsEndpointId"];
            var jobListingServiceUrl=ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.JobListingService.URL"];

            // job listing client for "Alice" user is created
            var aliceJobListingClient = GenericWorkerJobManagementClient.CreateSecureClient(
                    address: new EndpointAddress(new Uri(jobListingServiceUrl), new DnsEndpointIdentity(dnsEnpointId)),
                    issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                    username: "Alice",
                    password: "secret",
                    serviceCert: X509Helper.GetX509Certificate2(
                        StoreLocation.LocalMachine, StoreName.My,
                        authThumprint,
                        X509FindType.FindByThumbprint));
            
            // job listing client for "Bob" user is created
            var bobJobListingClient = GenericWorkerJobManagementClient.CreateSecureClient(
                    address: new EndpointAddress(new Uri(jobListingServiceUrl), new DnsEndpointIdentity(dnsEnpointId)),
                    issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                    username: "Bob",
                    password: "secret",
                    serviceCert: X509Helper.GetX509Certificate2(
                        StoreLocation.LocalMachine, StoreName.My,
                        authThumprint,
                        X509FindType.FindByThumbprint));

            // job listing client for "Administrator" user is created
            var adminJobListingClient = GenericWorkerJobManagementClient.CreateSecureClient(
                    address: new EndpointAddress(new Uri(jobListingServiceUrl), new DnsEndpointIdentity(dnsEnpointId)),
                    issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                    username: "Administrator",
                    password: "secret",
                    serviceCert: X509Helper.GetX509Certificate2(
                        StoreLocation.LocalMachine, StoreName.My,
                        authThumprint,
                        X509FindType.FindByThumbprint));

            try
            {
                // get all jobs with "Alice" user, it results an exception because "Alice" is not admin user
                aliceJobListingClient.GetAllJobs();
                Console.WriteLine("!!!!! researcher should not be able to access GetAllJobs");
            }
            catch (Exception )
            {
                Console.WriteLine("Once I am really happy to get a MessageSecurityException");
            }
            try
            {
                // get jobs of "Bob" with "Alice" user, it results an exception because "Alice" is not admin user
                aliceJobListingClient.GetJobs("Bob");
                Console.WriteLine("!!!!! researcher should not be able to access GetAllJobs");
            }
            catch (Exception )
            {
                Console.WriteLine("Once I am really happy to get a MessageSecurityException");
            }
            try
            {
                // get all jobs with admin
                adminJobListingClient.GetAllJobs();
                // get own jobs with "Alice"
                aliceJobListingClient.GetJobs("Alice");
                // get own jobs with "Bob"
                bobJobListingClient.GetJobs("Bob");
                // get jobs of "Alice" with admin
                adminJobListingClient.GetJobs("Alice");

                Console.WriteLine("Security is working for joblisting service!!! Press enter to quit the program!");
            }
            catch (Exception)
            {
                Console.WriteLine("Here we should not get any exceptions!! Your security settings are incorrect. Press enter to quit the program!");
            }
            Console.ReadLine();
        }
    }
}
