//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EMIC.Cloud.DataManagement;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.Xml;
using System.Threading;
using System.Diagnostics;

namespace Microsoft.EMIC.Cloud.Storage.Azure
{
    /// <summary>
    /// Azure Blob Reference class, implements IProviderSpecificReference
    /// </summary>
    [ProviderSpecificArgumentExport(LocalName = LocalName,
        NamespaceURI = IArgumentExtensions.GenericWorkerNamespace,
        Type = typeof(AzureBlobReference))]
    public class AzureBlobReference : IProviderSpecificReference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBlobReference"/> class.
        /// </summary>
        public AzureBlobReference() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBlobReference"/> class.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="connectionString">The connection string.</param>
        public AzureBlobReference(string address, string connectionString)
        {
            this.DataAddress = address;
            this.ConnectionString = connectionString;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBlobReference"/> class.
        /// </summary>
        /// <param name="blob">The BLOB.</param>
        /// <param name="connectionString">The connection string.</param>
        public AzureBlobReference(CloudBlob blob, string connectionString)
        {
            this.DataAddress = blob.Uri.AbsoluteUri;
            this.ConnectionString = connectionString;
        }

        internal const string LocalName = "AzureBlobReference";
        
        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        /// <value>
        /// The connection string.
        /// </value>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the data address.
        /// </summary>
        /// <value>
        /// The data address.
        /// </value>
        public string DataAddress { get; set; }

        /// <summary>
        /// Sets the file location of the reference
        /// </summary>
        /// <value>
        /// The path to the file location.
        /// </value>
        public string FileLocation { private get; set; }

        //integer value showing whether the thread is running = 0, ranToCompletion =1 or faulted=-1
        private int _threadReturn = 0;

        // Timespan to define to timeout for download / upload operations
        private TimeSpan BLOB_DEFAULT_TIMEOUT = TimeSpan.FromHours(2);

        private class DataToBePassedToThread
        {
            public CloudBlob Blob { get; set; }
            public string WorkingDirectory { get; set; }
        }

        /// <summary>
        /// Loads the contents.
        /// </summary>
        /// <param name="xmlElement">The XML element.</param>
        public void LoadContents(XmlElement xmlElement)
        {
            DataAddress = xmlElement.Attributes["DataAddress"].Value;
            ConnectionString = xmlElement.Attributes["ConnectionString"].Value;
        }

        /// <summary>
        /// Serializes the specified doc.
        /// </summary>
        /// <param name="doc">The doc.</param>
        /// <returns></returns>
        public XmlElement Serialize(XmlDocument doc)
        {
            var e = doc.CreateElement("AzureBlobReference",
                IArgumentExtensions.GenericWorkerNamespace);

            Action<string, string> add = (name, value) =>
            {
                var a = doc.CreateAttribute(name);
                a.Value = value;
                e.Attributes.Append(a);
            };

            add("ConnectionString", ConnectionString);
            add("DataAddress", DataAddress);

            return e;
        }

        /// <summary>
        /// Existses the data item.
        /// </summary>
        /// <returns></returns>
        public bool ExistsDataItem()
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(ConnectionString);
            CloudBlobClient blobClient = account.CreateCloudBlobClient();
            var blob = blobClient.GetBlobReference(DataAddress);
            try
            {
                blob.FetchAttributes();
                return true;
            }
            catch (StorageClientException ex)
            {
                if (ex.ErrorCode != StorageErrorCode.ResourceNotFound)
                {
                    Console.WriteLine("exception message: {0}", ex.Message);
                    throw;
                }
                return false;
            }
        }

        /// <summary>
        /// Gets the last modification date time of the referenced blob.
        /// </summary>
        /// <returns></returns>
        public DateTime GetLastModificationDateTime()
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(ConnectionString);
            CloudBlobClient blobClient = account.CreateCloudBlobClient();
            var blob = blobClient.GetBlobReference(DataAddress);
            try
            {
                blob.FetchAttributes();
                return blob.Properties.LastModifiedUtc;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Gets the file location.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <returns></returns>
        public string GetFileLocation(string workingDirectory)
        {
            if (FileLocation == null)
            {
                var path = new Uri(DataAddress).LocalPath;
                Func<string, string> getFilename = (x => x.Substring(x.LastIndexOf("/") + 1));
                var filename = getFilename(path);
                FileLocation = new FileInfo(Path.Combine(workingDirectory, filename)).FullName;
            }
            return Path.Combine(workingDirectory, FileLocation);
        }

        private void MakeActualDownload(object data)
        {
            var blob = ((DataToBePassedToThread)data).Blob;
            var workingDirectory = ((DataToBePassedToThread)data).WorkingDirectory;
            try
            {
                var bro = new BlobRequestOptions();
                bro.Timeout = BLOB_DEFAULT_TIMEOUT;
                blob.DownloadToFile(GetFileLocation(workingDirectory), bro);
                _threadReturn = 1;
            }
            catch (Exception)
            {
                _threadReturn = -1;
            }
        }

        /// <summary>
        /// Downloads the specified working directory.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="token">The token.</param>
        public void Download(string workingDirectory, CancellationTokenSource token)
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(ConnectionString);
            CloudBlobClient blobClient = account.CreateCloudBlobClient();
            var blob = blobClient.GetBlobReference(DataAddress);

            Action<object> action = obj =>
            {
                var thread = new Thread(MakeActualDownload);
                thread.Start(new DataToBePassedToThread { Blob = blob, WorkingDirectory = workingDirectory });

                while (true)
                {
                    if (_threadReturn == -1)
                    {
                        throw new Exception(string.Format(ExceptionMessages.DownloadError, DataAddress));
                    }
                    else
                    {
                        if (_threadReturn == 1)
                        {
                            break;
                        }
                        if (token.IsCancellationRequested)
                        {
                            thread.Abort();
                        }
                    }

                    Thread.Sleep(10000);
                }
            };

            var downloadTask = Task.Factory.StartNew(action, DataAddress, token.Token);

            downloadTask.ContinueWith(task =>
            {
                Trace.TraceInformation(string.Format("Download of address {0} is {1}", task.AsyncState, task.Status.ToString()));

                if (task.IsFaulted)
                {
                    Trace.TraceError(string.Format("Error downloading {0}", task.AsyncState));
                    for (Exception e = task.Exception; e != null; e = e.InnerException)
                    {
                        Trace.TraceError(string.Format("\t{0}", e.Message));
                    }
                }
                else if (task.Status == TaskStatus.RanToCompletion)
                {
                    Trace.TraceInformation(string.Format("\tDownloaded successully {0} ", task.AsyncState));
                }
                else
                {
                    Trace.TraceInformation(string.Format("\tDownload of {0} is {1}", task.AsyncState, task.Status.ToString()));
                }
            });

            try
            {
                downloadTask.Wait();
                string hakan = downloadTask.Status.ToString();
            }
            catch (Exception)
            {
                token.Cancel();
                try
                {
                    if (File.Exists(GetFileLocation(workingDirectory)))
                    {
                        File.Delete(GetFileLocation(workingDirectory));
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError(string.Format("Error deleting the local file {0}", GetFileLocation(workingDirectory)));
                    for (Exception e = ex; e != null; e = e.InnerException)
                    {
                        Trace.TraceError(string.Format("\t{0}", e.Message));
                    }
                }
            }
        }

        /// <summary>
        /// Uploads the specified working directory.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        public void Upload(string workingDirectory)
        {
            UploadFile(GetFileLocation(workingDirectory));
        }

        private void UploadFile(string fileLocation)
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(ConnectionString);
            CloudBlobClient blobClient = account.CreateCloudBlobClient();
            var blob = blobClient.GetBlobReference(DataAddress);
            var bro = new BlobRequestOptions();
            bro.Timeout = BLOB_DEFAULT_TIMEOUT;
            Action<IAsyncResult> endMethod = null;
            var fs = File.OpenRead(fileLocation);

            var uploadTask = Task.Factory.FromAsync(
                blob.BeginUploadFromStream,
                blob.EndUploadFromStream,
                fs,
                bro,
                endMethod);

            uploadTask.Wait();
            fs.Dispose();
        }

        /// <summary>
        /// Downloads the contents.
        /// </summary>
        /// <returns></returns>
        public byte[] DownloadContents()
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(ConnectionString);
            CloudBlobClient blobClient = account.CreateCloudBlobClient();
            var blob = blobClient.GetBlobReference(DataAddress);
            var bro = new BlobRequestOptions();
            bro.Timeout = BLOB_DEFAULT_TIMEOUT;
            Action<IAsyncResult> endMethod = null;
            
            var ms = new MemoryStream();
            var downloadTask = Task.Factory.FromAsync(
                blob.BeginDownloadToStream,
                blob.EndDownloadToStream,
                ms,
                bro,
                endMethod);

            downloadTask.Wait();

            return ms.ToArray();
        }

    }

}
