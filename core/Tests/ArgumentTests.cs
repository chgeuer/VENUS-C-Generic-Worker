//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.ComponentModel.Composition.Hosting;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.EMIC.Cloud.Demo;
using Microsoft.EMIC.Cloud.DataManagement;
using Microsoft.EMIC.Cloud.Storage.Azure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EMIC.Cloud.GenericWorker;
using Microsoft.WindowsAzure;
using System.Threading;
using Microsoft.WindowsAzure.StorageClient;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EMIC.Cloud.ApplicationRepository;
using System;
using KTH.GenericWorker.CDMI;
using System.Net;
using System.Runtime.Serialization;
using OGF.BES;

namespace Tests
{
    [TestClass]
    public class ArgumentTests
    {
        static CompositionContainer m_compositionContainer;
        static ArgumentRepository m_ArgumentRepository;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            m_compositionContainer = new CompositionContainer(
                new AggregateCatalog(
                    new AssemblyCatalog(typeof(LiteralArgument).Assembly),
                    new AssemblyCatalog(typeof(AzureArgumentCatalogueReference).Assembly)
                    ));

            m_ArgumentRepository = m_compositionContainer.GetExportedValue<ArgumentRepository>();
        }

        [TestMethod]
        public void SerializeLiteralArgumentTest()
        {
            var doc = new XmlDocument();
            var a = new LiteralArgument() { Name = "PARAM1", LiteralValue = "3" };
            var e = a.Serialize(doc);

            var a2 = m_ArgumentRepository.Load(e) as LiteralArgument;
            Assert.IsNotNull(a2);
            Assert.AreEqual<string>(a.Name, a2.Name);
            Assert.AreEqual<string>(a.LiteralValue, a2.LiteralValue);
        }

        [TestMethod]
        public void SerializeSwitchArgumentTest()
        {
            var doc = new XmlDocument();
            var a = new SwitchArgument() { Name = "VERBOSE", Value = true };
            var e = a.Serialize(doc);

            var a2 = m_ArgumentRepository.Load(e) as SwitchArgument;
            Assert.IsNotNull(a2);
            Assert.AreEqual<string>(a.Name, a2.Name);
            Assert.AreEqual<bool>(a.Value, a2.Value);
        }

        [TestMethod]
        public void SerializeAzureArgumentSingleReferenceTest()
        {
            var doc = new XmlDocument();
            var a = new AzureArgumentSingleReference
            {
                Name = "AZUREBlob",
                ConnectionString = CloudSettings.TestCloudConnectionString,
                DataAddress = "https://venusstorage.blob.core.windows.net/teststore/1.bin"
            };
            var e = a.Serialize(doc);

            var a2 = m_ArgumentRepository.Load(e) as SingleReference;
            Assert.IsNotNull(a2);
            var nestedAzureBlobRef = a2.Ref.ProviderSpecificReference as AzureBlobReference;
            Assert.IsNotNull(a2);
            Assert.AreEqual<string>(a.Name, a2.Name);
            Assert.AreEqual<string>(a.ConnectionString, nestedAzureBlobRef.ConnectionString);
            Assert.AreEqual<string>(a.DataAddress, nestedAzureBlobRef.DataAddress);
        }

        [TestMethod]
        public void SerializeLiteralArgumentWithValueContainingQuotes() //Issue 90
        {
            var doc = new XmlDocument();
            //var a = new LiteralArgument() { Name = "customArgs", LiteralValue = "/x:&quot;Delta=-1&quot; /x:&quot;Time=-1&quot; /x:&quot;Steps=100&quot; /x:&quot;Runs=1&quot; /x:&quot;Aggregation=None&quot;" };
            var a = new LiteralArgument() { Name = "customArgs", LiteralValue = "/x:\"Delta=-1\" /x:\"Time=-1\"" };
            var e = a.Serialize(doc);

            var a2 = m_ArgumentRepository.Load(e) as LiteralArgument;
            Assert.IsNotNull(a2);
            Assert.AreEqual<string>(a.Name, a2.Name);
            Assert.AreEqual<string>(a.LiteralValue, a2.LiteralValue);

            var job1 = new VENUSJobDescription() { JobArgs = new ArgumentCollection() { a } };
            var xs = new XmlSerializer(typeof(CreateActivityRequest));
            var ms = new MemoryStream();
            xs.Serialize(ms, job1.GetCreateActivity());
            ms.Seek(0L, SeekOrigin.Begin);
            var ca = (CreateActivityRequest)xs.Deserialize(ms);
            ms.Close();
            var job2 = new VENUSJobDescription(ca, m_ArgumentRepository);
            var firstArg = (LiteralArgument)job2.JobArgs[0];
            Assert.AreEqual<string>(a.Name, firstArg.Name);
            Assert.AreEqual<string>(a.LiteralValue, firstArg.LiteralValue);
        }

        [TestMethod]
        public void DownloadsRefTest()
        {
            var a = new AzureBlobReference
            {
                ConnectionString = CloudSettings.TestCloudConnectionString,
                DataAddress = "https://venusstorage.blob.core.windows.net/teststore/1.bin"
            };

            var jobdesc = new VENUSJobDescription();
            var testRef = new Reference("test", a);
            jobdesc.DownloadsReference = testRef;
            var activity = jobdesc.GetCreateActivity();
            var anyElem = activity.CreateActivity.ActivityDocument.JobDefinition.JobDescription.Any;
            Func<XmlElement, bool> filter = e => e.LocalName.Equals("DownloadsRef") ;
            var dlRefsEl = anyElem.Where(filter).FirstOrDefault();

            Assert.IsNotNull(dlRefsEl);
        }

        [TestMethod]
        public void DownloadsRefTest2()
        {
            var a = new AzureBlobReference
            {
                ConnectionString = CloudSettings.TestCloudConnectionString,
                DataAddress = "https://venusstorage.blob.core.windows.net/teststore/1.bin"
            };

            var connStr = CloudSettings.GenericWorkerCloudConnectionString;

            var account = CloudStorageAccount.Parse(connStr);
            var blobClient = account.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference("testcontainer");
            blobContainer.CreateIfNotExist();
            
            // method for constructing the url of the file in the server
            Func<string, string> computeName = ((name) =>
            {
                var result = blobContainer.GetBlobReference(name).Uri.AbsoluteUri;

                return result;
            });

            var numFragments = 2;
            var downloadsRefCol = new ReferenceCollection();
            for (int i = 0; i < numFragments; i++)
            {
                var resultfileName = string.Format("result{0}.txt", i);
                downloadsRefCol.Add(new Reference(resultfileName, new AzureBlobReference(computeName(resultfileName), connStr)));
            }

            Func<ReferenceCollection, bool, CloudBlob> uploadReferenceCollection = ((referenceCollection, isUploadsCollection) =>
            {
                var postfix = (isUploadsCollection) ? "UploadsCollection" : "DownloadsCollection";
                var blobName = Guid.NewGuid().ToString() + postfix;

                var ra = new ReferenceArray() { Name = postfix, References = referenceCollection };

                var xmlDoc = new XmlDocument();
                var serializedRefArr = ra.Serialize(xmlDoc);
                xmlDoc.AppendChild(serializedRefArr);

                CloudBlob xmlBlob;

                xmlBlob = blobContainer.GetBlobReference(blobName);
                xmlBlob.Properties.ContentType = "text/xml";
                xmlBlob.UploadText(xmlDoc.InnerXml);

                return xmlBlob;
            });

            var jobdesc = new VENUSJobDescription();
            var testRef = new Reference("test", a);
            jobdesc.AppPkgReference = testRef;
            jobdesc.AppDescReference = testRef;
            jobdesc.DownloadsReference = new Reference("downRefs",new AzureBlobReference(uploadReferenceCollection(downloadsRefCol,false),CloudSettings.GenericWorkerCloudConnectionString));
            jobdesc.UploadsReference = new Reference("upRefs", new AzureBlobReference(uploadReferenceCollection(downloadsRefCol, true), CloudSettings.GenericWorkerCloudConnectionString));
            var activity = jobdesc.GetCreateActivity();
            var anyElem = activity.CreateActivity.ActivityDocument.JobDefinition.JobDescription.Any;
            Func<XmlElement, bool> filter = e => e.LocalName.Equals("DownloadsRef");
            var dlRefsEl = anyElem.Where(filter).FirstOrDefault();

            Assert.IsNotNull(dlRefsEl);
            var jobDesc2 = new VENUSJobDescription(activity, m_ArgumentRepository);
            Assert.AreEqual(2, jobDesc2.Downloads.Count());
            Assert.AreEqual(2, jobDesc2.Uploads.Count());

            var activity2 = jobDesc2.GetCreateActivity();
        }

        public class TestReferenceArgument : IReferenceArgument
        {
            private bool m_existsDataItem;
            public TestReferenceArgument(bool exists, string name)
            {
                this.m_existsDataItem = exists;
                this.Name = name;
            }

            #region IReferenceArgument Members
            public string Name { get; set; }
            public bool ExistsDataItem()
            {
                return m_existsDataItem;
            }

            public string CloudProvider { get { return "test"; } }

            public void Download(string workingDirectory, CancellationTokenSource tokenSource)
            {
                throw new System.NotImplementedException();
            }

            public void Upload(string workingDirectory)
            {
                throw new System.NotImplementedException();
            }


            public string GetFileLocation(string workingDirectory)
            {
                throw new System.NotImplementedException();
            }

            #endregion

            #region IArgument Members

            public void LoadContents(XmlElement xmlElement, ArgumentRepository argumentRepository)
            {
                throw new System.NotImplementedException();
            }

            public XmlElement Serialize(XmlDocument doc)
            {
                throw new System.NotImplementedException();
            }

            #endregion
        }

        [TestMethod]
        public void AzureBlobModificationTime_Test()
        {
            var account = CloudStorageAccount.Parse(CloudSettings.GenericWorkerCloudConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference("testcontainer");
            blobContainer.CreateIfNotExist();
            var blob = blobContainer.GetBlobReference("testblob");
            blob.UploadText("testcontent");
            var blobRef = new AzureBlobReference(blob, CloudSettings.GenericWorkerCloudConnectionString);
            var timeUpdate1 = blobRef.GetLastModificationDateTime();
            Thread.Sleep(TimeSpan.FromSeconds(5));
            blob.UploadText("testcontent2");
            var timeUpdate2 = blobRef.GetLastModificationDateTime();
            Assert.IsTrue(timeUpdate2 > timeUpdate1);
            Thread.Sleep(TimeSpan.FromSeconds(5));
            var timeUpdate3 = blobRef.GetLastModificationDateTime();
            Assert.IsTrue(timeUpdate2.Ticks == timeUpdate3.Ticks);
        }

        [TestMethod]
        public void CDMIBlobModificationTime_Test()
        {
            bool useSecureBinding = false;
            var cdmiAddress = useSecureBinding ? "https://emicloudbuild:8080" : "http://emicloudbuild:2365";
            var username = "user";
            var password = "cdmipass";

            var fileName = "testFile";
            var inputUri = string.Format("{0}/{1}", cdmiAddress,fileName);
            var cdmiRef = new CDMIBlobReference()
            {
                URI = inputUri,
                Credentials = new NetworkCredential(username, password),
                RequestFactory = url =>
                {
                    var request = (HttpWebRequest)WebRequest.Create(url);
                    return request;
                }
            };

            using (var testFile = File.CreateText(fileName))
            {
                testFile.Write("bla bla");
            }
            var inputReference = new Reference(fileName, cdmiRef);
            inputReference.Upload(".");

            var timeUpdate1 = cdmiRef.GetLastModificationDateTime();
            Thread.Sleep(TimeSpan.FromSeconds(5));
            inputReference.Upload(".");
            var timeUpdate2 = cdmiRef.GetLastModificationDateTime();
            Assert.IsTrue(timeUpdate2 > timeUpdate1);
            Thread.Sleep(TimeSpan.FromSeconds(5));
            var timeUpdate3 = cdmiRef.GetLastModificationDateTime();
            Assert.IsTrue(timeUpdate2.Ticks == timeUpdate3.Ticks);
            File.Delete(fileName);
        }

        [TestMethod]
        public void IReferenceArgument_DataItemExists_Test()
        {
            var account = CloudStorageAccount.Parse(CloudSettings.GenericWorkerCloudConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference("testcontainer");
            blobContainer.CreateIfNotExist();
            var blob = blobContainer.GetBlobReference("testblob");
            blob.UploadText("testcontent");
            AzureArgumentSingleReference azRef = new AzureArgumentSingleReference
            {
                Name = "InputFile",
                DataAddress = blob.Uri.AbsoluteUri,
                ConnectionString = CloudSettings.UserDataStoreConnectionString
            };
            XmlDocument doc = new XmlDocument();
            var e = azRef.Serialize(doc); //old apps with AzureArgumentSingleReference args have to be reinstalled
            var refArg = m_ArgumentRepository.Load(e) as SingleReference;
            Assert.IsTrue(refArg.ExistsDataItem());
            blob.DeleteIfExists();
            Assert.IsFalse(refArg.ExistsDataItem());
        }

        [TestMethod]
        public void RefArray_DataItemExists_Test()
        {
            var account = CloudStorageAccount.Parse(CloudSettings.GenericWorkerCloudConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference("testcontainer");
            blobContainer.CreateIfNotExist();
            ReferenceArray refArr = new ReferenceArray
            {
                Name = "inputfiles",
                References = new ReferenceCollection()
            };
            
            for (int i=0;i<3;i++)
            {
                var name = string.Format("inp{0}.txt",i);
                var blob = blobContainer.GetBlobReference(name);
                blob.UploadText("testContent"+i);
                var azBlobRef = new AzureBlobReference(blob,CloudSettings.GenericWorkerCloudConnectionString);
                refArr.References.Add(new Reference(name,azBlobRef));
            }
                            
            XmlDocument doc = new XmlDocument();
            var e = refArr.Serialize(doc); //old apps with AzureArgumentSingleReference args have to be reinstalled
            var refArg = m_ArgumentRepository.Load(e) as ReferenceArray;
            Assert.IsTrue(refArg.ExistsDataItem());
            for (int i = 0; i < 3; i++)
            {
                var name = string.Format("inp{0}.txt", i);
                var blob = blobContainer.GetBlobReference(name);
                blob.DeleteIfExists();
            }
            Assert.IsFalse(refArg.ExistsDataItem());
        }

        [TestMethod]
        public void RefArray_DownloadUpload_Test()
        {
            var account = CloudStorageAccount.Parse(CloudSettings.GenericWorkerCloudConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference("testcontainer");
            blobContainer.CreateIfNotExist();
            ReferenceArray refArr = new ReferenceArray
            {
                Name = "inputfiles",
                References = new ReferenceCollection()
            };

            for (int i = 0; i < 3; i++)
            {
                var name = string.Format("inp{0}.txt", i);
                var blob = blobContainer.GetBlobReference(name);
                blob.UploadText("testContent" + i);
                var azBlobRef = new AzureBlobReference(blob, CloudSettings.GenericWorkerCloudConnectionString);
                refArr.References.Add(new Reference(name, azBlobRef));
            }

            XmlDocument doc = new XmlDocument();
            var e = refArr.Serialize(doc); //old apps with AzureArgumentSingleReference args have to be reinstalled
            var refArg = m_ArgumentRepository.Load(e) as ReferenceArray;
            Assert.IsTrue(refArg.ExistsDataItem());
            var workingDir = Path.Combine(@"C:\temp",Guid.NewGuid().ToString());
            Directory.CreateDirectory(workingDir);
            CancellationTokenSource cts = new CancellationTokenSource();
            refArg.Download(workingDir, cts);
            Action DeleteFromBlobStore = () => {
                for (int i = 0; i < 3; i++)
                {
                    var name = string.Format("inp{0}.txt", i);
                    var blob = blobContainer.GetBlobReference(name);
                    blob.DeleteIfExists();                
                }
            };
            DeleteFromBlobStore();
            Assert.IsFalse(refArg.ExistsDataItem());
            for (int i = 0; i < 3; i++)
            {
                var name = string.Format("inp{0}.txt", i);
                Assert.IsTrue(File.Exists(Path.Combine(workingDir, name)), "File was not downloaded to the specified location");
            }
            refArg.Upload(workingDir);
            Assert.IsTrue(refArg.ExistsDataItem(), "Files were not uploaded properly");
            DeleteFromBlobStore();
            Directory.Delete(workingDir, true);
        }

        [TestMethod]
        public void CatalogueReferenceArgument_DataItemExists_Test()
        {
            var account = CloudStorageAccount.Parse(CloudSettings.GenericWorkerCloudConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference("testcontainer");
            blobContainer.CreateIfNotExist();
            var blob = blobContainer.GetBlobReference("testblob");
            blob.UploadText("testcontent");
            AzureArgumentSingleReference azRef = new AzureArgumentSingleReference
            {
                Name = "InputFile",
                DataAddress = blob.Uri.AbsoluteUri,
                ConnectionString = CloudSettings.UserDataStoreConnectionString
            };
            AzureCatalogueHandler catHandler = new AzureCatalogueHandler(CloudSettings.GenericWorkerCloudConnectionString);
            string logicalName ="testcatalogueref";
            catHandler.Add(logicalName, azRef);
            var catRef = catHandler.Get(logicalName);
            Assert.IsTrue(catRef.ExistsDataItem());
            blob.DeleteIfExists();
            Assert.IsFalse(catRef.ExistsDataItem());
        }

        [TestMethod]
        public void IReferenceArgument_DataItemExists_IJobExtensions_Ready_Test()
        {
            VENUSApplicationDescription applicationDescription = new VENUSApplicationDescription()
            {
                CommandTemplate = new VENUSCommandTemplate()
                {
                    Args = new List<CommandLineArgument>() 
                    {
                        new CommandLineArgument(){
                            Name = "InputFile",
                            FormatString = "{0}",
                            Required = Required.Mandatory,
                            CommandLineArgType = CommandLineArgType.SingleReferenceInputArgument
                        },
                        new CommandLineArgument(){
                            Name = "AnotherInputFile",
                            FormatString = "{0}",
                            Required = Required.Mandatory,
                            CommandLineArgType = CommandLineArgType.SingleReferenceInputArgument
                        },
                        new CommandLineArgument(){
                            Name = "OutputFile",
                            FormatString = "{0}",
                            Required = Required.Mandatory,
                            CommandLineArgType = CommandLineArgType.SingleReferenceOutputArgument
                        },
                        new CommandLineArgument(){
                            Name = "AnotherOutputFile",
                            FormatString = "{0}",
                            Required = Required.Mandatory,
                            CommandLineArgType = CommandLineArgType.SingleReferenceOutputArgument
                        }
                    }
                }
            };

            var account = CloudStorageAccount.Parse(CloudSettings.GenericWorkerCloudConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference("testcontainer");
            blobContainer.CreateIfNotExist();
            var blob = blobContainer.GetBlobReference("testblob");
            blob.UploadText("testcontent");
            AzureArgumentSingleReference azRef = new AzureArgumentSingleReference
            {
                Name = "InputFile",
                DataAddress = blob.Uri.AbsoluteUri,
                ConnectionString = CloudSettings.UserDataStoreConnectionString
            };
            XmlDocument doc=new XmlDocument();
            var e = azRef.Serialize(doc); //old apps with AzureArgumentSingleReference args have to be reinstalled
            var refArg = m_ArgumentRepository.Load(e) as SingleReference;

            IJob job = new TestJob("owner", "internalid")
            {
                Status = JobStatus.Submitted,
                VENUSJobDescription = new VENUSJobDescription
                {
                    JobArgs = new ArgumentCollection() 
                       { 
                           refArg,
                           new TestReferenceArgument(true, "AnotherInputFile"),
                           new TestReferenceArgument(true, "OutputFile"),
                           new TestReferenceArgument(false, "AnotherOutputFile")
                       }
                }
            };
            Assert.IsTrue(job.Ready(applicationDescription));
            blob.DeleteIfExists();
            Assert.IsFalse(job.Ready(applicationDescription));
        }

        [TestMethod]
        public void IJobExtensions_Ready_Test()
        {
            VENUSApplicationDescription applicationDescription = new VENUSApplicationDescription()
            {
                CommandTemplate = new VENUSCommandTemplate()
                {
                    Args = new List<CommandLineArgument>() 
                    {
                        new CommandLineArgument(){
                            Name = "InputFile",
                            FormatString = "{0}",
                            Required = Required.Mandatory,
                            CommandLineArgType = CommandLineArgType.SingleReferenceInputArgument
                        },
                        new CommandLineArgument(){
                            Name = "AnotherInputFile",
                            FormatString = "{0}",
                            Required = Required.Mandatory,
                            CommandLineArgType = CommandLineArgType.SingleReferenceInputArgument
                        },
                        new CommandLineArgument(){
                            Name = "OutputFile",
                            FormatString = "{0}",
                            Required = Required.Mandatory,
                            CommandLineArgType = CommandLineArgType.SingleReferenceOutputArgument
                        },
                        new CommandLineArgument(){
                            Name = "AnotherOutputFile",
                            FormatString = "{0}",
                            Required = Required.Mandatory,
                            CommandLineArgType = CommandLineArgType.SingleReferenceOutputArgument
                        }
                    }
                }
            };

            IJob complete = new TestJob("owner", "internalid")
             {
                 Status = JobStatus.Submitted,
                 VENUSJobDescription = new VENUSJobDescription
                 {
                     JobArgs = new ArgumentCollection() 
                       { 
                           new TestReferenceArgument(true, "InputFile"),
                           new TestReferenceArgument(true, "AnotherInputFile"),
                           new TestReferenceArgument(true, "OutputFile"),
                           new TestReferenceArgument(false, "AnotherOutputFile")
                       }
                 }
             };

            IJob incomplete = new TestJob("owner", "internalid")
            {
                Status = JobStatus.Submitted,
                VENUSJobDescription = new VENUSJobDescription
                {
                    JobArgs = new ArgumentCollection() 
                       { 
                           new TestReferenceArgument(true, "InputFile"),
                           new TestReferenceArgument(false, "AnotherInputFile"), // <-- This blocks
                           new TestReferenceArgument(true, "OutputFile"),
                           new TestReferenceArgument(false, "AnotherOutputFile")
                       }
                }
            };

            Assert.IsTrue(complete.Ready(applicationDescription));
            Assert.IsTrue(!incomplete.Ready(applicationDescription));
        }
        
        [TestMethod]
        public void ArgRepFtpReferenceTest()
        {
            var doc = new XmlDocument();
            var ftpRef = new FtpReference
            {
                DataAddress = "ftp://somehost/somefile",
                UserName = "user",
                Password = "pass"
            };
            var e = ftpRef.Serialize(doc);
            var ftpRef2 = new FtpReference();
            ftpRef2.LoadContents(e);
            Assert.AreEqual<string>(ftpRef.DataAddress, ftpRef2.DataAddress);
            Assert.AreEqual<string>(ftpRef.UserName, ftpRef2.UserName);
            Assert.AreEqual<string>(ftpRef.Password, ftpRef2.Password);
            var singleRef = new SingleReference
            {
                Name = "ftpTest",
                Ref = new Reference
                {
                    ProviderSpecificReference = ftpRef
                }
            };
            e = singleRef.Serialize(doc);
            var a2 = m_ArgumentRepository.Load(e) as SingleReference;
            Assert.IsNotNull(a2);
            var nestedFtpRef = a2.Ref.ProviderSpecificReference as FtpReference;
            Assert.AreEqual<string>(ftpRef.DataAddress, nestedFtpRef.DataAddress);
            Assert.AreEqual<string>(ftpRef.UserName, nestedFtpRef.UserName);
            Assert.AreEqual<string>(ftpRef.Password, nestedFtpRef.Password);

            singleRef.Ref.ProviderSpecificReference = new FtpReference(ftpRef.DataAddress); //testinf FtpReference without credentials
            e = singleRef.Serialize(doc);
            a2 = m_ArgumentRepository.Load(e) as SingleReference;
            Assert.IsNotNull(a2);
            nestedFtpRef = a2.Ref.ProviderSpecificReference as FtpReference;
            Assert.AreEqual<string>(ftpRef.DataAddress, nestedFtpRef.DataAddress);
            Assert.IsNull(nestedFtpRef.UserName);
            Assert.IsNull(nestedFtpRef.Password);
        }


        [TestMethod]
        public void JobDescDeSerializationTest()
        {
            var jobDesc = new VENUSJobDescription
            {
                AppPkgReference = new Reference("",new HttpGetReference("www.microsoft.com")),
                AppDescReference = new Reference("", new HttpGetReference("www.microsoft.com")),
                JobArgs = new ArgumentCollection() 
                { 
                    new SingleReference{Name="arg1", Ref=new Reference("",new HttpGetReference("www.microsoft.com"))},
                    new SingleReference{Name="arg2", Ref=new Reference("",new HttpGetReference("www.msdn.com"))},
                }
            };
            var stream = jobDesc.AsMemoryStream();
            var xmlRep = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            var jobDesc2 = VENUSJobDescription.FromStream(stream, m_ArgumentRepository);
            Assert.IsTrue(jobDesc.JobArgs.Count==jobDesc2.JobArgs.Count,"VENUSJobDescription (de)serialization is not working");
        }

        [TestMethod]
        public void MissingOutputFilePolicyDeSerializationTest()
        {
            var jobDesc = new VENUSJobDescription
            {
                AppPkgReference = new Reference("", new HttpGetReference("www.microsoft.com")),
                AppDescReference = new Reference("", new HttpGetReference("www.microsoft.com")),
                MissingResultFilePolicy = MissingResultFilePolicy.GenerateZeroFiles,
                JobArgs = new ArgumentCollection() 
                { 
                    new SingleReference{Name="arg1", Ref=new Reference("",new HttpGetReference("www.microsoft.com"))},
                    new SingleReference{Name="arg2", Ref=new Reference("",new HttpGetReference("www.msdn.com"))},
                }
            };
            var stream = jobDesc.AsMemoryStream();
            var xmlRep = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            var jobDesc2 = VENUSJobDescription.FromStream(stream, m_ArgumentRepository);
            Assert.IsTrue(jobDesc.JobArgs.Count == jobDesc2.JobArgs.Count, "MissingOutputFilePolicy (de)serialization is not working");
        }

        [TestMethod]
        public void MissingOutputFilePolicyDefaultValueDeSerializationTest()
        {
            var jobDesc = new VENUSJobDescription
            {
                AppPkgReference = new Reference("", new HttpGetReference("www.microsoft.com")),
                AppDescReference = new Reference("", new HttpGetReference("www.microsoft.com")),
                JobArgs = new ArgumentCollection() 
                { 
                    new SingleReference{Name="arg1", Ref=new Reference("",new HttpGetReference("www.microsoft.com"))},
                    new SingleReference{Name="arg2", Ref=new Reference("",new HttpGetReference("www.msdn.com"))},
                }
            };
            var stream = jobDesc.AsMemoryStream();
            var xmlRep = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            var jobDesc2 = VENUSJobDescription.FromStream(stream, m_ArgumentRepository);
            Assert.IsTrue(jobDesc.JobArgs.Count == jobDesc2.JobArgs.Count, "MissingOutputFilePolicy (de)serialization is not working");
            Assert.AreEqual(jobDesc.MissingResultFilePolicy, MissingResultFilePolicy.Standard);
        }

        [TestMethod()]
        public void ExistsTest()
        {
            var logicalName = "Test";

            var c = new CompositionContainer(new AggregateCatalog(
                new AssemblyCatalog(typeof(BESFactoryPortTypeImplService).Assembly),
                new AssemblyCatalog(typeof(AzureCatalogueHandler).Assembly)));
            var argRepo = c.GetExportedValue<ArgumentRepository>();

            var azureHandler = new AzureCatalogueHandler(CloudSettings.UserDataStoreConnectionString, argRepo);
            //I could also put "manually" a new logicalName in the table            
            azureHandler.Add(logicalName, new AzureArgumentSingleReference
            {
                Name = "TestName",
                DataAddress = "someUri",
                ConnectionString = CloudSettings.UserDataStoreConnectionString
            });
            Assert.IsTrue(azureHandler.Exists(logicalName));

            logicalName = "this logical name does not exist";
            Assert.IsFalse(azureHandler.Exists(logicalName));
        }

        [TestMethod()]
        public void ExistsAndDownloadFtpReferenceTest()
        {
            var ftpRef = new FtpReference
            {
                DataAddress = "ftp://localhost/File0",
                UserName = "Ftpuser",
                Password = "Q123456!"
            };
            Assert.IsTrue(ftpRef.ExistsDataItem(), "FtpReference ExistsDataItem provides false negatives");
            var content = ftpRef.DownloadContents();
            Assert.IsTrue(content.Length > 0, "FtpReference Download was not successful");
            ftpRef.DataAddress = "ftp://localhost/doesnotexist";
            Assert.IsFalse(ftpRef.ExistsDataItem(), "FtpReference ExistsDataItem provides false positives");
            var dir = Directory.CreateDirectory(@"c:\temp\uploadtest");
            File.WriteAllText(Path.Combine(dir.FullName,"doesnotexist"),"testststststststststsst");
            ftpRef.Upload(dir.FullName);
            Assert.IsTrue(ftpRef.ExistsDataItem(), "upload did not work");
            Assert.IsTrue(ftpRef.Delete(), "file could not be deleted from the ftp site");
        }

        [TestMethod()]
        public void GetLastModifiedFtpReferenceTest()
        {
            var ftpRef = new FtpReference
            {
                DataAddress = "ftp://localhost/File0",
                UserName = "Ftpuser",
                Password = "Q123456!"
            };
            Assert.IsTrue(ftpRef.ExistsDataItem(), "FtpReference ExistsDataItem provides false negatives");
            var oldFileLastModified = ftpRef.GetLastModificationDateTime();
            ftpRef.DataAddress = "ftp://localhost/doesnotexist";
            var dir = Directory.CreateDirectory(@"c:\temp\uploadtest");
            File.WriteAllText(Path.Combine(dir.FullName, "doesnotexist"), "testststststststststsst");
            ftpRef.Upload(dir.FullName);
            var newFileLastModified = ftpRef.GetLastModificationDateTime();
            Assert.IsTrue(newFileLastModified.Ticks > oldFileLastModified.Ticks,"GetLastModificationDateTime is not working for FTP references");
            Assert.IsTrue(ftpRef.ExistsDataItem(), "upload did not work");
            Assert.IsTrue(ftpRef.Delete(), "file could not be deleted from the ftp site");
        }

        [TestMethod]
        public void ArgRepoHttpGetReferenceTest()
        {
            var doc = new XmlDocument();
            var a = new HttpGetReference
            {
                DataAddress = "http://www.microsoft.com/index.html"
            };
            var refEl = new Reference
            {
                ProviderSpecificReference = a
            };
            var singleRef = new SingleReference
            {
                Name="SingleRef",
                Ref=refEl
            };
            var e = singleRef.Serialize(doc);
            var a2 = m_ArgumentRepository.Load(e) as SingleReference;
            Assert.IsNotNull(a2);
            var nestedHttpRef = a2.Ref.ProviderSpecificReference as HttpGetReference;
            Assert.AreEqual<string>(a.DataAddress, nestedHttpRef.DataAddress);
        }

        [TestMethod] //TODO: move LocalFileName to  ProviderSpecificReferences only or have a constructor for Reference which sets the localfilename of the providerspecRef automatically
        public void HttpGetLocalFileTest()
        {
            var pathWorkDir=@"C:\temp";
            var localDir="output";

            var relFilePath=Path.Combine(localDir,"index.html");
            var fullPath = Path.Combine(pathWorkDir, relFilePath);

            Directory.CreateDirectory(Path.Combine(pathWorkDir,localDir));
            using (var file = File.CreateText(fullPath))
            {
                file.WriteLine("test");
            }

            var doc = new XmlDocument();
            var a = new HttpGetReference
            {
                DataAddress = "http://www.gutenberg.org/",
            };
            var refEl = new Reference(relFilePath,a);           //TODO: standard contr -> private, no public properties 
            var singleRef = new SingleReference
            {
                Name = "SingleRef",
                Ref = refEl
            };
            var fileLocSingleRef = singleRef.GetFileLocation(pathWorkDir);
            Assert.AreEqual<string>(fileLocSingleRef, fullPath, "localfilename has unexpected value");
            var fileLocRef = singleRef.Ref.GetFileLocation(pathWorkDir);
            Assert.AreEqual<string>(fileLocSingleRef, fileLocRef, "the localfilenames of the singleref and the ref are not matching");
            var fileLoc = singleRef.Ref.ProviderSpecificReference.GetFileLocation(pathWorkDir);            
            Assert.AreEqual<string>(fileLocSingleRef, fullPath, "the localfilenames of the the ref and the providerspecific(httpget)reference are not matching");
            CancellationTokenSource cts = new CancellationTokenSource();
            singleRef.Download(pathWorkDir, cts);
            Assert.IsTrue(File.Exists(fullPath), "File is not saved in " + fullPath); //TODO: Make sure file is saved in (loaded from) Path.Combine(path,filename) not just filename
            Directory.Delete(@"C:\temp\output", true);
        }

        [TestMethod]
        public void AzureBlobRefLocalFileTest()
        {
            var pathWorkDir=@"C:\temp";
            var relFilePath=@"output\index2343.html";
            var fullPath = Path.Combine(pathWorkDir, relFilePath);

            var account = CloudStorageAccount.Parse(CloudSettings.GenericWorkerCloudConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference("testcontainer");
            blobContainer.CreateIfNotExist();
            var blob = blobContainer.GetBlobReference("testblob");
            blob.UploadText("testcontent");
            var singleRef = new SingleReference{ Name="SingleRef", Ref=new Reference(relFilePath,new AzureBlobReference(blob,CloudSettings.GenericWorkerCloudConnectionString)) };
            Assert.AreEqual<string>(singleRef.GetFileLocation(pathWorkDir),fullPath,"AzureBlobReference is not returning correct localfilepath");
            singleRef.Download(pathWorkDir,new CancellationTokenSource());
            Assert.IsTrue(File.Exists(fullPath),"File was not stored at the given location");
            blob.DeleteIfExists();
            singleRef.Upload(pathWorkDir);
            Directory.Delete(@"C:\temp\output",true);
        }

        [TestMethod]
        public void AzureBlobRefGetLastModDateTime()
        {
            var account = CloudStorageAccount.Parse(CloudSettings.GenericWorkerCloudConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference("testcontainer");
            blobContainer.CreateIfNotExist();
            var blob = blobContainer.GetBlobReference("testblob");
            blob.UploadText("testcontent");
            AzureBlobReference azBRef = new AzureBlobReference(blob, CloudSettings.GenericWorkerCloudConnectionString);
            azBRef.ExistsDataItem();
            var lastmod1 = azBRef.GetLastModificationDateTime();
            Thread.Sleep(TimeSpan.FromSeconds(1));
            blob.UploadText("newertestcontent");
            var lastmod2 = azBRef.GetLastModificationDateTime();
            Assert.IsTrue(lastmod2.Ticks > lastmod1.Ticks);

            var blob2 = blobContainer.GetBlobReference("blob2");
            blob2.UploadText("abcd");
            AzureBlobReference azBRef2 = new AzureBlobReference(blob2, CloudSettings.GenericWorkerCloudConnectionString);
            blob2.Delete();
            while (azBRef2.ExistsDataItem())
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
            try
            {
                azBRef2.GetLastModificationDateTime();
            }
            catch //make sure that the method throws an exception if the blob does not exist
            {
                return;
            }
            Assert.IsTrue(false);
        }


        [TestMethod]
        public void CDMIBlobRefDeleteTest()
        {
            bool useSecureBinding = false;
            var cdmiAddress = useSecureBinding ? "https://emicloudbuild:8080" : "http://emicloudbuild:2365";
            var username = "user";
            var password = "cdmipass";

            var fileName = "testFile";
            var inputUri = string.Format("{0}/{1}", cdmiAddress, fileName);
            var cdmiRef = new CDMIBlobReference()
            {
                URI = inputUri,
                FileLocation = fileName,
                Credentials = new NetworkCredential(username, password),
                RequestFactory = url =>
                {
                    var request = (HttpWebRequest)WebRequest.Create(url);
                    return request;
                }
            };

            using (var testFile = File.CreateText(fileName))
            {
                testFile.Write("bla bla");
            }
            cdmiRef.Upload(".");
            Assert.IsTrue(cdmiRef.ExistsDataItem());

            cdmiRef.Delete();
            Assert.IsFalse(cdmiRef.ExistsDataItem());
        }
        
        [TestMethod] //TODO: Create the Filesystem structure for the index.html file
        public void CDMIBlobRefLocalFileTest()
        {
            var pathWorkDir = @"C:\temp";
            var localDir = "output";

            var relFilePath = Path.Combine(localDir, "index.html");
            var fullPath = Path.Combine(pathWorkDir, relFilePath);

            Directory.CreateDirectory(Path.Combine(pathWorkDir, localDir));
            using (var file = File.CreateText(fullPath)) 
            {
                file.WriteLine("test");
            }

            bool useSecureBinding = false;
            var cdmiAddress = useSecureBinding ? "https://emicloudbuild:8080" : "http://emicloudbuild:2365";
            var username = "user";
            var password = "cdmipass";
            var creds = new NetworkCredential(username, password);
            var fileName = "test";
            var uri = string.Format("{0}/{1}", cdmiAddress, fileName);

            var singleRef = new SingleReference
            {
                Name = "SingleRef",               
                Ref = new Reference(relFilePath, new CDMIBlobReference
                {
                    Credentials = creds,
                    URI = uri,
                    FileLocation = relFilePath,
                RequestFactory = url =>
                {
                    var request = (HttpWebRequest)WebRequest.Create(url);
                    return request;
                } }) };

            singleRef.Upload(pathWorkDir);

            Assert.AreEqual<string>(singleRef.GetFileLocation(pathWorkDir), fullPath, "AzureBlobReference is not returning correct localfilepath");
            singleRef.Download(pathWorkDir, new CancellationTokenSource());
            Assert.IsTrue(File.Exists(fullPath), "File was not stored at the given location");
            
            singleRef.Upload(pathWorkDir);
            Directory.Delete(@"C:\temp\output", true);
        }

        [TestMethod]
        public void UploadNotExistingFile()
        {
            var pathWorkDir = @"C:\temp";
            var relFilePath = @"filethatdowsnotexist";
            var fullPath = Path.Combine(pathWorkDir, relFilePath);

            var account = CloudStorageAccount.Parse(CloudSettings.GenericWorkerCloudConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference("testcontainer");
            blobContainer.CreateIfNotExist();
            var blob = blobContainer.GetBlobReference("testblob");
            var singleRef = new SingleReference { Name = "SingleRef", Ref = new Reference(relFilePath, new AzureBlobReference(blob, CloudSettings.GenericWorkerCloudConnectionString)) };
            try
            {
                singleRef.Upload(pathWorkDir);
            }
            catch (FileNotFoundException)
            {
                return;
            }
            finally
            {
                //more tests
            }
            Assert.Fail("FileNotFoundException was not thrown");
        }

        [TestMethod]
        public void SerializeHttpGetReferenceTest()
        {
            var doc = new XmlDocument();
            var a = new HttpGetReference
            {
                DataAddress = "http://www.microsoft.com/index.html"
            };
            var e = a.Serialize(doc);
            Assert.IsNotNull(e.Attributes["DataAddress"]);
            Assert.AreEqual<string>(a.DataAddress, e.Attributes["DataAddress"].Value,"serialization for HttpGetReference is not working");
            var b = new HttpGetReference();
            b.LoadContents(e);
            Assert.AreEqual<string>(b.DataAddress, e.Attributes["DataAddress"].Value, "deserialization for HttpGetReference is not working");
        }

        [TestMethod]
        public void LastModifiedHttpGetTest()
        {
            var httpRef = new HttpGetReference("http://www.gutenberg.org/");
            var lastMod = httpRef.GetLastModificationDateTime();
            var lastModYahoo = (new HttpGetReference("http://uk.yahoo.com/?p=us")).GetLastModificationDateTime();
            Assert.IsTrue(lastModYahoo.Ticks>lastMod.Ticks,"finally the MS site is updated");
        }

        [TestMethod]
        public void DataExistsAndDownloadHttpGetRequestTest()
        {
            var doc = new XmlDocument();
            var a = new HttpGetReference
            {
                DataAddress = "http://www.microsoft.com"
            };
            Assert.IsTrue(a.ExistsDataItem(),"HttpGetReference ExistsDataItem provides false negatives");
            var content = a.DownloadContents();
            Assert.IsTrue(content.Length > 0, "HttpGetReference Download was not successful");

            a = new HttpGetReference
            {
                DataAddress = "http://www.thissitedoesnotexistandthisjustatest.com"
            };
            Assert.IsFalse(a.ExistsDataItem(), "HttpGetReference ExistsDataItem provides false positives");
        }

        [TestMethod]
        public void SerializeAzureBlobReferenceTest()
        {
            var doc = new XmlDocument();
            var a = new AzureBlobReference
            {
                ConnectionString = CloudSettings.TestCloudConnectionString,
                DataAddress = "https://venusstorage.blob.core.windows.net/teststore/1.bin"
            };
            var e = a.Serialize(doc);
            Assert.IsNotNull(e.Attributes, "Doc has no attributes at all");
            Assert.IsNotNull(e.Attributes["ConnectionString"],"ConnectionString attribute is missing");
            Assert.IsNotNull(e.Attributes["DataAddress"], "DataAddress attribute is missing");

            var b = new AzureBlobReference();
            b.LoadContents(e);
            Assert.AreEqual<string>(a.ConnectionString, b.ConnectionString, "(de)serialization for AzureBlobReference is not working");
            Assert.AreEqual<string>(a.DataAddress, b.DataAddress,"(de)serialization for AzureBlobReference is not working" );
        }

        [TestMethod]
        public void SerializeCDMIBlobReferenceTest()
        {
            var doc = new XmlDocument();

            bool useSecureBinding = false;
            var cdmiAddress = useSecureBinding ? "https://emicloudbuild:8080" : "http://emicloudbuild:2365";
            var username = "user";
            var password = "cdmipass";

            var fileName = "testFile";
            var inputUri = string.Format("{0}/{1}", cdmiAddress, fileName);
            var cdmiRef = new CDMIBlobReference()
            {
                URI = inputUri,
                FileLocation = fileName,
                Credentials = new NetworkCredential(username, password),
                RequestFactory = url =>
                {
                    var request = (HttpWebRequest)WebRequest.Create(url);
                    return request;
                }
            };
            
            var e = cdmiRef.Serialize(doc);
            Assert.IsNotNull(e.Attributes, "Doc has no attributes at all");
            Assert.IsNotNull(e.Attributes["URI"], "URI attribute is missing");
            Assert.IsNotNull(e.Attributes["LocalFileName"], "LocalFileName attribute is missing");
            Assert.IsNotNull(e.Attributes["Username"], "Username attribute is missing");
            Assert.IsNotNull(e.Attributes["Password"], "Password attribute is missing");

            var b = new CDMIBlobReference();
            b.LoadContents(e);
            Assert.AreEqual<string>(cdmiRef.URI, b.URI, "(de)serialization for CDMIBlobReference is not working");
            Assert.AreEqual<string>(cdmiRef.FileLocation, b.FileLocation, "(de)serialization for CDMIBlobReference is not working");
            Assert.AreEqual<string>(cdmiRef.Credentials.UserName, b.Credentials.UserName, "(de)serialization for CDMIBlobReference is not working");
            Assert.AreEqual<string>(cdmiRef.Credentials.Password, b.Credentials.Password, "(de)serialization for CDMIBlobReference is not working");
        }

        [TestMethod]
        public void SerializeReferenceTest()
        {
            var doc = new XmlDocument();
            var azureBlobRefEl = new AzureBlobReference
            {
                ConnectionString = CloudSettings.TestCloudConnectionString,
                DataAddress = "https://venusstorage.blob.core.windows.net/teststore/1.bin"
            };
            var refEl = new Reference("input.txt",azureBlobRefEl);
            var e = refEl.Serialize(doc);

            Assert.IsNotNull(e.Attributes["LocalFileName"], "LocalFileName attribute is missing");
            Assert.IsNotNull(e.GetElementsByTagName("AzureBlobReference"));

            var refEl2 = new Reference();
            refEl2.LoadContents(e);
            Assert.AreEqual<string>(refEl.LocalFileName, refEl2.LocalFileName, "(de)serialization for Reference is not working");
            //in order to fully load a reference which is not nested in a singlereference, reference needs to be registered in the argrepo
        }

        [TestMethod]
        public void SerializeReferenceArrayTest()
        {
            var doc = new XmlDocument();
            var azureBlobRefEl = new AzureBlobReference
            {
                ConnectionString = CloudSettings.TestCloudConnectionString,
                DataAddress = "https://venusstorage.blob.core.windows.net/teststore/1.bin"
            };
            var refEl = new Reference("input.txt", azureBlobRefEl);
            var azureBlobRefEl2 = new AzureBlobReference
            {
                ConnectionString = CloudSettings.TestCloudConnectionString,
                DataAddress = "https://venusstorage.blob.core.windows.net/teststore/2.bin"
            };
            var refEl2 = new Reference("input.txt", azureBlobRefEl2);
            var multipleRefs = new ReferenceArray
            {
                Name = "inputfiles",
                References = new ReferenceCollection
                {
                    refEl,
                    refEl2
                }
            };
            var e = multipleRefs.Serialize(doc);

            var a2 = m_ArgumentRepository.Load(e) as ReferenceArray;
            Assert.IsNotNull(a2);
            Assert.AreEqual<string>(multipleRefs.Name, a2.Name);
            Assert.AreEqual<int>(multipleRefs.References.Count, a2.References.Count, "The number of references in the orignal refArray does not match to number of the references after serialization and deserialization");
            var nestedAzureBlobRef2 = a2.References[1].ProviderSpecificReference as AzureBlobReference;
            Assert.AreEqual<string>(azureBlobRefEl2.ConnectionString, nestedAzureBlobRef2.ConnectionString);
            Assert.AreEqual<string>(azureBlobRefEl2.DataAddress, nestedAzureBlobRef2.DataAddress);

            //test also conversion to list of singleReferences:

            List<SingleReference> srl = multipleRefs.GetSingleReferenceList();
            Assert.IsNotNull(srl);
            Assert.AreEqual<int>(srl.Count, multipleRefs.References.Count, "The SingleReference list dows not contain all references that are in the ReferenceArray");
            Assert.AreEqual<string>(azureBlobRefEl2.ConnectionString, (srl[1].Ref.ProviderSpecificReference as AzureBlobReference).ConnectionString);
            Assert.AreEqual<string>(azureBlobRefEl2.DataAddress, (srl[1].Ref.ProviderSpecificReference as AzureBlobReference).DataAddress);
        }

        [TestMethod]
        public void SerializeSingleReferenceArgumentTest()
        {
            var doc = new XmlDocument();
            var azureBlobRefEl = new AzureBlobReference
            {
                ConnectionString = CloudSettings.TestCloudConnectionString,
                DataAddress = "https://venusstorage.blob.core.windows.net/teststore/1.bin"
            };
            var refEl = new Reference("input.txt",azureBlobRefEl);

            var singleRef = new SingleReference
            {
                Name = "inputfile",
                Ref = refEl
            };
            var e = singleRef.Serialize(doc);

            var a2 = m_ArgumentRepository.Load(e) as SingleReference;
            var nestedAzureBlobRef = a2.Ref.ProviderSpecificReference as AzureBlobReference;
            Assert.IsNotNull(a2);
            Assert.AreEqual<string>(singleRef.Name, a2.Name);
            Assert.AreEqual<string>(azureBlobRefEl.ConnectionString, nestedAzureBlobRef.ConnectionString);
            Assert.AreEqual<string>(azureBlobRefEl.DataAddress, nestedAzureBlobRef.DataAddress);
        }
    }
}