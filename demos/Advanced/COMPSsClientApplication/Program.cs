//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using OGF.BES;
using OGF.JDSL;
using OGF.JSDL_HPCPA;
using Microsoft.EMIC.Cloud.COMPSsClient;
using Microsoft.EMIC.Cloud.Security;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;

namespace COMPSsClientApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            var unsecureEndpoint = ConfigurationManager.AppSettings["UnsecureEndpoint"];
            var securedEndpoint = ConfigurationManager.AppSettings["SecuredEndpoint"];
            var userName = ConfigurationManager.AppSettings["UserName"];
            var passwd = ConfigurationManager.AppSettings["Passwd"];
            var thumbprint = ConfigurationManager.AppSettings["Thumbprint"];

            #region createJSDL
            var createActivtiyRequest = new CreateActivityRequest()
            {
                CreateActivity = new CreateActivityType()
                {
                    ActivityDocument = new ActivityDocumentType()
                    {
                        JobDefinition = new JobDefinition_Type()
                        {
                            JobDescription = new JobDescription_Type()
                            {
                                JobIdentification = new JobIdentification_Type()
                                {
                                    JobName = "UPVBLAST",
                                    Description = "VENUS-C BLAST"
                                },
                                Application = new Application_Type()
                                {
                                    ApplicationName = "Blast",
                                    Any = new List<System.Xml.XmlElement>()
                                    {
                                        XMLSerializerHelper.Serialize(
                                        new HPCProfileApplication_Type()
                                        {
                                            Executable = "blast.Blast",
                                            Argument = new List<string>()
                                            {
                                                "true",
                                                "/home/user/binary/blastall",
                                                "/sharedDisk/swissprot/swissprot",
                                                "/sharedDisk/sargasso_1MB.fasta",
                                                "20",
                                                "/home/user/",
                                                "/home/user/IT/blast.Blast/out.txt",
                                                "-v 10 -b 10 -e 1e-10"
                                            }
                                        })
                                    }
                                },
                                Resources = new Resources_Type()
                                {
                                    OperatingSystem = new OperatingSystem_Type()
                                    {
                                        Description = "venuscdebianbase"
                                    },
                                    IndividualDiskSpace = new RangeValue_Type()
                                    {
                                        Exact = new List<Exact_Type>()
                                        {
                                            new Exact_Type()
                                            {
                                                Value = 1.0
                                            }
                                        }
                                    },
                                    TotalCPUCount = new RangeValue_Type()
                                    {
                                        Range = new List<Range_Type>()
                                        {
                                            new Range_Type()
                                            {
                                                LowerBound = new Boundary_Type()
                                                {
                                                    Value = 0.0
                                                },
                                                UpperBound = new Boundary_Type()
                                                {
                                                    Value = 4.0
                                                } 
                                            }
                                        }
                                    }
                                },
                                DataStaging = new List<DataStaging_Type>()
                                {
                                    new DataStaging_Type()
                                    {
                                        FileName = "Blast",
                                        Source = new SourceTarget_Type()
                                        {
                                            URI = "<CDMIReference Credential=\"Username=ogf30;Password=ogf30\" Address=\"http://bscgrid20.bsc.es:2365/ogf30/Blast/Blast.tar.gz\" />"
                                        }
                                    },
                                    new DataStaging_Type()
                                    {
                                        FileName = "swissprot/",
                                        Source = new SourceTarget_Type()
                                        {
                                            URI = "<CDMIReference Credential=\"Username=ogf30;Password=ogf30\" Address=\"http://bscgrid20.bsc.es:2365/ogf30/Blast/databases/swissprot/\" />"
                                        }
                                    },
                                    new DataStaging_Type()
                                    {
                                        FileName = "sargasso_1MB.fasta",
                                        Source = new SourceTarget_Type()
                                        {
                                            URI = "<CDMIReference Credential=\"Username=ogf30;Password=ogf30\" Address=\"http://bscgrid20.bsc.es:2365/ogf30/Blast/sequences/sargasso_1MB.fasta\" />"
                                        }
                                    },
                                    new DataStaging_Type()
                                    {
                                        FileName = "out.txt",
                                        Target = new SourceTarget_Type()
                                        {
                                            URI = "<CDMIReference Credential=\"Username=ogf30;Password=ogf30\" Address=\"http://bscgrid20.bsc.es:2365/ogf30/Blast/results/out.txt\" />"
                                        }
                                    },
                                    new DataStaging_Type()
                                    {
                                        FileName = "it.log",
                                        Target = new SourceTarget_Type()
                                        {
                                            URI = "<CDMIReference Credential=\"Username=ogf30;Password=ogf30\" Address=\"http://bscgrid20.bsc.es:2365/ogf30/Blast/results/COMPSs.log\" />"
                                        }
                                    },
                                    new DataStaging_Type()
                                    {
                                        FileName = "resources.log",
                                        Target = new SourceTarget_Type()
                                        {
                                            URI = "<CDMIReference Credential=\"Username=ogf30;Password=ogf30\" Address=\"http://bscgrid20.bsc.es:2365/ogf30/Blast/results/Resources.log\" />"
                                        }
                                    },
                                    new DataStaging_Type()
                                    {
                                        FileName = "UPVBLAST.out",
                                        Target = new SourceTarget_Type()
                                        {
                                            URI = "<CDMIReference Credential=\"Username=ogf30;Password=ogf30\" Address=\"http://bscgrid20.bsc.es:2365/ogf30/Blast/results/UPVBLAST.out\" />"
                                        }
                                    },
                                    new DataStaging_Type()
                                    {
                                        FileName = "UPVBLAST.err",
                                        Target = new SourceTarget_Type()
                                        {
                                            URI = "<CDMIReference Credential=\"Username=ogf30;Password=ogf30\" Address=\"http://bscgrid20.bsc.es:2365/ogf30/Blast/results/UPVBLAST.err\" />"
                                        }
                                    },
                                    new DataStaging_Type()
                                    {
                                        FileName = "jobs.tar.gz",
                                        Target = new SourceTarget_Type()
                                        {
                                            URI = "<CDMIReference Credential=\"Username=ogf30;Password=ogf30\" Address=\"http://bscgrid20.bsc.es:2365/ogf30/Blast/results/jobs.tar.gz\" />"
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
            #endregion

            var unsecureResponse = SubmitViaUnprotectedClient(unsecureEndpoint, createActivtiyRequest);

            var serviceCert = X509Helper.GetX509Certificate2(
                       StoreLocation.LocalMachine, StoreName.My,
                       thumbprint,
                       X509FindType.FindByThumbprint);

            var response = SubmitViaSecureClient(new EndpointAddress(new Uri(securedEndpoint), new DnsEndpointIdentity(securedEndpoint)), userName, passwd, serviceCert, createActivtiyRequest);
            int statusQueryCount = 0;
            bool isCanceled = false;
            while (true)
            {
                var status =  GetTheStatusViaSecureClient(response, new EndpointAddress(new Uri(securedEndpoint), new DnsEndpointIdentity(securedEndpoint)), userName, passwd, serviceCert);
                if (status.GetActivityStatusesResponse1.Response[0].ActivityStatus.state == ActivityStateEnumeration.Finished ||
                    status.GetActivityStatusesResponse1.Response[0].ActivityStatus.state == ActivityStateEnumeration.Failed ||
                    status.GetActivityStatusesResponse1.Response[0].ActivityStatus.state == ActivityStateEnumeration.Cancelled)
                {
                    break;
                }

                if (statusQueryCount++ >= 10 && !isCanceled)
                {
                    TerminateActivitesViaSecureClient(response, new EndpointAddress(new Uri(securedEndpoint), new DnsEndpointIdentity(securedEndpoint)), userName, passwd, serviceCert);
                    isCanceled = true;
                }
            }

        }

        static EndpointReferenceType SubmitViaUnprotectedClient(string endpointAddress, CreateActivityRequest request)
        {
            COMPSsClient client = COMPSsClient.CreateUnprotectedClient(endpointAddress);

            var response = client.CreateActivity(request);

            if (response == null || String.IsNullOrEmpty(response.CreateActivityResponse1.ActivityIdentifier.Address.Value))
            {
                throw new Exception("Create Activity Response is not valid!");
            }

            return response.CreateActivityResponse1.ActivityIdentifier;
        }

        static EndpointReferenceType SubmitViaSecureClient(EndpointAddress endpointAddress, string userName, string passwd, X509Certificate2 serviceCert, CreateActivityRequest request)
        {
            COMPSsClient client = COMPSsClient.CreateSecureClient(endpointAddress, userName, passwd, serviceCert);

            var response = client.CreateActivity(request);


            if (response == null || String.IsNullOrEmpty(response.CreateActivityResponse1.ActivityIdentifier.Address.Value))
            {
                throw new Exception("Create Activity Response is not valid!");
            }

            return response.CreateActivityResponse1.ActivityIdentifier;
        }

        static GetActivityStatusesResponse GetTheStatusViaSecureClient(EndpointReferenceType endpointReferenceType, EndpointAddress endpointAddress, string userName, string passwd, X509Certificate2 serviceCert)
        {
            COMPSsClient client = COMPSsClient.CreateSecureClient(endpointAddress, userName, passwd, serviceCert);


            GetActivityStatusesRequest request = new GetActivityStatusesRequest()
            {
                GetActivityStatuses = new GetActivityStatusesType()
                {
                    ActivityIdentifier = new List<EndpointReferenceType>
                    {
                        endpointReferenceType
                    }
                }
            };


            var response = client.GetActivityStatuses(request);

            if (response == null || response.GetActivityStatusesResponse1.Response.Count <= 0 ||
                !(response.GetActivityStatusesResponse1.Response[0].ActivityStatus.state == ActivityStateEnumeration.Finished ||
                response.GetActivityStatusesResponse1.Response[0].ActivityStatus.state == ActivityStateEnumeration.Pending ||
                response.GetActivityStatusesResponse1.Response[0].ActivityStatus.state == ActivityStateEnumeration.Running))
            {
                throw new Exception("GetStatus response is not valid!");
            }

            return response;

        }

        static GetActivityStatusesResponse GetTheStatusViaUnprotectedClient(EndpointReferenceType endpointReferenceType, string endpointAddress)
        {
            COMPSsClient client = COMPSsClient.CreateUnprotectedClient(endpointAddress);


            GetActivityStatusesRequest request = new GetActivityStatusesRequest()
            {
                GetActivityStatuses = new GetActivityStatusesType()
                {
                    ActivityIdentifier = new List<EndpointReferenceType>
                    {
                        endpointReferenceType
                    }
                }
            };


            var response = client.GetActivityStatuses(request);

            if (response == null || response.GetActivityStatusesResponse1.Response.Count <= 0 ||
                !(response.GetActivityStatusesResponse1.Response[0].ActivityStatus.state == ActivityStateEnumeration.Finished ||
                response.GetActivityStatusesResponse1.Response[0].ActivityStatus.state == ActivityStateEnumeration.Pending ||
                response.GetActivityStatusesResponse1.Response[0].ActivityStatus.state == ActivityStateEnumeration.Running))
            {
                throw new Exception("GetStatus response is not valid!");
            }

            return response;

        }


        static void TerminateActivitesViaSecureClient(EndpointReferenceType endpointReferenceType, EndpointAddress endpointAddress, string userName, string passwd, X509Certificate2 serviceCert)
        {
            COMPSsClient client = COMPSsClient.CreateSecureClient(endpointAddress, userName, passwd, serviceCert);

            TerminateActivitiesRequest request = new TerminateActivitiesRequest()
            {
                TerminateActivities = new TerminateActivitiesType()
                {
                    ActivityIdentifier = new List<EndpointReferenceType>
                    {
                        endpointReferenceType
                    }
                }
            };

            var response = client.TerminateActivities(request);

            if (response == null || response.TerminateActivitiesResponse1.Response.Count <= 0 ||
                !(response.TerminateActivitiesResponse1.Response[0].Terminated))
            {
                throw new Exception("GetStatus response is not valid!");
            }
        }
    }
}
