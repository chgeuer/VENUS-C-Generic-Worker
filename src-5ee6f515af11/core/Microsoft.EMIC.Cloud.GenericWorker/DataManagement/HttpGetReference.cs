//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.EMIC.Cloud.DataManagement
{
    /// <summary>
    /// A class to provide HTTP download calls for file references
    /// </summary>
    [ProviderSpecificArgumentExport(
    LocalName = LocalName,
    NamespaceURI = IArgumentExtensions.GenericWorkerNamespace,
        Type = typeof(HttpGetReference))]
    public class HttpGetReference : IProviderSpecificReference
    {
        internal const string LocalName = "HttpGetReference";

        /// <summary>
        /// Gets or sets the data address.
        /// </summary>
        /// <value>
        /// The data address.
        /// </value>
        public string DataAddress { get; set; }

        /// <summary>
        /// Sets the file location.
        /// </summary>
        /// <value>
        /// The file location.
        /// </value>
        public string FileLocation {private get; set;}

        //integer value showing whether the thread is running = 0, ranToCompletion =1 or faulted=-1
        private int threadReturn = 0;

        // Timespan to define to timeout for download / upload operations
        private TimeSpan BLOB_DEFAULT_TIMEOUT = TimeSpan.FromHours(2); //Todo: make configurable

        private class DataToBePassedToThread
        {
            public string dataUri;
            public string workingDirectory;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpGetReference"/> class.
        /// </summary>
        public HttpGetReference() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpGetReference"/> class.
        /// </summary>
        /// <param name="dataAddress">The data address.</param>
        public HttpGetReference(string dataAddress)
        {
            DataAddress = dataAddress;
        }

        /// <summary>
        /// Loads the data address of the given XML element.
        /// </summary>
        /// <param name="xmlElement">The XML element.</param>
        public void LoadContents(XmlElement xmlElement)
        {
            DataAddress = xmlElement.Attributes["DataAddress"].Value;
        }

        /// <summary>
        /// Serializes the data address to the given XML document.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <returns></returns>
        public XmlElement Serialize(XmlDocument doc)
        {
            var e = doc.CreateElement(LocalName,
                IArgumentExtensions.GenericWorkerNamespace);

            Action<string, string> add = (name, value) =>
            {
                var a = doc.CreateAttribute(name);
                a.Value = value;
                e.Attributes.Append(a);
            };

            add("DataAddress", DataAddress);

            return e;
        }


        /// <summary>
        /// Checks whether the data time exists.
        /// </summary>
        /// <returns></returns>
        public bool ExistsDataItem()
        {
            HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(DataAddress);
            webRequest.Method = "HEAD";
 
            try
            {
                var response = webRequest.GetResponse();
                response.Close();                
            }
            catch (WebException)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Get a file location in the given working directory.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <returns></returns>
        public string GetFileLocation(string workingDirectory)
        {
            if (FileLocation == null)
            {
                var path = new Uri(DataAddress).LocalPath;
                Func<string, string> getFilename = ((x) => x.Substring(x.LastIndexOf("/") + 1));
                var filename = getFilename(path);
                FileLocation = new FileInfo(Path.Combine(workingDirectory, filename)).FullName;
            }
            return Path.Combine(workingDirectory, FileLocation);
        }

        private void MakeActualDownload(object data)
        {
            var uri = ((DataToBePassedToThread)data).dataUri;
            var workingDirectory = ((DataToBePassedToThread)data).workingDirectory;
            try
            {
                var fileLoc = GetFileLocation(workingDirectory);
                new WebClient().DownloadFile(uri,fileLoc ); //Todo: change to HttpWebRequest and set timeout
                threadReturn = 1;
            }
            catch (Exception)
            {
                threadReturn = -1;
            }
        }

        /// <summary>
        /// Downloads the file to the specified working directory.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="token">The cancellation token.</param>
        public void Download(string workingDirectory, CancellationTokenSource token)
        {
            Action<object> action = (object obj) =>
            {
                var thread = new Thread(MakeActualDownload);
                thread.Start(new DataToBePassedToThread { dataUri = DataAddress, workingDirectory = workingDirectory });

                while (true)
                {
                    if (threadReturn == -1)
                    {
                        throw new Exception(string.Format(ExceptionMessages.DownloadError, DataAddress));
                    }
                    else if (threadReturn == 1)
                    {
                        break;
                    }
                    else if (token.IsCancellationRequested)
                    {
                        thread.Abort();
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(10));
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
        /// Uploads the file from the specified working directory. (not supported!)
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        public void Upload(string workingDirectory)
        {
            throw new NotSupportedException(ExceptionMessages.FileUploadsNotSupportedByHttpGetReference);
        }

        /// <summary>
        /// Downloads the content of the file from the data adress.
        /// </summary>
        /// <returns></returns>
        public byte[] DownloadContents()
        {
            var downloadTask = new WebClient().DownloadDataTask(DataAddress);

            downloadTask.Wait();

            return downloadTask.Result;
        }



        /// <summary>
        /// Gets the last modification date time of the referenced file.
        /// </summary>
        /// <returns></returns>
        public DateTime GetLastModificationDateTime()
        {
            HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(DataAddress);
            webRequest.Method = "HEAD";

            try
            {
                var response = (HttpWebResponse)webRequest.GetResponse();
                var lastModified = response.LastModified;
                response.Close();
                return lastModified;
            }
            catch (WebException)
            {
                throw;
            }
        }
    }
}