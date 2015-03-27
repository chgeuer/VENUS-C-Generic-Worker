//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using Ionic.Zip;
using System.Runtime.Serialization;
using Microsoft.EMIC.Cloud.ApplicationRepository;
using Microsoft.EMIC.Cloud.GenericWorker;
using Microsoft.EMIC.Cloud.DataManagement;
using Microsoft.EMIC.Cloud.Security;
using Microsoft.EMIC.Cloud.Storage.Azure;
using Microsoft.EMIC.Cloud.Utilities;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.Threading.Tasks;
using System.Threading;
using System.Web;

namespace UPVBioInstaller
{
    class Program
    {
        const int numFragments = 3;
        /// <summary>
        /// This installer uploads the files needed to run the UPVBioClient. 
        /// In the installer the application package and description for these executables are defined 
        /// so that these packages and description files are created and uploaded to the server.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Console.Title = "UPVBio Installer Client";

            Console.Write("Press <return> to start");
            Console.ReadLine();


            Action<ConsoleColor, string> print = (color, str) =>
            {
                Console.ForegroundColor = color;
                Console.Out.WriteLine(str);
                Console.ResetColor();
            };

            string upvBioURIPrefix = "http://www.upvbio.eu/cloud/demo/gw/UPVBIOApp/";
            string splitterAppIdentificationURI = upvBioURIPrefix + "Splitter";
            string blastAppIdentificationURI = upvBioURIPrefix + "Blast";
            string assemblerAppIdentificationURI = upvBioURIPrefix + "Assembler";

            #region Zip applications

            // method for creating application package zip file
            Func<string[], MemoryStream> createZippedAppPkg = (filenameArray) =>
            {
                MemoryStream AppZipBytes = new MemoryStream();
                using (var zip = new ZipFile())
                {
                    foreach (string file in filenameArray)
                        zip.AddFile(file, "");
                    zip.Save(AppZipBytes);
                }
                AppZipBytes.Seek(0L, SeekOrigin.Begin);
                return AppZipBytes;
            };


            string[] splitterFiles = { @".\Splitter.exe" };
            MemoryStream splitterAppZipBytes = createZippedAppPkg(splitterFiles);

            string[] blastFiles = { @".\blastall.exe" };
            MemoryStream blastAppZipBytes = createZippedAppPkg(blastFiles);

            string[] assemblerFiles = { @".\Assembler.exe" };
            MemoryStream assemblerAppZipBytes = createZippedAppPkg(assemblerFiles);

            #endregion

            #region Specify application arguments

            #region Specify Splitter App
            VENUSApplicationDescription splitterAppDescription = new VENUSApplicationDescription()
            {
                ApplicationIdentificationURI = splitterAppIdentificationURI,
                CommandTemplate = new VENUSCommandTemplate()
                {
                    Path = string.Empty,
                    Executable = "Splitter.exe",
                    Args = new List<CommandLineArgument>() 
                        {
                            new CommandLineArgument(){
                                Name = "inputfile",
                                FormatString = "{0}",
                                Required = Required.Mandatory,
                                CommandLineArgType = CommandLineArgType.SingleReferenceInputArgument
                            },
                            new CommandLineArgument() 
                            { 
                                Name = "numfragments", 
                                FormatString = "{0}", 
                                Required = Required.Mandatory,
                                CommandLineArgType = CommandLineArgType.SingleLiteralArgument
                            },
                            new CommandLineArgument() 
                            { 
                                Name = "startfragment", 
                                FormatString = "{0}", 
                                Required = Required.Mandatory,
                                CommandLineArgType = CommandLineArgType.SingleLiteralArgument
                            }
                        }
                }
            };
            #endregion

            #region Specify BLAST App
            VENUSApplicationDescription blastAppDescription = new VENUSApplicationDescription()
            {
                ApplicationIdentificationURI = blastAppIdentificationURI,
                CommandTemplate = new VENUSCommandTemplate()
                {
                    Path = string.Empty,
                    Executable = "blastall.exe",
                    Args = new List<CommandLineArgument>() 
                    {
                        new CommandLineArgument() 
                        { 
                            Name = "programName", 
                            FormatString = "-p {0}",                           
                            Required = Required.Mandatory,
                            CommandLineArgType = CommandLineArgType.SingleLiteralArgument
                        },
                        new CommandLineArgument(){
                            Name = "databaseName",
                            FormatString = "-d {0}",
                            Required = Required.Mandatory,
                            CommandLineArgType = CommandLineArgType.SingleLiteralArgument
                        },
                        new CommandLineArgument()
                        {
                            Name = "inputfile",
                            FormatString = "-i {0}",
                            Required = Required.Mandatory,
                            CommandLineArgType = CommandLineArgType.SingleReferenceInputArgument
                        },
                        new CommandLineArgument(){
                            Name = "expectationValue",
                            FormatString = "-e {0}",
                            Required = Required.Mandatory,
                            CommandLineArgType = CommandLineArgType.SingleLiteralArgument
                        },
                        new CommandLineArgument()
                        {
                            Name = "outputfile",
                            FormatString = "-o {0}",
                            Required = Required.Mandatory,
                            CommandLineArgType = CommandLineArgType.SingleReferenceOutputArgument
                        },
                        new CommandLineArgument(){
                            Name = "numDbSequencesDescription",
                            FormatString = "-v {0}",
                            Required = Required.Mandatory,
                            CommandLineArgType = CommandLineArgType.SingleLiteralArgument
                        },
                        new CommandLineArgument(){
                            Name = "numDbSequencesAlignment",
                            FormatString = "-b {0}",
                            Required = Required.Mandatory,
                            CommandLineArgType = CommandLineArgType.SingleLiteralArgument
                        }
                    }
                }
            };
            #endregion

            #region Specify Assembler Apps
            VENUSApplicationDescription assemblerAppDescription = new VENUSApplicationDescription()
            {
                ApplicationIdentificationURI = assemblerAppIdentificationURI,
                CommandTemplate = new VENUSCommandTemplate()
                {
                    Path = string.Empty,
                    Executable = "Assembler.exe",
                    Args = new List<CommandLineArgument>() 
                    {
                        new CommandLineArgument() 
                        { 
                            Name = "resultFileName", 
                            FormatString = "{0}", 
                            Required = Required.Mandatory,
                            CommandLineArgType = CommandLineArgType.SingleReferenceOutputArgument
                        }
                    }
                }
            };

            #endregion
            #endregion

            #region Upload Apps to AppStore

            var UserDataStoreConnectionString = ConfigurationManager.AppSettings["Microsoft.EMIC.Cloud.Demo.User.ConnectionString"];
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

                print(ConsoleColor.Green, string.Format("Uploaded {0} bytes", zipBytes.Length));
                return applicationBlob;                
            };

            Action<VENUSApplicationDescription,MemoryStream > uploadPkg = (appDesc,zipBytes) =>
            {
                uploadApp(appDesc.ApplicationIdentificationURI, zipBytes);
                uploadAppDesc(appDesc);
            };

            uploadPkg(splitterAppDescription, splitterAppZipBytes);
            uploadPkg(blastAppDescription, blastAppZipBytes);
            uploadPkg(assemblerAppDescription, assemblerAppZipBytes);

            #endregion

            Console.WriteLine("Press enter to quit the installer");
            Console.ReadLine();
        }
    }
    
    
}
