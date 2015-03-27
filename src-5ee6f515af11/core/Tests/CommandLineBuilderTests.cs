//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.ServiceModel;
using System.Threading;
using Microsoft.EMIC.Cloud.Administrator.Host;
using Microsoft.EMIC.Cloud.ApplicationRepository;
using Microsoft.EMIC.Cloud.DataManagement;
using Microsoft.EMIC.Cloud.Demo;
using Microsoft.EMIC.Cloud.GenericWorker;
using Microsoft.EMIC.Cloud.GenericWorker.AzureProvider;
using Microsoft.EMIC.Cloud.Storage.Azure;
using Microsoft.EMIC.Cloud.UserAdministration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Tests
{
    public class TestJob : IJob
    {
        public TestJob()
        {
            this.Owner = "Bob";
            this.InternalJobID = "id-12345";
            this.CustomerJobID = "Customer Job ID 123";
            this.Status = JobStatus.Submitted;
            this.ApplicationIdentificationURI = "http://algo";
        }

        public TestJob(string owner, string internalJobID)
        {
            this.Owner = owner;
            this.InternalJobID = internalJobID;
            this.CustomerJobID = "Customer Job ID 123";
            this.Status = JobStatus.Submitted;
            this.ApplicationIdentificationURI = "http://algo";
        }

        public string Owner { get; set; }

        public string CustomerJobID { get; set; }

        public string ApplicationIdentificationURI { get; private set; }

        public string InternalJobID { get; set; }

        public JobStatus Status { get; set; }

        public DateTime LastChange { get; set; }

        public int ResetCounter { get; set; }


        public string InstanceID { get; set; }

        public string Stdout { get; set; }

        public string Stderr { get; set; }

        public DateTime? Submission { get; set; }

        public DateTime? Start { get; set; }

        public DateTime? End { get; set; }

        public DateTime? DataChecked { get; set; }


        internal VENUSJobDescription VENUSJobDescription { get; set; }
        public VENUSJobDescription GetVENUSJobDescription()
        {
            return this.VENUSJobDescription;
        }

        public string StatusText { get; set; }
    }

    [TestClass]
    public class CommandLineBuilderTests
    {
        private static ProfileData t_ProfileData;

        private static CancellationTokenSource t_cts = new CancellationTokenSource();

        private static FilesystemMapper t_FilesystemMapper;

        private static string t_jobOwner;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            var container = new CompositionContainer(
                   new AggregateCatalog(
                       new TypeCatalog(typeof(CloudSettings)),
                       new AssemblyCatalog(typeof(BESFactoryPortTypeImplService).Assembly),
                       new AssemblyCatalog(typeof(AzureGWRuntimeEnvironment).Assembly),
                       new AssemblyCatalog(typeof(AzureArgumentSingleReference).Assembly)
                   ));
            t_FilesystemMapper = container.GetExportedValue<FilesystemMapper>();
          
            CreateProfileClient.CreateClientIdentityFileForService(CloudSettings.ProcessIdentityFilename);

            HostState state = new HostState(); 
            var t = CreateProfileHost.CreateTask(CloudSettings.ProcessIdentityFilename, t_cts, state);
            t.Start();

            while (state.Status != CommunicationState.Opened)
            {
                Thread.Sleep(200);
            }

            t_ProfileData = CreateProfileClient.GetLocalUser(
                "testuser", CloudSettings.GenericWorkerDirectoryUserFolder);
            t_jobOwner = "Jim";
           var appId = new VENUSApplicationDescription() { ApplicationIdentificationURI = "http://algo" }.ApplicationID;
           var folder = Path.Combine(t_FilesystemMapper.GenericWorkerDirectoryApplicationInstallation, t_jobOwner, appId, "bin");
           if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
           File.WriteAllText(Path.Combine(folder, "1.exe"), "MZsomeapp");
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            t_cts.Cancel();
        }

        [TestMethod]
        public void VENUSAppStoreTestMethod1()
        {
            var description = new VENUSApplicationDescription()
            {
                ApplicationIdentificationURI = "http://www.microsoft.com/emic/cloud/samples/e-ScienceApp#v1.0",
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
                    Executable = "bin/1.exe",
                    Path = "./bin;./lib",
                    WorkingDirectory = "./bin",
                    Args = new List<CommandLineArgument>()
                    {
                        new CommandLineArgument() 
                        {
                            Name = "INPUT",
                            FormatString = "-in \"{0}\"",
                            Required = Required.Mandatory, 
                            CommandLineArgType = CommandLineArgType.SingleLiteralArgument
                        },
                        new CommandLineArgument() 
                        {
                            Name = "Verbose",
                            FormatString = "--v",
                            CommandLineArgType = CommandLineArgType.Switch
                        }
                    }
                }
            };

            ArgumentCollection args = new ArgumentCollection() 
            { 
                new LiteralArgument { Name="INPUT", LiteralValue="k1"},
                new SwitchArgument{ Name="Verbose", Value=false}
            };

            string errorDescription = string.Empty;
            Assert.IsTrue(CommandLineBuilder.HasAllMandatoryArguments(args, description, out errorDescription), errorDescription);

            Assert.IsFalse(CommandLineBuilder.HasAllMandatoryArguments(
                new ArgumentCollection() 
                { 
                    new LiteralArgument { Name="INPUT", LiteralValue="k1"}
                },
                new VENUSApplicationDescription()
                {
                    CommandTemplate = new VENUSCommandTemplate()
                    {
                        Args = new List<CommandLineArgument>() 
                        {
                            new CommandLineArgument() 
                            {
                                Name = "Output",
                                Required = Required.Mandatory
                            }
                        }
                    }
                }, out errorDescription));

            Assert.IsFalse(CommandLineBuilder.HasAllMandatoryArguments(
                new ArgumentCollection() 
                { 
                    new LiteralArgument { Name="INPUT", LiteralValue="k1"},
                    new LiteralArgument { Name="Unrecognized", LiteralValue="k1"}
                },
                new VENUSApplicationDescription()
                {
                    CommandTemplate = new VENUSCommandTemplate()
                    {
                        Args = new List<CommandLineArgument>() 
                        {
                            new CommandLineArgument() 
                            {
                                Name = "INPUT",
                                Required = Required.Mandatory
                            }
                        }
                    }
                }, out errorDescription));
        }

        [TestMethod]
        public void BuildACommandLineWithSpecifiedLocalNameForInputReference()
        {
            var relFilePath = @"input\index2343.html";

            var account = CloudStorageAccount.Parse(CloudSettings.GenericWorkerCloudConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference("testcontainer");
            blobContainer.CreateIfNotExist();
            var blob = blobContainer.GetBlobReference("testblob");
            blob.UploadText("testcontent");
            var singleRef = new SingleReference { Name = "InputData", Ref = new Reference(relFilePath, new AzureBlobReference(blob, CloudSettings.GenericWorkerCloudConnectionString)) };


            var success = CommandLineBuilder.DownloadFilesAndBuildCommandLine(new TestJob("Jim", "jobid")
                    {
                        VENUSJobDescription = new VENUSJobDescription()
                        {
                            JobArgs =
                                new ArgumentCollection() 
                                    { 
                                        singleRef
                                    }
                        }
                    },
                    new VENUSApplicationDescription()
                    {
                        ApplicationIdentificationURI = "http://algo",
                        CommandTemplate = new VENUSCommandTemplate()
                        {
                            Executable = "bin/1.exe",
                            Path = "./bin;./lib",
                            WorkingDirectory = "./bin",
                            Args = new List<CommandLineArgument>()
                            {
                                new CommandLineArgument() 
                                {
                                    Name = "InputData",
                                    FormatString = "-in \"{0}\"",
                                    Required = Required.Mandatory, 
                                    CommandLineArgType = CommandLineArgType.SingleReferenceInputArgument
                                },
                            }
                        }
                    }, t_ProfileData, t_FilesystemMapper);
            Assert.IsTrue(success.Success);
        }

        [TestMethod]
        public void MissingMandatoryArgument()
        {
            var success = CommandLineBuilder.DownloadFilesAndBuildCommandLine(new TestJob("Jim", "jobid")
            {
                VENUSJobDescription = new VENUSJobDescription()
                {
                    JobArgs =
                        new ArgumentCollection() 
                            { 
                                new LiteralArgument { Name="INPUT", LiteralValue="input.txt"},
                                new LiteralArgument { Name="Output", LiteralValue="http://out"},
                                new SwitchArgument { Name="Verbose", Value=true},
                                new SwitchArgument { Name="loglevel", Value = true}
                            }
                }
            },
                    new VENUSApplicationDescription()
                    {
                        CommandTemplate = new VENUSCommandTemplate()
                        {
                            Executable = "bin/1.exe",
                            Path = "./bin;./lib",
                            WorkingDirectory = "./bin",
                            Args = new List<CommandLineArgument>()
                                    {
                                        new CommandLineArgument() 
                                        {
                                            Name = "INPUT",
                                            FormatString = "-in \"{0}\"",
                                            Required = Required.Mandatory, 
                                            CommandLineArgType = CommandLineArgType.SingleLiteralArgument
                                        },
                                        new CommandLineArgument() 
                                        {
                                            Name = "Output",
                                            FormatString = "-out '{0}'",
                                            Required = Required.Mandatory, 
                                            CommandLineArgType = CommandLineArgType.SingleLiteralArgument
                                        },
                                        new CommandLineArgument() 
                                        {
                                            Name = "Verbose",
                                            FormatString = "--v",
                                            CommandLineArgType = CommandLineArgType.Switch
                                        },
                                        new CommandLineArgument() 
                                        {
                                            Name = "loglevel",
                                            Required = Required.Optional,
                                            FormatString = "--l",
                                            CommandLineArgType = CommandLineArgType.Switch
                                        },
                                        new CommandLineArgument() 
                                        {
                                            Name = "mandatory",
                                            Required = Required.Mandatory,
                                            FormatString = "--x",
                                            CommandLineArgType = CommandLineArgType.Switch
                                        }
                                    }

                        }
                    }, t_ProfileData, t_FilesystemMapper);
            Assert.IsFalse(success.Success);
        }

        [TestMethod]
        public void UnknownArgument()
        {
            var success = CommandLineBuilder.DownloadFilesAndBuildCommandLine(new TestJob("Jim", "jobid")
            {
                VENUSJobDescription = new VENUSJobDescription()
                {
                    JobArgs =
                        new ArgumentCollection() 
                            { 
                                new LiteralArgument() { Name = "INPUT", LiteralValue = "input.txt" },
                                new LiteralArgument() { Name = "Output", LiteralValue = "http://out" },
                                new SwitchArgument() { Name = "Verbose", Value = true },
                                new SwitchArgument() { Name = "loglevel", Value = true } // What is a loglevel?
                            }
                }
            },
                    new VENUSApplicationDescription()
                    {
                        CommandTemplate = new VENUSCommandTemplate()
                        {
                            Executable = "bin/1.exe",
                            Path = "./bin;./lib",
                            WorkingDirectory = "./bin",
                            Args = new List<CommandLineArgument>()
                                    {
                                        new CommandLineArgument() 
                                        {
                                            Name = "INPUT",
                                            FormatString = "-in \"{0}\"",
                                            Required = Required.Mandatory, 
                                            CommandLineArgType = CommandLineArgType.SingleLiteralArgument
                                        },
                                        new CommandLineArgument() 
                                        {
                                            Name = "Output",
                                            FormatString = "-out '{0}'",
                                            Required = Required.Mandatory, 
                                            CommandLineArgType = CommandLineArgType.SingleLiteralArgument
                                        },
                                        new CommandLineArgument() 
                                        {
                                            Name = "Verbose",
                                            FormatString = "--v",
                                            CommandLineArgType = CommandLineArgType.Switch
                                        },
                                        new CommandLineArgument() 
                                        {
                                            Name = "mandatory",
                                            Required = Required.Mandatory,
                                            FormatString = "--x",
                                            CommandLineArgType = CommandLineArgType.Switch
                                        
                                        }
                    }
                        }
                    }, t_ProfileData, t_FilesystemMapper);

            Assert.IsFalse(success.Success);
        }

        [TestMethod]
        public void BuildACommandLineWithLiterals()
        {
            var success = CommandLineBuilder.DownloadFilesAndBuildCommandLine(new TestJob("Jim", "jobid")
                            {
                                VENUSJobDescription = new VENUSJobDescription()
                                {
                                    ApplicationIdentificationURI = "http://algo",
                                    JobArgs =
                                        new ArgumentCollection() 
                                            { 
                                                new LiteralArgument() { Name = "INPUT", LiteralValue = "input.txt" },
                                                new LiteralArgument() { Name = "Output", LiteralValue = "http://out" }
                                            }
                                }
                            },
                            new VENUSApplicationDescription()
                            {
                                ApplicationIdentificationURI = "http://algo",
                                CommandTemplate = new VENUSCommandTemplate()
                                {
                                    Executable = "bin/1.exe",
                                    Path = "./bin;./lib",
                                    WorkingDirectory = "./bin",
                                    Args = new List<CommandLineArgument>()
                                    {
                                        new CommandLineArgument() 
                                        {
                                            Name = "INPUT",
                                            FormatString = "-in \"{0}\"",
                                            Required = Required.Mandatory, 
                                            CommandLineArgType = CommandLineArgType.SingleLiteralArgument
                                        },
                                        new CommandLineArgument() 
                                        {
                                            Name = "Output",
                                            FormatString = "-out '{0}'",
                                            Required = Required.Optional, 
                                            CommandLineArgType = CommandLineArgType.SingleLiteralArgument
                                        }
                                    }
                                }
                            }, t_ProfileData, t_FilesystemMapper);

            Console.Out.WriteLine("Command line : {0} \"{1}\"", success.Executable, success.CommandLineArgs);
        }


        [TestMethod]
        public void BuildACommandLineUsingOptionalArgument()
        {
            var success = CommandLineBuilder.DownloadFilesAndBuildCommandLine(
                            new TestJob(t_jobOwner, "jobid")
                            {
                                VENUSJobDescription = new VENUSJobDescription()
                                {
                                    ApplicationIdentificationURI = "http://algo",
                                    JobArgs =
                                        new ArgumentCollection() 
                                            { 
                                                new LiteralArgument() { Name = "INPUT", LiteralValue = "input.txt" }
                                            }
                                }
                            },
                            new VENUSApplicationDescription()
                            {
                                ApplicationIdentificationURI = "http://algo",
                                CommandTemplate = new VENUSCommandTemplate()
                                {
                                    Executable = "bin/1.exe",
                                    Path = "./bin;./lib",
                                    WorkingDirectory = "./bin",
                                    Args = new List<CommandLineArgument>()
                                    {
                                        new CommandLineArgument() 
                                        {
                                            Name = "INPUT",
                                            FormatString = "-in \"{0}\"",
                                            Required = Required.Mandatory, 
                                            CommandLineArgType = CommandLineArgType.SingleLiteralArgument
                                        },
                                        new CommandLineArgument() 
                                        {
                                            Name = "Output",
                                            FormatString = "-out '{0}'",
                                            Required = Required.Optional, 
                                            CommandLineArgType = CommandLineArgType.SingleLiteralArgument
                                        }
                                    }
                                }
                            }, t_ProfileData, t_FilesystemMapper);
            Assert.IsTrue(success.Success, success.ErrorDescription);

            success = CommandLineBuilder.DownloadFilesAndBuildCommandLine(new TestJob(t_jobOwner, "jobid")
                            {
                                VENUSJobDescription = new VENUSJobDescription()
                                {
                                    ApplicationIdentificationURI = "http://algo",
                                    JobArgs =
                                        new ArgumentCollection() 
                                            { 
                                                new LiteralArgument() { Name = "INPUT", LiteralValue = "input.txt" },
                                                new SwitchArgument() { Name = "Verbose", Value = true }
                                            }
                                }
                            },
                            new VENUSApplicationDescription()
                            {
                                ApplicationIdentificationURI = "http://algo",
                                CommandTemplate = new VENUSCommandTemplate()
                                {
                                    Executable = "bin/1.exe",
                                    Path = "./bin;./lib",
                                    WorkingDirectory = "./bin",
                                    Args = new List<CommandLineArgument>()
                                    {
                                        new CommandLineArgument() 
                                        {
                                            Name = "INPUT",
                                            FormatString = "-in \"{0}\"",
                                            Required = Required.Mandatory, 
                                            CommandLineArgType = CommandLineArgType.SingleLiteralArgument
                                        },
                                        new CommandLineArgument() 
                                        {
                                            Name = "Verbose",
                                            FormatString = "-out Verbose",
                                            Required = Required.Optional, 
                                            CommandLineArgType = CommandLineArgType.Switch
                                        }
                                    }
                                }
                            }, t_ProfileData, t_FilesystemMapper);

            Assert.IsTrue(success.Success, success.ErrorDescription);
        }

        public void BuildACommandLineWithMissingReferenceDataSets()
        {
            var account = CloudStorageAccount.Parse(CloudSettings.GenericWorkerCloudConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference("teststore");
            blobContainer.CreateIfNotExist();
            BlobContainerPermissions resultPermissions = blobContainer.GetPermissions();
            resultPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
            blobContainer.SetPermissions(resultPermissions);

            Func<string, string, string> upload = ((filename, content) =>
            {
                var blob = blobContainer.GetBlobReference(filename);
                blob.UploadText(content);
                var blobAddress = blob.Uri.AbsoluteUri;
                return blobAddress;
            });

            var success = CommandLineBuilder.DownloadFilesAndBuildCommandLine(new TestJob()
                            {
                                VENUSJobDescription = new VENUSJobDescription()
                                {
                                    ApplicationIdentificationURI = "http://algo",
                                    JobArgs =
                                        new ArgumentCollection() 
                                            { 
                                                new AzureArgumentSingleReference() { Name = "INPUTDataSet", DataAddress = "https://venusstorage.blob.core.windows.net/test/16MB.bin", ConnectionString=CloudSettings.TestCloudConnectionString },
                                                new AzureArgumentSingleReference() { Name = "INPUTDataSet2", DataAddress = upload("2.bin", "The data in 2.txt") , ConnectionString=CloudSettings.TestCloudConnectionString },
                                                new AzureArgumentSingleReference() { Name = "INPUTDataSet3", DataAddress = "https://venusstorage.blob.core.windows.net/teststore/doesnotexist", ConnectionString=CloudSettings.TestCloudConnectionString}
                                            }
                                }
                            },
                            new VENUSApplicationDescription()
                            {
                                ApplicationIdentificationURI = "http://algo",
                                CommandTemplate = new VENUSCommandTemplate()
                                {
                                    Executable = "bin/1.exe",
                                    Path = "./bin;./lib",
                                    WorkingDirectory = "./bin",
                                    Args = new List<CommandLineArgument>()
                                    {
                                        new CommandLineArgument() 
                                        {
                                            Name = "INPUTDataSet",
                                            FormatString = "-in \"{0}\"",
                                            Required = Required.Mandatory, 
                                            CommandLineArgType = CommandLineArgType.SingleReferenceInputArgument
                                        },
                                        new CommandLineArgument() 
                                        {
                                            Name = "INPUTDataSet2",
                                            FormatString = "-in2 \"{0}\"",
                                            Required = Required.Mandatory, 
                                            CommandLineArgType = CommandLineArgType.SingleReferenceInputArgument
                                        },
                                        new CommandLineArgument() 
                                        {
                                            Name = "INPUTDataSet3",
                                            FormatString = "-in3 \"{0}\"",
                                            Required = Required.Mandatory, 
                                            CommandLineArgType = CommandLineArgType.SingleReferenceInputArgument
                                        }
                                    }
                                }
                            }, t_ProfileData, t_FilesystemMapper);

            Assert.IsFalse(success.Success, success.ErrorDescription);
        }
    }
}