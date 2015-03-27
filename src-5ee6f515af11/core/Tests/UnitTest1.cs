//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Tests
{
    public class UnitTest1
    {
        public void AppStoreTest()
        {
            //var folderName = @"..\..\..\SimpleMathConsoleApp\bin\Debug";
            //var mainExe = Path.Combine(folderName, "SimpleMathConsoleApp.exe");

            //var folder = new DirectoryInfo(folderName);
            //if (!Directory.Exists(folderName))
            //{
            //    Console.WriteLine("Does not exist: " + folder.FullName);
            //    return;
            //}

            //// FileInfo zipFileName = new FileInfo(Path.GetTempFileName() + ".zip");
            //var ms = new MemoryStream();
            //using (var zipFile = new ZipFile(/*zipFileName.FullName*/))
            //{
            //    zipFile.AddDirectory(folder.FullName);
            //    zipFile.Save(ms);
            //}
            //var buf = ms.ToArray();


            //var description = new VENUSApplicationDescription()
            //{
            //    ApplicationIdentificationURI = "http://www.microsoft.com/emic/cloud/samples/e-ScienceApp#v1.0",
            //    Author = new VENUSApplicationAuthor()
            //    {
            //        Company = "Microsoft",
            //        EMail = "chgeuer@microsoft.com"
            //    },
            //    Dependencies = new List<CloudApplicationDependencies>()
            //    {
            //        new CloudApplicationDependencies("Java", "5.0"), 
            //        new CloudApplicationDependencies("Windows Azure","1.4"), 
            //        new CloudApplicationDependencies(".NET", "4.0")
            //    },
            //    CommandTemplate = new VENUSCommandTemplate()
            //    {
            //        Executable = mainExe,
            //        Path = "./bin;./lib",
            //        WorkingDirectory = "./bin",
            //        Args = new List<CommandLineArgument>()
            //        {
            //            new CommandLineArgument() 
            //            {
            //                Name = "INPUT",
            //                FormatString = "-in {0}",
            //                Required = Required.Mandatory, 
            //                CommandLineArgType = CommandLineArgType.SingleReferenceArgument
            //            }
            //        }
            //    }
            //};

            //AppStoreClient c = new AppStoreClient(new BasicHttpBinding() { TransferMode = TransferMode.Streamed },
            //    new EndpointAddress("http://localhost/VENUS/AppStore"));

            //c.RegisterApplication(description, buf);

            //var blobAddress = c.GetApplicationDownloadURL(description.ApplicationIdentificationURI);

            //byte[] bits = c.DownloadApplicationBinaries(description.ApplicationIdentificationURI);
            //Console.WriteLine("Downloaded {0} octets", bits.Length);

            //Console.WriteLine("Download also under {0}", c.GetApplicationDownloadURL(description.ApplicationIdentificationURI));
            //byte[] bits2 = c.DownloadApplicationBinariesDirectly(description.ApplicationIdentificationURI);
            //Console.WriteLine("Downloaded {0} octets", bits2.Length);
            //Console.WriteLine("Press <Return> to delete");
            //Console.ReadLine();

            //c.RemoveApplication(description.ApplicationIdentificationURI);


            //var uploadToken = c.GetUploadToken("http://fooapp");
            //System.Net.WebClient wc = new System.Net.WebClient();
            //byte[] response = wc.UploadData(string.Format("{0}{1}{2}", uploadToken.BlobEndpoint, uploadToken.Filepath, uploadToken.SAS), "PUT",
            //    Encoding.UTF8.GetBytes("Hallo"));
            //Console.WriteLine("Uploaded data");

            //var cl = new CloudBlobClient(uploadToken.BlobEndpoint, new StorageCredentialsSharedAccessSignature(uploadToken.SAS));
            //var sasBlob = cl.GetBlobReference(uploadToken.Filepath);
            //sasBlob.UploadText("Hallo Du da!");
            //Console.WriteLine(sasBlob.DownloadText());
        }
    }
}