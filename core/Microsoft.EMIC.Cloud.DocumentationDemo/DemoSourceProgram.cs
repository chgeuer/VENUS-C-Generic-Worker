//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿#region UsingStatementsForRegularSystemNamespaces
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
#endregion

#region UsingStatementsForTheGenericWorker
using Ionic.Zip;
using Microsoft.EMIC.Cloud;
using Microsoft.EMIC.Cloud.ApplicationRepository;
using Microsoft.EMIC.Cloud.DataManagement;
using Microsoft.EMIC.Cloud.GenericWorker;
using Microsoft.EMIC.Cloud.Security;
using System.Security.Cryptography.X509Certificates;
using System.Configuration;
#endregion

#region UsingStatementsForWindowsAzureStorage
using Microsoft.EMIC.Cloud.Storage.Azure;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.EMIC.Cloud.Utilities;
using System.Runtime.Serialization;
using System.Web;
using OGF.BES;
#endregion

#region UsingStatementForNotificationPluginConfiguration
using PluginAttr = Microsoft.EMIC.Cloud.Notification.SerializableKeyValuePair<string, string>;
using Microsoft.EMIC.Cloud.GenericWorker.AzureProvider;
using Microsoft.EMIC.Cloud.Notification;
using Microsoft.EMIC.Cloud.GenericWorker.SampleNotifications;
using Microsoft.EMIC.Cloud.GenericWorker.AzureAccounting;
#endregion

namespace Microsoft.EMIC.Cloud.DocumentationDemo
{
	/// <summary>
	/// Documentation sample for Sandcastle. 
	/// 
	/// This application is not intended to be executed, but serves as 
	/// live code to be compiles against the Generic Worker API. 
	/// </summary>
	class DemoSourceProgram
	{
		const string azureStorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=venuscdemouserstorage;AccountKey=utTKN4O1uWmbj09u8JdT+mS6iRXhnjIcACFCpDmWZdYFqZXPOarwKl2xH9/EaH8rTRFN6GAi37//J3ZwzNIaIA==";
		const string azureStorageConnectionStringReplacement = "DefaultEndpointsProtocol=https;AccountName=adminstorage;AccountKey=EK ... jk==";
		const string secureApplicationRepositoryURL = "http://myexperiment.cloudapp.net/AppStore/SecureService.svc";
		const string secureGenericWorkerURL = "http://myexperiment.cloudapp.net/JobSubmission/SecureService.svc";
		const string secureScalingURL = "http://myexperiment.cloudapp.net/JobSubmission/SecureService.svc";
		const string JobManagementURL = "http://myexperiment.cloudapp.net/JobManagement/";
		const string securityTokenServiceURL = "http://myexperiment.cloudapp.net/STS/UsernamePassword.svc";
		const string applicationIdentificationURI = "http://www.microsoft.com/emic/cloud/demo/SimpleMathApp";
		const string secureNotificationServiceUrl = "http://myexperiment.cloudapp.net/NotificationService/SecureService.svc";

		static void Main(string[] args)
		{

			//This is just for displaying the content of an application/job description in the documentation
			string addressInputFileDatastore = "sampleInputFileAdressURI";
			string addressOutputFileDatastore = "sampleOutputFileAdressURI";

			VENUSApplicationDescription applicationDescription = CreateApplicationDescription();
			VENUSJobDescription aSimpleJobDescription = createJobDescription(addressInputFileDatastore, addressOutputFileDatastore);

			//open file for write which gets included later in the documentation
			StreamWriter xmlFile = new StreamWriter(@"../../docuDemoJobAppDescription.xml");
			xmlFile.WriteLine("<!-- #region appdescription -->");
			xmlFile.WriteLine(System.Text.Encoding.UTF8.GetString(createXMLMemoryStreamOfAppDescription(applicationDescription).ToArray()).Replace(azureStorageConnectionString, azureStorageConnectionStringReplacement).Replace("venuscdemouserstorage", "adminstorage"));
			xmlFile.WriteLine("<!-- #endregion -->");
			xmlFile.WriteLine("<!-- #region jobdescription -->");
			xmlFile.WriteLine(System.Text.Encoding.UTF8.GetString(aSimpleJobDescription.AsMemoryStream().ToArray()).Replace(azureStorageConnectionString, azureStorageConnectionStringReplacement).Replace("venuscdemouserstorage", "adminstorage"));
			xmlFile.WriteLine("<!-- #endregion -->");
			xmlFile.Close();
		}

		static void Run(string[] args)
		{
			#region UploadApplication
			// The application calling semantics needs to be described 
			VENUSApplicationDescription applicationDescription = CreateApplicationDescription();
			// The zipped application is available as byte[] array
			string[] appFiles = { @".\SimpleMathConsoleApp.exe" };
			MemoryStream zippedApplication = PackageApplication(appFiles);

			UploadAndRegisterApplication(zippedApplication, applicationDescription);
			#endregion

			#region UploadUserDataAndJobPreperation
			CloudBlobContainer userDataContainer = connectToCloudStorageContainer("userdatacontainer");

			string inputFileLocal = "../input.csv";
			string inputFileName = "input.csv";
			string inputFileAlreadyInDatastore = "otherinput.csv";
			string resultFileName = "result.txt";
			string addressInputFileDatastore = string.Empty;
			string addressOutputFileDatastore = string.Empty;

			//Upload local file to the datastore
			addressInputFileDatastore = uploadFile(userDataContainer, inputFileName, inputFileLocal);

			//Inputfile is already in datastore so return its adress
			addressInputFileDatastore = getBlobAdress(userDataContainer, inputFileAlreadyInDatastore);

			//Outputfile is not in the datastore so get an adress for it
			addressOutputFileDatastore = getBlobAdress(userDataContainer, resultFileName);
			#endregion

			VENUSJobDescription aSimpleJobDescription = createJobDescription(addressInputFileDatastore, addressOutputFileDatastore);

			#region SubmitJob

			GenericWorkerJobManagementClient jobSubmissionPortal = GenericWorkerJobManagementClient.CreateSecureClient(
				new EndpointAddress(new Uri(secureGenericWorkerURL), new DnsEndpointIdentity("myexperiment.cloudapp.net")),
				new EndpointAddress(new Uri(securityTokenServiceURL), new DnsEndpointIdentity("myexperiment.cloudapp.net")),
				"Researcher", "secret",
				X509Helper.GetX509Certificate2(
					StoreLocation.LocalMachine, StoreName.My,
					"123456789ABCDEF123456789ABCDEF123456789A",
					X509FindType.FindByThumbprint));

			jobSubmissionPortal.SubmitVENUSJob(aSimpleJobDescription);

			#endregion

			#region ListJobs
			var researcherJobs = jobSubmissionPortal.GetJobs("Researcher");
			#endregion

			#region JobNotificationSecurity
			var notificationClient = NotificationServiceClient.CreateSecureClient(
				address: new EndpointAddress(new Uri(secureNotificationServiceUrl), new DnsEndpointIdentity("myexperiment.cloudapp.net")),
				issuer: new EndpointAddress(new Uri(securityTokenServiceURL), new DnsEndpointIdentity("myexperiment.cloudapp.net")),
				username: "Researcher",
				password: "secret",
				serviceCert: X509Helper.GetX509Certificate2(
					StoreLocation.LocalMachine, StoreName.My,
					"123456789ABCDEF123456789ABCDEF123456789A",
					X509FindType.FindByThumbprint));
			#endregion

			#region SetupQueueNotificationPlugin
			var finishedstatus = "finishedstatus";
			var queuePluginConfigFinished = new List<PluginAttr>();
			queuePluginConfigFinished.Add(new PluginAttr(PluginConfigMandatoryKeys.NAME, JobStatusNotificationPluginNames.AZURE_QUEUE));
			queuePluginConfigFinished.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.QUEUE_NAME, finishedstatus));
			queuePluginConfigFinished.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.MESSAGE, "msg: I like finished statuses"));
			queuePluginConfigFinished.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.CONNECTION_STRING, azureStorageConnectionString));
			#endregion

			#region SetupNotificationsForAllFinished
			researcherJobs.ForEach(job => notificationClient.CreateSubscriptionForStatuses(job, new List<JobStatus>() {JobStatus.Finished},queuePluginConfigFinished));
			#endregion

			var job1queue = "job1queue";
			var otherQueuePluginConfig = new List<PluginAttr>();
			otherQueuePluginConfig.Add(new PluginAttr(PluginConfigMandatoryKeys.NAME, JobStatusNotificationPluginNames.AZURE_QUEUE));
			otherQueuePluginConfig.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.QUEUE_NAME, job1queue));
			otherQueuePluginConfig.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.MESSAGE, "msg: I like any progress"));
			otherQueuePluginConfig.Add(new PluginAttr(AzureQueueJobStatusNotificationPlugin.CONNECTION_STRING, azureStorageConnectionString));

			#region SetupNotificationsForMultipleStatuses
			var statuses = new List<JobStatus>(){JobStatus.Running,JobStatus.Finished,JobStatus.Failed};
			notificationClient.CreateSubscriptionForStatuses(researcherJobs[0], statuses, otherQueuePluginConfig);
			#endregion

			#region SetupNotificationsForAllStatuses
			notificationClient.CreateSubscription(researcherJobs[0], otherQueuePluginConfig);
			#endregion

			#region UnsubscribeNotifications
			notificationClient.Unsubscribe(researcherJobs[0]);
			#endregion

			#region SetupNotificationForJobGroup
			var group = "blastJobs";
			notificationClient.CreateSubscriptionForGroupStatuses(group, new List<JobStatus> { JobStatus.Finished }, queuePluginConfigFinished);
			#endregion

			#region ListAllJobs
			var allJobs = jobSubmissionPortal.GetAllJobs();
			#endregion

			#region ListAllJobsPaged
			var jobsPage0 = jobSubmissionPortal.GetAllJobs(0);
			var jobsPage1 = jobSubmissionPortal.GetAllJobs(1);
			#endregion

			#region GetNumberOfJobs
			var numSubmittedJobs = jobSubmissionPortal.GetNumberOfJobs("Researcher", new List<JobStatus>{JobStatus.Submitted});
			#endregion
			#region GetOwnJobStatuses
			var myJobs = jobSubmissionPortal.GetJobs("Researcher");
			var myStats = jobSubmissionPortal.GetActivityStatuses(myJobs);
			#endregion

			#region TerminateOwnJobs
			jobSubmissionPortal.TerminateActivities(myJobs);
			#endregion

			#region JobStatusMapping
			var myJobStatusMapping = myStats.GetActivityStatusesResponse1.Response.Select(r => new { r.ActivityStatus.state, r.ActivityIdentifier }).ToList();
			#endregion

			#region TerminateWaitingJobs
			var myPendingJobs = myStats.GetActivityStatusesResponse1.Response.Where(r => r.ActivityStatus.state == ActivityStateEnumeration.Pending).Select(e => e.ActivityIdentifier).ToList();
			jobSubmissionPortal.TerminateActivities(myPendingJobs);
			#endregion

			#region RemoveTerminatedJobs
			jobSubmissionPortal.RemoveTerminatedJobs("Researcher");
			#endregion

			#region DownloadResults
			CancellationTokenSource cts = new CancellationTokenSource();

			Task jobResultPoller = new Task(() => pollForResults(userDataContainer, addressOutputFileDatastore, resultFileName,
				cts.Token, TimeSpan.FromSeconds(5)), cts.Token, TaskCreationOptions.AttachedToParent);
			jobResultPoller.Start();
			#endregion

		}

		#region CreateApplicationDescription
		public static VENUSApplicationDescription CreateApplicationDescription()
		{
			VENUSApplicationDescription applicationDescription = new VENUSApplicationDescription()
			{
				//This is the name where the application later gets identified
				//If you change your application, this must also be changed
				ApplicationIdentificationURI = applicationIdentificationURI,
				CommandTemplate = new VENUSCommandTemplate()
				{
					Path = string.Empty,
					Executable = "SimpleMathConsoleApp.exe",
					Args = new List<CommandLineArgument>() 
					{
						//InputFile is here a SingleReferenceInputArgument because it has to be downloaded 
						//from the Datastore before execution
						new CommandLineArgument(){
							Name = "InputFile",
							FormatString = "-infile {0}",
							Required = Required.Mandatory,
							CommandLineArgType = CommandLineArgType.SingleReferenceInputArgument
						},
						//Outputfile is a SingleReferenceOutputArgument because this file
						//gets uploaded to the Datastore after execution
						new CommandLineArgument(){
							Name = "OutputFile",
							FormatString = "-outfile {0}",
							Required = Required.Mandatory,
							CommandLineArgType = CommandLineArgType.SingleReferenceOutputArgument
						},
						//Operation is a Switch because it triggers the execution of the application
						new CommandLineArgument(){
							Name = "Operation",
							FormatString = "-sum",
							Required = Required.Optional,
							CommandLineArgType = CommandLineArgType.Switch
						},
						//Multiply is a SingleLiteralArgument because its value get set directly through the job description
						new CommandLineArgument(){
							Name = "Multiply",
							FormatString = "-mul {0}",
							Required = Required.Optional,
							CommandLineArgType = CommandLineArgType.SingleLiteralArgument
						}
					}
				}
			};
			return applicationDescription;
		}
		#endregion

		#region CreateJobDescription
		public static VENUSJobDescription createJobDescription(string addressInputFileDatastore, string addressOutputFileDatastore)
		{
			var account = CloudStorageAccount.Parse(azureStorageConnectionString);
			var blobClient = account.CreateCloudBlobClient();
			CloudBlobContainer appDataContainer = blobClient.GetContainerReference("applicationcontainer");
			appDataContainer.CreateIfNotExist();

			#region ProvideJobDescription
			VENUSJobDescription aSimpleJobDescription = new VENUSJobDescription()
			{
				ApplicationIdentificationURI = applicationIdentificationURI,
				CustomerJobID = "SimpleMathConsoleApp Job " + DateTime.Now.ToLocalTime().ToString(),
				JobName = "some job name",
				AppPkgReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(applicationIdentificationURI) + "_App"), azureStorageConnectionString)),
				AppDescReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(applicationIdentificationURI) + "_Desc"), azureStorageConnectionString)),
				JobArgs = new ArgumentCollection()
				{
					new SingleReference
					{
						Name="inputfile",
						Ref= new Reference(                                
								"InputFile",
								new AzureBlobReference
								{
									DataAddress = addressInputFileDatastore, 
									ConnectionString = azureStorageConnectionString,                            
								})
					},
					new SingleReference
					{
						Name="outputfile",
						Ref= new Reference(                                
								"OutputFile",
								new AzureBlobReference
								{
									DataAddress = addressOutputFileDatastore, 
									ConnectionString = azureStorageConnectionString,                            
								})
					},
					new SwitchArgument
					{
						Name = "Operation",
						Value = true
					},
					new LiteralArgument
					{
						Name = "Multiply",
						LiteralValue = "3"
					}   
				}
			};
			#endregion

			CloudBlob inputCloudBlob1, inputCloudBlob2;
			inputCloudBlob1 = inputCloudBlob2 = null;
			int numOutputs = 3;
			CloudBlobContainer userDataContainer = null;

			#region JobDescriptionWithDownloadsUploads            

			VENUSJobDescription aJobDescriptionWithUploadsDownloads = new VENUSJobDescription()
			{
				ApplicationIdentificationURI = applicationIdentificationURI,
				CustomerJobID = "SimpleMathConsoleApp Job " + DateTime.Now.ToLocalTime().ToString(),
				JobName = "some job name",
				AppPkgReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(applicationIdentificationURI) + "_App"), azureStorageConnectionString)),
				AppDescReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(applicationIdentificationURI) + "_Desc"), azureStorageConnectionString)),
				JobArgs = new ArgumentCollection()
				{
					new SwitchArgument
					{
						Name = "Operation",
						Value = true
					},
					new LiteralArgument
					{
						Name = "Multiply",
						LiteralValue = "3"
					}   
				},
				Downloads = new ReferenceCollection()
				{
					new Reference(new AzureBlobReference(inputCloudBlob1,azureStorageConnectionString)),
					new Reference(new AzureBlobReference(inputCloudBlob2,azureStorageConnectionString))
				},
				Uploads = new ReferenceCollection() { }
			};
			for (int i = 0; i < numOutputs; i++)
			{
				var outfileName = string.Format("Output{0}.txt", i);
				aJobDescriptionWithUploadsDownloads.Uploads.Add(new Reference(outfileName, new AzureBlobReference(userDataContainer.GetBlobReference(outfileName).Uri.AbsoluteUri, azureStorageConnectionString)));
			}

			#endregion

			#region SerializeANDUploadReferenceList
			Func<ReferenceCollection, bool, CloudBlob> uploadReferenceCollectionAndRetrieveBlob = ((referenceCollection, isUploadsCollection) =>
			{
				var postfix = (isUploadsCollection) ? "UploadsCollection" : "DownloadsCollection";
				var blobName = Guid.NewGuid().ToString() + postfix;


				var ra = new ReferenceArray() { Name = postfix, References = referenceCollection };

				var xmlDoc = new XmlDocument();
				var serializedRefArr = ra.Serialize(xmlDoc);
				xmlDoc.AppendChild(serializedRefArr);

				CloudBlob xmlBlob;

				xmlBlob = userDataContainer.GetBlobReference(blobName);
				xmlBlob.Properties.ContentType = "text/xml";
				xmlBlob.UploadText(xmlDoc.InnerXml);

				return xmlBlob;
			});
			#endregion

			#region JobDescriptionWithDownloadsUploadsRefs
			var aJobDescriptionWithUploadsRef = new VENUSJobDescription()
			{
				ApplicationIdentificationURI = applicationIdentificationURI,
				CustomerJobID = "SimpleMathConsoleApp Job " + DateTime.Now.ToLocalTime().ToString(),
				JobName = "some job name",
				AppPkgReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(applicationIdentificationURI) + "_App"), azureStorageConnectionString)),
				AppDescReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(applicationIdentificationURI) + "_Desc"), azureStorageConnectionString)),
				JobArgs = new ArgumentCollection()
				{
					new SwitchArgument
					{
						Name = "Operation",
						Value = true
					},
					new LiteralArgument
					{
						Name = "Multiply",
						LiteralValue = "3"
					}                          
				}
			};


			//add uploads to job
			//create your own ReferenceCollection:
			var outRefCol = new ReferenceCollection();

			//add References to it as you did with the Uploads and Downloads property:
			for (int i = 0; i < numOutputs; i++)
			{
				var outfileName = string.Format("Output{0}.txt", i);
				outRefCol.Add(new Reference(outfileName, new AzureBlobReference(userDataContainer.GetBlobReference(outfileName).Uri.AbsoluteUri, azureStorageConnectionString)));
			}

			//new:
			var uploadsRefListBlob = uploadReferenceCollectionAndRetrieveBlob(outRefCol, true);
			aJobDescriptionWithUploadsRef.UploadsReference = new Reference(new AzureBlobReference(uploadsRefListBlob, azureStorageConnectionString));
			#endregion

			//#region JobDescriptionWithDownloadsUploadsRefsSplitter
			//var splitterJob = new VENUSJobDescription()
			//{
			//    ApplicationIdentificationURI = applicationIdentificationURI,
			//    CustomerJobID = "UPVBIO Splitter Job " + DateTime.Now.ToLocalTime().ToString(),
			//    JobName = "some job name",
			//    AppPkgReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(applicationIdentificationURI) + "_App"), azureStorageConnectionString)),
			//    AppDescReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(applicationIdentificationURI) + "_Desc"), azureStorageConnectionString)),
			//    JobArgs = new ArgumentCollection()
			//    {
			//            new SingleReference
			//            {
			//                Name="inputfile",
			//                Ref= new Reference(
			//                        inputFile,
			//                        new AzureBlobReference
			//                        {
			//                            DataAddress = addressInputFile, 
			//                            ConnectionString = azureStorageConnectionString,                            
			//                        })
			//            },
			//            new LiteralArgument
			//            {
			//                Name = "numfragments",
			//                LiteralValue = String.Format("{0}", numFragments)
			//            },
			//            new LiteralArgument
			//            {
			//                Name = "startfragment",
			//                LiteralValue = "0"
			//            }                        
			//    }
			//};


			////add uploads to job
			////create your own ReferenceCollection:
			//var refCol = new ReferenceCollection();

			////add References to it as you did with the Uploads and Downloads property:
			//for (int i = 0; i < numFragments; i++)
			//{
			//    var seqfileName = string.Format("seqfile{0}.sqf", i);
			//    refCol.Add(new Reference(seqfileName, new AzureBlobReference(userDataContainer.GetBlobReference(seqfileName).Uri.AbsoluteUri, azureStorageConnectionString)));
			//}

			////new:
			//var uploadsRefBlobSplitter = uploadReferenceCollectionAndRetrieveBlob(refCol, true);
			//splitterJob.UploadsReference = new Reference(new AzureBlobReference(uploadsRefBlobSplitter, azureStorageConnectionString));
			//#endregion

			return aSimpleJobDescription;
		}
		#endregion



		#region PackageApplication
		public static MemoryStream PackageApplication(string[] applicationFiles)
		{

			// All application file gets zipped and returned as byte array 
			var memoryStream = new MemoryStream();
			using (var zip = new ZipFile())
			{
				foreach (string file in applicationFiles)
					zip.AddFile(file, "");
				zip.Save(memoryStream);
			}

			return memoryStream;
		}
		#endregion

		#region RegisterApplication
		public static void UploadAndRegisterApplication(MemoryStream zippedApplication, VENUSApplicationDescription applicationDescription)
		{
			var account = CloudStorageAccount.Parse(azureStorageConnectionString);
			var blobClient = account.CreateCloudBlobClient();
			CloudBlobContainer appDataContainer = blobClient.GetContainerReference("applicationcontainer");
			appDataContainer.CreateIfNotExist();

			Func<string, VENUSApplicationDescription, string> uploadAppDesc = ((name, appDescription) =>
			{
				DataContractSerializer dcs = new DataContractSerializer(typeof(VENUSApplicationDescription));
				MemoryStream msxml = new MemoryStream();
				dcs.WriteObject(msxml, appDescription);
				CloudBlob xmlBlob = appDataContainer.GetBlobReference(name);
				xmlBlob.Properties.ContentType = "text/xml";
				xmlBlob.UploadByteArray(msxml.ToArray());
				var blobAddress = xmlBlob.Uri.AbsoluteUri;
				return blobAddress;
			});

			Action<string, MemoryStream, VENUSApplicationDescription> uploadApp = (blobName, zipBytes, appDesc) =>
			{
				CloudBlob splitterBlob = appDataContainer.GetBlobReference(blobName + "_App");
				splitterBlob.UploadByteArray(zipBytes.ToArray());

				uploadAppDesc(blobName + "_Desc", appDesc);
			};

			uploadApp(HttpUtility.UrlEncode(applicationDescription.ApplicationIdentificationURI), zippedApplication, applicationDescription);

		}
		#endregion

		#region HelperMethods

		/// <summary>
		/// Uploads the file to the given blob container with the given name
		/// </summary>
		/// <param name="userDataContainer">The user data container.</param>
		/// <param name="blobname">The blobname.</param>
		/// <param name="filename">The local filename/path.</param>
		/// <returns>The blob adress of the uploaded file.</returns>
		#region uploadFileMethod
		public static string uploadFile(CloudBlobContainer userDataContainer, string blobname, string filename)
		{
			CloudBlob blob = userDataContainer.GetBlobReference(blobname);
			blob.UploadFile(filename);
			return blob.Uri.AbsoluteUri;
		}
		#endregion
		/// <summary>
		/// Computes a name of a given or new file in the blob container and returns its URI adress
		/// </summary>
		/// <param name="userDataContainer">The user data container.</param>
		/// <param name="name">The name of the file.</param>
		/// <returns>Returns the URI of the file in this blob container.</returns>
		#region getBlobAdressMethod
		public static string getBlobAdress(CloudBlobContainer userDataContainer, string name)
		{
			return userDataContainer.GetBlobReference(name).Uri.AbsoluteUri;
		}
		#endregion

		/// <summary>
		/// Connects to a blob container in the cloud storage.
		/// </summary>
		/// <param name="containerName">Name of the container.</param>
		/// <returns>The blob container</returns>
		#region connectTocloudStorageContainerMethod
		public static CloudBlobContainer connectToCloudStorageContainer(string containerName)
		{
			CloudStorageAccount account = CloudStorageAccount.Parse(azureStorageConnectionString);
			CloudBlobClient blobClient = account.CreateCloudBlobClient();
			CloudBlobContainer blobContainer = blobClient.GetContainerReference(containerName);
			blobContainer.CreateIfNotExist();
			return blobContainer;
		}
		#endregion
		#endregion

		#region PollForResults
		private static void pollForResults(CloudBlobContainer container, string blobName, string filename, CancellationToken cts, TimeSpan interval)
		{
			CloudBlob blobRef = container.GetBlobReference(blobName);
			while (!cts.IsCancellationRequested)
			{
				try
				{
					blobRef.DownloadToFile(filename);
					return;
				}
				catch (StorageClientException)
				{
					Thread.Sleep(interval);
				}
			}
		}
		#endregion

		#region Scaling
		private static void scaling()
		{
			#region CreateScalingClient

			Func<ScalingServiceClient> CreateSecureScalingClient = () =>
			{
				return ScalingServiceClient.CreateSecureClient(
					address: new EndpointAddress(new Uri(secureScalingURL), new DnsEndpointIdentity("myexperiment.cloudapp.net")),
					issuer: new EndpointAddress(new Uri(securityTokenServiceURL), new DnsEndpointIdentity("myexperiment.cloudapp.net")),
					username: "Administrator",
					password: "secret",
					serviceCert: X509Helper.GetX509Certificate2(
						StoreLocation.LocalMachine, StoreName.My,
						"123456789ABCDEF123456789ABCDEF123456789A",
						X509FindType.FindByThumbprint));
			};

			var scalingClient = CreateSecureScalingClient();

			#endregion

			#region getDeploymentsList

			var deployments = scalingClient.ListDeployments();

			#endregion

			#region increaseInstanceNumber

			var favCloudProvider = "Azure";
			var favCloudProviderInstances = deployments.Where(d => d.CloudProviderID == favCloudProvider).Select(d => d.InstanceCount).FirstOrDefault();

			scalingClient.UpdateDeployment(new DeploymentSize() { CloudProviderID = favCloudProvider, InstanceCount = favCloudProviderInstances + 1 });

			#endregion

			#region removeDeployment
			scalingClient.UpdateDeployment(new DeploymentSize() { CloudProviderID = favCloudProvider, InstanceCount = 0 });
			#endregion

			#region addNewDeployment
			scalingClient.UpdateDeployment(new DeploymentSize() { CloudProviderID = favCloudProvider, InstanceCount = 4 });
			#endregion
		}
		#endregion

		#region LocalJobSubmission



		private static GenericWorkerJobManagementClient CreateLocalJobSubmissionClient()
		{
			#region CreateLocalJobSubmissionClient
			var serviceBinding = new WS2007HttpBinding(SecurityMode.None, reliableSessionEnabled: false);

			var localJobSubmissionUri = System.Environment.GetEnvironmentVariable("jobSubmissionEndpoint");

			if (localJobSubmissionUri == null)
				localJobSubmissionUri = "http://localhost/AcceptLocalJobs/dev_Enviroment_Port1";

			var localJobSubmissionClient = new GenericWorkerJobManagementClient(
				serviceBinding,
				new EndpointAddress(
					new Uri(localJobSubmissionUri)));

			#endregion
			return localJobSubmissionClient;
		}


		private static void ExampleLocalJobSubmission()
		{
			CloudBlobContainer userDataContainer = connectToCloudStorageContainer("userdatacontainer");

			string inputFileLocal = "../input.csv";
			string inputFileName = "input.csv";
			string inputFileAlreadyInDatastore = "otherinput.csv";
			string resultFileName = "result.txt";
			string addressInputFileDatastore = string.Empty;
			string addressOutputFileDatastore = string.Empty;

			//Upload local file to the datastore
			addressInputFileDatastore = uploadFile(userDataContainer, inputFileName, inputFileLocal);

			//Inputfile is already in datastore so return its adress
			addressInputFileDatastore = getBlobAdress(userDataContainer, inputFileAlreadyInDatastore);

			//Outputfile is not in the datastore so get an adress for it
			addressOutputFileDatastore = getBlobAdress(userDataContainer, resultFileName);

			#region SubmitASingleLocalJob

			VENUSJobDescription aSimpleJobDescription = createJobDescription(addressInputFileDatastore, addressOutputFileDatastore);
			var localJobSubmissionClient = CreateLocalJobSubmissionClient();
			localJobSubmissionClient.SubmitVENUSJob(aSimpleJobDescription);

			#endregion

			#region TestJobID
			var rootID = "jobid://Root";
			JobID jobId = null;
			var isHierarchical = JobID.TryParse(rootID, out jobId);

			#endregion

			#region SubmitAHierarchicalJob

			string childJobId;
			try
			{
				var parentId = new JobID(System.Environment.GetEnvironmentVariable("jobId"));
				childJobId = parentId.CreateChildJob(String.Format("childjob#{0}", 1)).ToString();
			}
			catch (NotSupportedException)
			{
				throw new Exception(ExceptionMessages.ParentJobID);
			}

			VENUSJobDescription hierarchicalSimpleJobDescription = createJobDescription(addressInputFileDatastore, addressOutputFileDatastore);
			hierarchicalSimpleJobDescription.CustomerJobID = childJobId;
			localJobSubmissionClient.SubmitVENUSJob(hierarchicalSimpleJobDescription);

			#endregion


		}

		#endregion

		#region CreateJobAsPartOfAGroup
		public static VENUSJobDescription CreateGroupJSDL(string groupName, string jobName)
		{
			return new VENUSJobDescription()
			{
				ApplicationIdentificationURI = "http://www.microsoft.com/someapp",
				CustomerJobID = string.Format("GroupID://{0}/{1}", groupName, jobName),
				JobName = "Demo job for demonstrating group definitions"
			};
		}
		#endregion

	   
		private static void grouping()
		{
			var someOtherJobNotPartOfAGroup = new VENUSJobDescription(); 

			#region SubmitJobsForSameGroup
			var user = "Researcher";

			GenericWorkerJobManagementClient jobSubmissionPortal = GenericWorkerJobManagementClient.CreateSecureClient(
				new EndpointAddress(new Uri(secureGenericWorkerURL), new DnsEndpointIdentity("myexperiment.cloudapp.net")),
				new EndpointAddress(new Uri(securityTokenServiceURL), new DnsEndpointIdentity("myexperiment.cloudapp.net")),
				user, "secret",
				X509Helper.GetX509Certificate2(
					StoreLocation.LocalMachine, StoreName.My,
					"123456789ABCDEF123456789ABCDEF123456789A",
					X509FindType.FindByThumbprint));
			var groupName = "someGroupName";

			jobSubmissionPortal.SubmitVENUSJob(CreateGroupJSDL(groupName,"job1"));
			jobSubmissionPortal.SubmitVENUSJob(CreateGroupJSDL(groupName, "job2"));
			jobSubmissionPortal.SubmitVENUSJob(CreateGroupJSDL(groupName, "job3"));
			jobSubmissionPortal.SubmitVENUSJob(CreateGroupJSDL(groupName, "job4"));
			#endregion

			#region SubmitIndependentJob
			jobSubmissionPortal.SubmitVENUSJob(someOtherJobNotPartOfAGroup);
			#endregion

			#region RetrieveJobsOfAGroup
			var jobGroup = jobSubmissionPortal.GetJobsByGroup(user, groupName);
			#endregion

			#region CancelGroup
			jobSubmissionPortal.CancelGroup(user, groupName);
			#endregion
		}

		#region HierarchyPolling
		
		private static void Hierarchy()
		{
			#region RootSubmission
			var user = "Researcher";

			GenericWorkerJobManagementClient jobManagementPortal = GenericWorkerJobManagementClient.CreateSecureClient(
				new EndpointAddress(new Uri(secureGenericWorkerURL), new DnsEndpointIdentity("myexperiment.cloudapp.net")),
				new EndpointAddress(new Uri(securityTokenServiceURL), new DnsEndpointIdentity("myexperiment.cloudapp.net")),
				user, "secret",
				X509Helper.GetX509Certificate2(
					StoreLocation.LocalMachine, StoreName.My,
					"123456789ABCDEF123456789ABCDEF123456789A",
					X509FindType.FindByThumbprint));

			CreateActivityResponse resp = jobManagementPortal.SubmitVENUSJob(new VENUSJobDescription()
			{
				ApplicationIdentificationURI = "http://www.microsoft.com/someapp",
				CustomerJobID = "jobid://Root",
				JobName = "Demo job for demonstrating group definitions"
			});
			#endregion

			#region RetrieveHierarchy
			EndpointReferenceType root = resp.CreateActivityResponse1.ActivityIdentifier;
			var jobHierarchy = jobManagementPortal.GetHierarchy(root);
			#endregion

			#region GetRoot
			EndpointReferenceType someJob = jobHierarchy.ElementAt(2);
			root = jobManagementPortal.GetRoot(someJob);
			#endregion

			#region CancelHierarchy
			jobManagementPortal.CancelHierarchy(root);
			#endregion
		}

		#endregion

		#region AccountingPusher
		private static void AccountingPusher(AccountingTableEntity accountingInfo, string serverUrl, string userName, string passwd)
		{
			VenusAccountingClient client = new VenusAccountingClient(serverUrl, userName, passwd);

			JobResourceUsage jobResourceUsage = new JobResourceUsage();
			jobResourceUsage.ConsumerID = accountingInfo.PartitionKey;
			jobResourceUsage.CreateTime = accountingInfo.StartTime.Value;
			jobResourceUsage.CPUDuration = 0;
			jobResourceUsage.CreatorID = "Azure Accounting Connector";
			jobResourceUsage.Disk = 0;
			jobResourceUsage.EndTime = accountingInfo.EndTime.Value;
			jobResourceUsage.ID = accountingInfo.RowKey;
			jobResourceUsage.JobEndTime = accountingInfo.EndTime.Value;
			jobResourceUsage.JobID = accountingInfo.RowKey;
			jobResourceUsage.JobName = accountingInfo.CustomerJobID;
			jobResourceUsage.JobStartTime = accountingInfo.StartTime.Value;
			jobResourceUsage.Memory = 0;
			jobResourceUsage.NumberofCores = accountingInfo.NumberofCores;
			jobResourceUsage.Processors = accountingInfo.NumberofCores;
			jobResourceUsage.NetworkIn = accountingInfo.ReceivedBytesEnd - accountingInfo.ReceivedBytesStart;
			jobResourceUsage.NetworkOut = accountingInfo.SentBytesEnd - accountingInfo.SentBytesStart;
			jobResourceUsage.NumberOfInputFiles = accountingInfo.NumberOfInputFiles;
			jobResourceUsage.NumberOfOutputFiles = accountingInfo.NumberofOutputFiles;
			jobResourceUsage.refVM = accountingInfo.InstanceID;
			jobResourceUsage.ResourceOwner = "Azure Accounting Connector";
			jobResourceUsage.ResourceType = "job";
			jobResourceUsage.SizeOfInputFiles = accountingInfo.SizeofInputFiles;
			jobResourceUsage.SizeOfOutputFiles = accountingInfo.SizeofOutputFiles;
			jobResourceUsage.StartTime = accountingInfo.StartTime.Value;
			jobResourceUsage.Status = accountingInfo.Status;
			jobResourceUsage.WallDuration = 0;

			client.Post(jobResourceUsage);
		}

		#endregion
		private static MemoryStream createXMLMemoryStreamOfAppDescription(VENUSApplicationDescription description)
		{
			//open streams and writer
			MemoryStream stream = new MemoryStream();
			XmlTextWriter writer = new XmlTextWriter(stream, null);
			//set formating
			writer.Formatting = Formatting.Indented;
			writer.Indentation = 2;
			writer.IndentChar = ' ';

			//serialize object
			XmlSerializer serializer = new XmlSerializer(typeof(VENUSApplicationDescription));
			serializer.Serialize(writer, description);
			writer.Close();

			return stream;
		}

		private static void security()
		{
			#region JobSubmissionSecurity
			GenericWorkerJobManagementClient jobSubmissionPortal = GenericWorkerJobManagementClient.CreateSecureClient(
				new EndpointAddress(new Uri(secureGenericWorkerURL), new DnsEndpointIdentity("myexperiment.cloudapp.net")),
				new EndpointAddress(new Uri(securityTokenServiceURL), new DnsEndpointIdentity("myexperiment.cloudapp.net")),
				"Researcher", "secret",
				X509Helper.GetX509Certificate2(
					StoreLocation.LocalMachine, StoreName.My,
					"123456789ABCDEF123456789ABCDEF123456789A",
					X509FindType.FindByThumbprint));
			#endregion

			#region JobNotificationSecurity
			var notificationClient = NotificationServiceClient.CreateSecureClient(
				address: new EndpointAddress(new Uri(secureNotificationServiceUrl), new DnsEndpointIdentity("myexperiment.cloudapp.net")),
				issuer: new EndpointAddress(new Uri(securityTokenServiceURL), new DnsEndpointIdentity("myexperiment.cloudapp.net")),
				username: "Researcher",
				password: "secret",
				serviceCert: X509Helper.GetX509Certificate2(
					StoreLocation.LocalMachine, StoreName.My,
					"123456789ABCDEF123456789ABCDEF123456789A",
					X509FindType.FindByThumbprint));
			#endregion
		}

		private static void downloadUploadJobDescription(CloudBlobContainer userDataContainer)
		{
			#region DownloadUploadJobDescription
			Func<string, string> computeName = ((name) =>
			{
				var result = userDataContainer.GetBlobReference(name).Uri.AbsoluteUri;
				return result;
			});

			var job = new VENUSJobDescription()
			{
				ApplicationIdentificationURI = applicationIdentificationURI,
				CustomerJobID = "Download/Upload Task Job " + DateTime.Now.ToLocalTime().ToString(),
				JobName = "some job name",
				ResultZipFilename = "everything.zip",
				JobArgs = new ArgumentCollection()
				{
					new LiteralArgument
					{
						Name = "action",
						LiteralValue = "someLiteral"
					}
				},
				Downloads = new ReferenceCollection()
				{
					new Reference(new AzureBlobReference(computeName("downloadfile1.txt") ,azureStorageConnectionString)),
					new Reference(new AzureBlobReference(computeName("downloadfile2.txt"), azureStorageConnectionString))   
				},
				Uploads = new ReferenceCollection()
				{
					new Reference(new AzureBlobReference(computeName("resultfile1.txt"), azureStorageConnectionString)),
					new Reference(new AzureBlobReference(computeName("resultfile2.txt"), azureStorageConnectionString))
				}

			};
			#endregion
		}

		private static void SubmitPasswordJobWithoutFederation()
		{
			VENUSJobDescription aSimpleJobDescription = null;
			#region SubmitJobWithoutSTS
			var secureGenericWorkerURL = "http://localhost:81/SubmissionService/UsernamePasswordSample.svc";
			GenericWorkerJobManagementClient jobSubmissionPortal = new GenericWorkerJobManagementClient(
				WCFUtils.CreateUsernamePasswordSecurityTokenServiceBinding(),
			   new EndpointAddress(new Uri(secureGenericWorkerURL), new DnsEndpointIdentity("myexperiment.cloudapp.net")));

			jobSubmissionPortal.ClientCredentials.UserName.UserName = "ResearchUser";
			jobSubmissionPortal.ClientCredentials.UserName.Password = "somePassword";

			jobSubmissionPortal.SubmitVENUSJob(aSimpleJobDescription);
			#endregion
		}
	}
}

