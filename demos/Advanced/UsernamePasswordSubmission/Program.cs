//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿
namespace UsernamePasswordSubmission
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.EMIC.Cloud.GenericWorker;
    using System.ServiceModel;
    using Microsoft.EMIC.Cloud.Utilities;
    using Microsoft.EMIC.Cloud.DataManagement;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.EMIC.Cloud.Security;
    
    /// <summary>
    /// This is a simple application that runs the SimpleMathConsole App Job by authenticating against the supplied service URL
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        { 
            // Simple Math Console App job description

            VENUSJobDescription aSimpleJobDescription = new VENUSJobDescription
            {
                ApplicationIdentificationURI = "http://somestuff",
                CustomerJobID = "SimpleMathConsoleApp Job " + DateTime.Now.ToLocalTime().ToString(),
                JobName = "some job name",
                JobArgs = new ArgumentCollection()
                {
                    new LiteralArgument
                    {
                        Name = "Multiply",
                        LiteralValue = "3"
                    }   
                }
            };

            // configure username/password service url here
            var secureGenericWorkerURL = "http://localhost:81/JobSubmission/UsernamePasswordSample.svc";
            GenericWorkerJobManagementClient jobSubmissionPortal = new GenericWorkerJobManagementClient(
                WCFUtils.CreateUsernamePasswordSecurityTokenServiceBinding(),
               new EndpointAddress(new Uri(secureGenericWorkerURL), new DnsEndpointIdentity("MyAzureSTS")));

            // supply the credentials
            jobSubmissionPortal.ClientCredentials.UserName.UserName = "Administrator";
            jobSubmissionPortal.ClientCredentials.UserName.Password = "secret2";

            // configure the thumbprint of the certificate
            var serviceCert = X509Helper.LookupCertificate(
                StoreLocation.LocalMachine, StoreName.My.ToString(),
                "57F404B1B48120BE275C79DB02D6617B2367058D", X509FindType.FindByThumbprint);

            jobSubmissionPortal.ClientCredentials.ServiceCertificate.DefaultCertificate = serviceCert;

            Console.Write("Press <return> to submit job... ");
            Console.ReadLine();

            // submit the jobs
            jobSubmissionPortal.SubmitVENUSJob(aSimpleJobDescription);
            jobSubmissionPortal.SubmitVENUSJob(aSimpleJobDescription);
            jobSubmissionPortal.SubmitVENUSJob(aSimpleJobDescription);

            Console.WriteLine("Submitted... ");
            Console.Write("Press <return> to close... ");
            Console.ReadLine();
        }
    }
}
