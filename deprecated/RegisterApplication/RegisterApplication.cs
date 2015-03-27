//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using System.Text;
using Ionic.Zip;
using Microsoft.EMIC.Cloud.ApplicationRepository;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.EMIC.Cloud.Demo;
using Microsoft.EMIC.Cloud.DataManagement;
using Microsoft.EMIC.Cloud.Storage.Azure;
using System.Web;
using System.Runtime.Serialization;

namespace RegisterApplication
{
	/// <summary>
	/// Command-line utility to register e-Science applications with teh application store. 
	/// </summary>
	class RegisterApplication
	{
        private static string AppURI = "http://www.microsoft.com/emic/cloud/samples/e-ScienceApp#v1.0";

		static void Main(string[] args)
		{

			Console.Title = "RegisterApplication";
			Console.Write("Press return to invoke");
			Console.ReadLine();


			var folderName = @"..\..\..\SimpleMathConsoleApp\bin\Debug";
			var mainExe = Path.Combine(folderName, "SimpleMathConsoleApp.exe");

			var folder = new DirectoryInfo(folderName);
			if (!Directory.Exists(folderName))
			{
				Console.WriteLine("Does not exist: " + folder.FullName);
				return;
			}

			// FileInfo zipFileName = new FileInfo(Path.GetTempFileName() + ".zip");
			var ms = new MemoryStream();
			using (var zipFile = new ZipFile(/*zipFileName.FullName*/))
			{
				zipFile.AddDirectory(folder.FullName);
				zipFile.Save(ms);
			}

			var description = new VENUSApplicationDescription()
			{
                ApplicationIdentificationURI = AppURI,
				Author = new VENUSApplicationAuthor()
				{
					Company = "Microsoft",
					EMail = "chgeuer@microsoft.com"
				},
				Dependencies = new List<CloudApplicationDependencies>()
				{
					new CloudApplicationDependencies("Java", "5.0"), 
					new CloudApplicationDependencies("Windows Azure","1.4"), 
					new CloudApplicationDependencies(".NET", "4.0")
				},
				CommandTemplate = new VENUSCommandTemplate()
				{
					Executable = mainExe,
					Path = "./bin;./lib",
					WorkingDirectory = "./bin",
					Args = new List<CommandLineArgument>()
					{
						new CommandLineArgument() 
						{
							Name = "INPUT",
							FormatString = "-in {0}",
							Required = Required.Mandatory, 
							CommandLineArgType = CommandLineArgType.SingleReferenceInputArgument
						}
					}
				}
			};

            var UserDataStoreConnectionString = CloudSettings.UserDataStoreConnectionString;
            var account = CloudStorageAccount.Parse(UserDataStoreConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer appDataContainer = blobClient.GetContainerReference("applicationcontainer");
            appDataContainer.CreateIfNotExist();


            Func<VENUSApplicationDescription, CloudBlob> uploadAppDesc = ((appDescription) =>
            {
                var blobName = HttpUtility.UrlEncode(appDescription.ApplicationIdentificationURI) + "_Desc";
                DataContractSerializer dcs = new DataContractSerializer(typeof(VENUSApplicationDescription));
                MemoryStream msxml = new MemoryStream();
                dcs.WriteObject(msxml, appDescription);
                CloudBlob xmlBlob = appDataContainer.GetBlobReference(blobName);
                xmlBlob.Properties.ContentType = "text/xml";
                xmlBlob.UploadByteArray(msxml.ToArray());
                return xmlBlob;
            });

            Func<string, MemoryStream, CloudBlob> uploadApp = (appURI, zipBytes) =>
            {
                var blobName = HttpUtility.UrlEncode(appURI) + "_App";
                CloudBlob applicationBlob = appDataContainer.GetBlobReference(blobName);
                applicationBlob.UploadByteArray(zipBytes.ToArray());

                Console.WriteLine(string.Format("Uploaded {0} bytes", zipBytes.Length));
                return applicationBlob;
            };

            Action<VENUSApplicationDescription, MemoryStream> uploadPkg = (appDesc, zipBytes) =>
            {
                uploadApp(appDesc.ApplicationIdentificationURI, zipBytes);
                uploadAppDesc(appDesc);
            };

            uploadPkg(description, ms);

			Console.WriteLine("Press <Return> to delete");
			Console.ReadLine();

            var appBlob = appDataContainer.GetBlobReference(HttpUtility.UrlEncode(AppURI) + "_App");
            var descBlob = appDataContainer.GetBlobReference(HttpUtility.UrlEncode(AppURI) + "_Desc");

            appBlob.DeleteIfExists();
            descBlob.DeleteIfExists();
		}
	}
}