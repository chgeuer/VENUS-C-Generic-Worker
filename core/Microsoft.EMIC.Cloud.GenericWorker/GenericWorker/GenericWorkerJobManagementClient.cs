//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using Microsoft.EMIC.Cloud.Utilities;

namespace Microsoft.EMIC.Cloud.GenericWorker
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using OGF.BES;
    using System.IdentityModel.Claims;
    using Microsoft.EMIC.Cloud.Security.Saml;
    using System.ServiceModel.Description;
    using System.ServiceModel.Security.Tokens;
    using System.Reflection;

    /// <summary>
    /// The <see cref="GenericWorkerJobManagementClient"/> is the client-side proxy to submit <see cref="VENUSJobDescription"/> jobs.
    /// </summary>
    public class GenericWorkerJobManagementClient : BESFactoryPortTypeClient
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericWorkerJobManagementClient"/> class.
        /// </summary>
        public GenericWorkerJobManagementClient() : base() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="GenericWorkerJobManagementClient"/> class.
        /// </summary>
        /// <param name="endpointConfigurationName">Name of the endpoint configuration.</param>
        public GenericWorkerJobManagementClient(string endpointConfigurationName) : base(endpointConfigurationName) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="GenericWorkerJobManagementClient"/> class.
        /// </summary>
        /// <param name="endpointConfigurationName">Name of the endpoint configuration.</param>
        /// <param name="remoteAddress">The remote address.</param>
        public GenericWorkerJobManagementClient(string endpointConfigurationName, string remoteAddress) : base(endpointConfigurationName, remoteAddress) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="GenericWorkerJobManagementClient"/> class.
        /// </summary>
        /// <param name="endpointConfigurationName">Name of the endpoint configuration.</param>
        /// <param name="remoteAddress">The remote address.</param>
        public GenericWorkerJobManagementClient(string endpointConfigurationName, EndpointAddress remoteAddress) : base(endpointConfigurationName, remoteAddress) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="GenericWorkerJobManagementClient"/> class.
        /// </summary>
        /// <param name="binding">The binding.</param>
        /// <param name="remoteAddress">The remote address.</param>
        public GenericWorkerJobManagementClient(Binding binding, EndpointAddress remoteAddress) : base(binding, remoteAddress) { }

        #endregion

        private T DoExponentialBackoffAndRetryIfBusy<T>(Func<T> func) where T:class
        {
            var retries = 5;
            for (int i = 1; i <= retries; i++)
            {
                try
                {
                    var a = func();
                    return a;
                }
                catch (FaultException<OGF.BES.Faults.NotAcceptingNewActivitiesFaultType>)
                {
                    if (i == retries)
                    {
                        throw;
                    }
                    else
                    {
                        Console.WriteLine(String.Format("Retry {0}",i));
                        System.Threading.Thread.Sleep(10 ^ i);
                    }
                }  
            }
            throw new FaultException<OGF.BES.Faults.NotAcceptingNewActivitiesFaultType>(new OGF.BES.Faults.NotAcceptingNewActivitiesFaultType()
            {

            }, new FaultReason(String.Format(ExceptionMessages.ServiceBusy)));
        }

        /// <summary>
        /// Submits the VENUS job.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <returns></returns>
        public CreateActivityResponse SubmitVENUSJob(VENUSJobDescription job) 
        {
            var assem = Assembly.GetExecutingAssembly();
            var version = assem.GetName().Version;
            job.ClientVersion = version.ToString();

            Func<CreateActivityResponse> DoJobSubmission = () =>
            {
                    var activity = job.GetCreateActivity();
                    return ((BESFactoryPortType)this).CreateActivity(activity);
            };

            return DoExponentialBackoffAndRetryIfBusy<CreateActivityResponse>(DoJobSubmission);
        }

        #region BESFactoryPortTypeClient members

        /// <summary>
        /// Gets the activity statuses.
        /// </summary>
        /// <param name="jobs">The jobs.</param>
        /// <returns></returns>
        public GetActivityStatusesResponse GetActivityStatuses(List<EndpointReferenceType> jobs)
        {
            var request = new GetActivityStatusesRequest
            {
                GetActivityStatuses = new GetActivityStatusesType
                {
                    ActivityIdentifier = jobs
                }
            };

            return ((BESFactoryPortType)this).GetActivityStatuses(request);
        }

        /// <summary>
        /// Terminates the activities.
        /// </summary>
        /// <param name="jobs">The jobs.</param>
        /// <returns></returns>
        public TerminateActivitiesResponse TerminateActivities(List<EndpointReferenceType> jobs)
        {
            var request = new TerminateActivitiesRequest
            {
                TerminateActivities = new TerminateActivitiesType
                {
                    ActivityIdentifier = jobs
                }
            };

            return ((BESFactoryPortType)this).TerminateActivities(request);
        }

        /// <summary>
        /// Gets the activity documents.
        /// </summary>
        /// <param name="jobs">The jobs.</param>
        /// <returns></returns>
        public GetActivityDocumentsResponse GetActivityDocuments(List<EndpointReferenceType> jobs)
        {
            var request = new GetActivityDocumentsRequest
            {
                GetActivityDocuments = new GetActivityDocumentsType
                {
                    ActivityIdentifier = jobs
                }
            };

            return ((BESFactoryPortType)this).GetActivityDocuments(request);
        }

        #endregion

        /// <summary>
        /// Remove the terminated jobs from the tables
        /// </summary>
        /// <param name="owner">Owner of the group</param>
        public new void RemoveTerminatedJobs(string owner)
        {
            base.Channel.RemoveTerminatedJobs(owner);
        }

        /// <summary>
        /// Get the number of the jobs of the given owner and statuses
        /// </summary>
        /// <param name="owner">Owner of the jobs</param>
        /// <param name="statuses">Statuses asked for</param>
        /// <returns>Number of the jobs</returns>
        public new int GetNumberOfJobs(string owner,List<JobStatus> statuses)
        {
            return base.Channel.GetNumberOfJobs(owner, statuses);
        }

        /// <summary>
        /// Get all jobs for the given owner
        /// </summary>
        /// <param name="owner">Owner of the of jobs</param>
        /// <param name="page">Paging mechanism is implemented to split the return size. The number of page is needed.</param>
        /// <returns>List of unique identifiers of the jobs</returns>
        public new List<OGF.BES.EndpointReferenceType> GetJobs(string owner, int page = -1)
        {
            return base.Channel.GetJobs(owner, page);
        }

        /// <summary>
        /// Get all the jobs in the system
        /// </summary>
        /// <param name="page">Paging mechanism is implemented to split the return size. The number of page is needed.</param>
        /// <returns>List of unique identifiers of the jobs</returns>
        public new List<OGF.BES.EndpointReferenceType> GetAllJobs(int page = -1)
        {
            return base.Channel.GetAllJobs(page);
        }

        /// <summary>
        /// Get the list of the jobs (hieararchy) under the root
        /// </summary>
        /// <param name="root">Unique identifier of the root job</param>
        /// <param name="page">Paging mechanism is implemented to split the return size. The number of page is needed.</param>
        /// <returns>List of unique identifiers of the jobs in the hieararchy</returns>
        public new List<OGF.BES.EndpointReferenceType> GetHierarchy(EndpointReferenceType root, int page = -1)
        {
            return base.Channel.GetHierarchy(root, page);
        }

        /// <summary>
        /// Get the root job of the given job 
        /// </summary>
        /// <param name="job">Unique identifier of the job in the hieararchy</param>
        /// <returns>Unique identifier of the root job</returns>
        public new OGF.BES.EndpointReferenceType GetRoot(EndpointReferenceType job)
        {
            return base.Channel.GetRoot(job);
        }

        /// <summary>
        /// Gets all the jobs in the given group
        /// </summary>
        /// <param name="owner">Owner of the group</param>
        /// <param name="groupName">Name of the group</param>
        /// <param name="page">Paging mechanism is implemented to split the return size. The number of page is needed.</param>
        /// <returns>List of unique identifiers of the jobs in the group</returns>
        public new List<EndpointReferenceType> GetJobsByGroup(string owner, string groupName, int page = -1)
        {
            return base.Channel.GetJobsByGroup(owner, groupName, page);
        }

        /// <summary>
        /// Cancel all the jobs in the group
        /// </summary>
        /// <param name="owner">Owner of the group</param>
        /// <param name="groupName">Name of the group</param>
        public new void CancelGroup(string owner, string groupName)
        {        
            base.Channel.CancelGroup(owner, groupName);
        }

        /// <summary>
        /// Cancel all the jobs in the hieararchy under the given root
        /// </summary>
        /// <param name="root">Unique identifier of the root job</param>
        public new void CancelHierarchy(EndpointReferenceType root)
        {
            base.Channel.CancelHierarchy(root);
        }


        /// <summary>
        /// Creates the unprotected client.
        /// </summary>
        /// <param name="applicationStoreUrl">The application store URL.</param>
        /// <returns></returns>
        public static GenericWorkerJobManagementClient CreateUnprotectedClient(string applicationStoreUrl)
        {
            return new GenericWorkerJobManagementClient(
                      new WS2007HttpBinding(SecurityMode.None, reliableSessionEnabled: false),
                      new EndpointAddress(new Uri(applicationStoreUrl)));
        }

        /// <summary>
        /// Creates the secure client.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="issuer">The issuer.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="serviceCert">The service certificate</param>
        /// <returns></returns>
        public static GenericWorkerJobManagementClient CreateSecureClient(EndpointAddress address, EndpointAddress issuer, string username, string password, X509Certificate2 serviceCert)
        {
            var secureBinding = WCFUtils.CreateSecureUsernamePasswordClientBinding(issuer);

            var client = new GenericWorkerJobManagementClient(secureBinding, address);
            client.ClientCredentials.UserName.UserName = username;
            client.ClientCredentials.UserName.Password = password;
            client.ClientCredentials.ServiceCertificate.DefaultCertificate = serviceCert;


            return client;
        }

        /// <summary>
        /// Creates the secure client.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="claimset">The claimset.</param>
        /// <param name="clientCert">The client certificate.</param>
        /// <param name="serviceCert">The service certificate.</param>
        /// <returns></returns>
        public static GenericWorkerJobManagementClient CreateSecureClient(EndpointAddress address, ClaimSet claimset, X509Certificate2 clientCert, X509Certificate2 serviceCert)
        {
            var secureBinding = WCFUtils.CreateSecureSamlBinding();
            var client = new GenericWorkerJobManagementClient(secureBinding, address);

            #region Create new credential class
            
            var samlCC = new SamlClientCredentials();

            // Set the client certificate. This is the cert that will be used to sign the SAML token in the symmetric proof key case
            samlCC.ClientCertificate.Certificate = clientCert;

            // Set the service certificate. This is the cert that will be used to encrypt the proof key in the symmetric proof key case
            samlCC.ServiceCertificate.DefaultCertificate = serviceCert;

            samlCC.Claims = claimset;

            samlCC.Audience = address.Uri;

            #endregion

            // set new credentials
            client.ChannelFactory.Endpoint.Behaviors.Remove(typeof(ClientCredentials));
            client.ChannelFactory.Endpoint.Behaviors.Add(samlCC);
            
            return client;
        }

        /// <summary>
        /// Creates a job submission client for use within a cloud machine.
        /// </summary>
        /// <returns></returns>
        public static GenericWorkerJobManagementClient CreateLocalJobSubmissionClient() 
        {
            // retrieve the local job submission endpoint from the environment variable
            var localJobSubmissionUri = Environment.GetEnvironmentVariable(GenericWorkerDriver.ENVIRONMENT_VARIABLE_JOBSUBMISSIONENDPOINT);

            return CreateUnprotectedClient(localJobSubmissionUri);
        }
    }
}