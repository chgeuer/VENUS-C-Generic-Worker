//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EMIC.Cloud.GenericWorker;
using Microsoft.EMIC.Cloud.DataManagement;
using System.ServiceModel;
using System.IdentityModel.Claims;
using Microsoft.EMIC.Cloud.Security;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;


namespace SelfIssuedTokenSubmission
{
    /// <summary>
    /// This application demonstrates how the Generic Worker can be used with self-issued authentication tokens. 
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // create job description
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

            // configure Generic Worker Secure Service url here

            var secureGenericWorkerURL = "http://ameick-test.cloudapp.net/JobSubmission/SecureService.svc";

            // configure endpoint here
            var submissionEndpoint = new EndpointAddress(new Uri(secureGenericWorkerURL), new DnsEndpointIdentity("Windows Azure Tools")); //This has to be the identity of the STS certificate
            
            // Create some claims to put in the SAML assertion
            IList<Claim> claims = new List<Claim>();

            claims.Add(new Claim(ClaimTypes.Name, "Name Joe", Rights.PossessProperty));
            claims.Add(new Claim(ClaimTypes.GivenName, "Joe", Rights.PossessProperty));
            claims.Add(new Claim(ClaimTypes.Surname, "Bloggs", Rights.PossessProperty));
            claims.Add(new Claim(ClaimTypes.Email, "joe@university.edu", Rights.PossessProperty));
            claims.Add(new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "VENUS-C Researcher", Rights.PossessProperty));
            
            ClaimSet claimSet = new DefaultClaimSet(claims);

            // configure certificate thumbprint here
            var serviceCert = LookupCertificate2(
                StoreLocation.LocalMachine, StoreName.My.ToString(),
                "‎<YOUR CERTIFICATE THUMBPRINT UPPERCASE>", X509FindType.FindByThumbprint);

            // For the time being, clientCert and serviceCert have to be the same.
            var clientCert = serviceCert;
            
            // create secure client
            var jobSubmissionPortal = GenericWorkerJobManagementClient.CreateSecureClient(submissionEndpoint, claimSet, clientCert, serviceCert);

            Console.Write("Press <return> to submit job... ");
            Console.ReadLine();

            // submit the job
            jobSubmissionPortal.SubmitVENUSJob(aSimpleJobDescription);

            Console.WriteLine("Submitted... ");
            Console.Write("Press <return> to close... ");
            Console.ReadLine();

        }

        private static X509Certificate2 LookupCertificate2(StoreLocation storeLocation, string storeName, string findValue, X509FindType x509FindType)
        {
            X509Store store = null;
            try
            {
                store = new X509Store(storeName, storeLocation);
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certs = store.Certificates;
                for (int i = 0; i < certs.Count; i++)
                {
                    if (certs[i].Thumbprint.Equals(findValue, StringComparison.CurrentCulture))
                    {
                        return certs[i];
                    }
                }
                if (certs.Count != 1)
                {
                    throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture,
                        "FedUtil: Certificate {0} not found in store {2}/{3} (there were {1} certs matching the query)",
                        findValue, certs.Count, Enum.GetName(typeof(StoreLocation), storeLocation), storeName));
                }

                return (X509Certificate2)certs[0];
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                if (store != null)
                {
                    store.Close();
                }
            }
        }
    }
}
