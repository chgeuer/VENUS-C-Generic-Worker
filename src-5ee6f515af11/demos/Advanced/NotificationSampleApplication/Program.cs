//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.ServiceModel;
using Microsoft.EMIC.Cloud.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.EMIC.Cloud.GenericWorker;
using Microsoft.EMIC.Cloud.Utilities;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using PluginAttr = Microsoft.EMIC.Cloud.Notification.SerializableKeyValuePair<string, string>;
using OGF.BES;
using Microsoft.EMIC.Cloud.GenericWorker.AzureProvider;
using System.Security;
using System.Diagnostics;
using System.ServiceModel.Security;
using System.Xml;
using Microsoft.EMIC.Cloud.Notification;
using Microsoft.EMIC.Cloud.GenericWorker.SampleNotifications;

namespace NotificationSampleApplication
{
    class Program
    {
        /// <summary>
        /// The NotificationSampleApplication demo shows how a notification subscription can be made to a submitted job. 
        /// This is useful when the job takes a long time and the user wants to get notified about the outcome of the executed job. 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Console.Title = "Notification Test Client";
            
            // configuration parameters are taken from the configuration file
            var issuerAddress = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.STS.URL"];
            var authThumprint = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.AuthCert.Thumbprint"];
            var dnsEnpointId = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.AuthCert.DnsEndpointId"];
            var secNotificationServiceUrl = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.SecureNotificationService.URL"];
            var unsecNotificationSvcUrl = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.NotificationService.URL"];
            var secureGenericWorkerURL = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.UsernamePasswordSecuredNotificationService.URL"];
            var jobJobManagementServiceServiceUrl = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.JobManagementService.URL"];
            var storageConnectionString = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.Demo.User.ConnectionString"];

            // job management client for Researcher is created
            var researcherJobManagementClient = GenericWorkerJobManagementClient.CreateSecureClient(
                    address: new EndpointAddress(new Uri(jobJobManagementServiceServiceUrl), new DnsEndpointIdentity(dnsEnpointId)),
                    issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                    username: "Researcher",
                    password: "secret",
                    serviceCert: X509Helper.GetX509Certificate2(
                        StoreLocation.LocalMachine, StoreName.My,
                        authThumprint,
                        X509FindType.FindByThumbprint));

            Action<ConsoleColor, string> print = (color, str) =>
            {
                Console.ForegroundColor = color;
                Console.Out.WriteLine(str);
                Console.ResetColor();
            };

            // method for creating secure notification service client
            Func<string, NotificationServiceClient> CreateSecureClient = (username) =>
            {
                print(ConsoleColor.Red, string.Format("Using NotificationService at: {0}", secNotificationServiceUrl));
                print(ConsoleColor.Red, string.Format("fetching tokens from {0}", issuerAddress));

                // In the development setting (when talking to localhost), we need to override WCF security checks
                // with the DnsEndpointIdentity.
                //

                return NotificationServiceClient.CreateSecureClient(
                    address: new EndpointAddress(new Uri(secNotificationServiceUrl), new DnsEndpointIdentity(dnsEnpointId)),
                    issuer: new EndpointAddress(new Uri(issuerAddress), new DnsEndpointIdentity(dnsEnpointId)),
                    username: username,
                    password: "secret",
                    serviceCert: X509Helper.GetX509Certificate2(
                        StoreLocation.LocalMachine, StoreName.My,
                        authThumprint,
                        X509FindType.FindByThumbprint));
            };

            // method for creating unprotected notification service client
            Func<NotificationServiceClient> CreateUnprotectedClient = () =>
            {                
                return NotificationServiceClient.CreateUnprotectedClient(unsecNotificationSvcUrl);
            };

            // method for creating username password secured notification service client
            Func<NotificationServiceClient> CreateUsernamePasswordSecuredNotificationClient = () =>
            {
                NotificationServiceClient notificationClient = new NotificationServiceClient(
                    WCFUtils.CreateUsernamePasswordSecurityTokenServiceBinding(),
                    new EndpointAddress(new Uri(secureGenericWorkerURL), new DnsEndpointIdentity(dnsEnpointId)));

                notificationClient.ClientCredentials.UserName.UserName = "Hakan";
                notificationClient.ClientCredentials.UserName.Password = "test123!";

                var serviceCert = X509Helper.LookupCertificate(
                    StoreLocation.LocalMachine, StoreName.My.ToString(),
                    authThumprint, X509FindType.FindByThumbprint);

                notificationClient.ClientCredentials.ServiceCertificate.DefaultCertificate = serviceCert;
                return notificationClient;
            };

            // creating notification service client for Researcher
            var researcherNotificationClient = CreateSecureClient("Researcher");
            
            // creating notification service client for Alice
            var aliceNotificationClient = CreateSecureClient("Alice");

            // creating notification service client for Administrator
            var administratorNotificationClient = CreateSecureClient("Administrator");

            // creating Cloud Storage Account
            var account = CloudStorageAccount.Parse(storageConnectionString);

            // creating queue client
            var queueClient = account.CreateCloudQueueClient();

            // add date or timestamp ticks to the queuenames below

            // names of the queues that will be created
            var allstatuses = "allstatuses";
            var runningstatus = "runningstatus";
            var finishedstatus = "finishedstatus";

            // creating queue references
            var allqueue = queueClient.GetQueueReference(allstatuses);
            var runningqueue = queueClient.GetQueueReference(runningstatus);
            var finishedqueue = queueClient.GetQueueReference(finishedstatus);

            // delete if previous data in queue exists
            if (allqueue.Exists()) allqueue.Delete();
            if (runningqueue.Exists()) runningqueue.Delete();
            if (finishedqueue.Exists()) finishedqueue.Delete();

            //Todo submit x aligner jobs depending on one splitter //I ll use the UPVBioClient and add a Readline before submitting the splitter job
            //Register notifications for the aligner jobs:
                //all statuses: going to allstatusesqueue
                //running: going to runningqueue
                //finished: going to finishedqueue
            //try to subscribe to researchers jobs with user alice -> security exception
            
            //try to subscribe as admin -> security exception
            //try to subscribe notification on non existing job -> security exception

            //submit splitter job
            Console.WriteLine("If all blast jobs are submitted (using the upvbioclient), press Enter to subscribe for notifications");
            Console.ReadLine();

            // get jobs for notification subscription
            var researcherJobs = researcherJobManagementClient.GetJobs("Researcher");
            if (researcherJobs.Count == 0)
            {
                Console.WriteLine("there were no jobs to retrieve. please use upv.bio client to submit some jobs");
                Console.ReadLine();
                Environment.Exit(-1);
            }

            // define the properties for the queues

            var pluginConfigAllQueue = new List<PluginAttr>();// 
            pluginConfigAllQueue.Add(new PluginAttr(PluginConfigMandatoryKeys.NAME, JobStatusNotificationPluginNames.AZURE_QUEUE));
            pluginConfigAllQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.QUEUE_NAME, allstatuses));
            pluginConfigAllQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.MESSAGE, "msg1: I like all statuses"));
            pluginConfigAllQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.CONNECTION_STRING, storageConnectionString));

            var pluginConfigRunningQueue = new List<PluginAttr>();// 
            pluginConfigRunningQueue.Add(new PluginAttr(PluginConfigMandatoryKeys.NAME, JobStatusNotificationPluginNames.AZURE_QUEUE));
            pluginConfigRunningQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.QUEUE_NAME, runningstatus));
            pluginConfigRunningQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.MESSAGE, "msg1: I like running statuses"));
            pluginConfigRunningQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.CONNECTION_STRING, storageConnectionString));

            var pluginConfigFinishedQueue = new List<PluginAttr>();// 
            pluginConfigFinishedQueue.Add(new PluginAttr(PluginConfigMandatoryKeys.NAME, JobStatusNotificationPluginNames.AZURE_QUEUE));
            pluginConfigFinishedQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.QUEUE_NAME, finishedstatus));
            pluginConfigFinishedQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.MESSAGE, "msg1: I like finished statuses"));
            pluginConfigFinishedQueue.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.CONNECTION_STRING, storageConnectionString));

            // method to check if an action will cause security exceptions
            Action<Action, string, string> CheckSecurity = (action, succ, failure) =>
            {
                try
                {
                    action();
                    print(ConsoleColor.Red, failure);
                }
                catch (SecurityException)
                {
                    print(ConsoleColor.Green, succ);
                }
                catch (MessageSecurityException)
                {
                    print(ConsoleColor.Green, succ);
                }
                catch (SecurityAccessDeniedException) //WCF converts sec exception to sec acc denied exception
                {
                    print(ConsoleColor.Green, succ);
                }
                catch (FaultException f)
                {
                    var msg = string.Format("endpoint was not found -- {0} \nerror message of the exception:{1} \nreason: {2}", succ, f.Message, f.Reason);
                    print(ConsoleColor.Green, msg); 
                }
            };

            // check if subscription to another users job can be created
            Action AccessForeignJob = () =>
            {
                var job = researcherJobs[0];
                aliceNotificationClient.CreateSubscription(job, pluginConfigAllQueue);
            };

            CheckSecurity(AccessForeignJob, "alice was prevented subscribing on another users job", "access to foreign job was permitted");

            // check if admin can subscribe to job notifications
            Action AdminSubscribe = () =>
            {
                var job = researcherJobs[0];
                administratorNotificationClient.CreateSubscription(job, pluginConfigAllQueue);
            };

            CheckSecurity(AdminSubscribe, "admin is not allowed to subscribe on jobs", "admin was permitted to subscribe on a job");

            // check if a user can subscribe to a dummy, non-existing job
            Action SubscribeOnNonExistingJob = () =>
            {
                var jobIdAsEpr = new JobIdAsEPR("nonexistingjob");
                var job = new EndpointReferenceType();
                job.Address = new AttributedURIType()
                {
                    Value = secureGenericWorkerURL
                };
                job.ReferenceParameters = new ReferenceParametersType
                {
                    Any = new List<XmlElement> { jobIdAsEpr.AsXmlElement() }
                };                
                aliceNotificationClient.CreateSubscription(job, pluginConfigAllQueue);
            };

            CheckSecurity(SubscribeOnNonExistingJob, "users can only subscribe on existing jobs", "user was permitted to subscribe on a non existing job");

            // create subscription for the first job in all statuses
            researcherNotificationClient.CreateSubscription(researcherJobs[0],pluginConfigAllQueue);
            // create subscription for the first job in Running status
            researcherNotificationClient.CreateSubscriptionForStatuses(researcherJobs[0], new List<JobStatus>() { JobStatus.Running }, pluginConfigRunningQueue);
            // create subscription for the first job in Finished status
            researcherNotificationClient.CreateSubscriptionForStatuses(researcherJobs[0], new List<JobStatus>() { JobStatus.Finished }, pluginConfigFinishedQueue);
            // create subscription for the second job in Finished status
            researcherNotificationClient.CreateSubscriptionForStatuses(researcherJobs[1], new List<JobStatus>() { JobStatus.Finished }, pluginConfigFinishedQueue);
            // unsubscribe from the second job
            researcherNotificationClient.Unsubscribe(researcherJobs[1]);
            Console.WriteLine("ok now we have subscribed to the notifications. Use the UPV.Bio client to submit the splitter job. Press enter when all jobs have terminated");
            Console.ReadLine();

            allqueue = queueClient.GetQueueReference(allstatuses);
            runningqueue = queueClient.GetQueueReference(runningstatus);
            finishedqueue = queueClient.GetQueueReference(finishedstatus);

            // check the queues
            var numMsgsAllQueue = allqueue.PeekMessages(32).ToList().Count();
            Debug.Assert(numMsgsAllQueue > 0, "allqueue is empty!");

            var numMsgsRunQueue = runningqueue.PeekMessages(32).ToList().Count();
            Debug.Assert(numMsgsRunQueue == 1, "runningqueue should contain one message (for researcherjob 0)!");

            var numMsgsFinQueue = finishedqueue.PeekMessages(32).ToList().Count();
            Debug.Assert(numMsgsFinQueue==1, "finishedqueue should contain two message (for researcherjob 0)!");

            Console.WriteLine("Press Enter to quit the program");
            Console.ReadLine();
        }
    }
}
