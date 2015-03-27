//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.EMIC.Cloud.DataManagement
{
    /// <summary>
    /// A class to provide an FTP reference where you can download or upload data from
    /// </summary>
    [ProviderSpecificArgumentExport(
    LocalName = LocalName,
    NamespaceURI = IArgumentExtensions.GenericWorkerNamespace,
        Type = typeof(FtpReference))]
    public class FtpReference : IProviderSpecificReference
    {
        internal const string LocalName = "FtpReference";

        /// <summary>
        /// Gets or sets the data address.
        /// </summary>
        public string DataAddress { get; set; }
        /// <summary>
        /// Gets or sets the ftp username.
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// Gets or sets the ftp password.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Sets the file location.
        /// </summary>
        /// <value>
        /// The file location.
        /// </value>
        public string FileLocation { private get; set; }

        //integer value showing whether the thread is running = 0, ranToCompletion =1 or faulted=-1
        private int threadReturn = 0;

        // Timespan to define to timeout for download / upload operations
        private TimeSpan BLOB_DEFAULT_TIMEOUT = TimeSpan.FromHours(2); //Todo: make configurable

        private class DataToBePassedToThread
        {
            public string dataUri;
            public string workingDirectory;
            public string username;
            public string password;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="FtpReference"/> class.
        /// </summary>
        public FtpReference()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpReference"/> class.
        /// </summary>
        /// <param name="dataAddress">The data address.</param>
        public FtpReference(string dataAddress)
        {
            DataAddress = dataAddress;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpReference"/> class.
        /// </summary>
        /// <param name="dataAddress">The data address.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        public FtpReference(string dataAddress, string username, string password)
        {
            DataAddress = dataAddress;
            UserName = username;
            Password = password;
        }

        /// <summary>
        /// Loads the contents from the xml element.
        /// </summary>
        /// <param name="xmlElement">The XML element.</param>
        public void LoadContents(XmlElement xmlElement)
        {
            DataAddress = xmlElement.Attributes["DataAddress"].Value;
            if (xmlElement.HasAttribute("UserName"))
            {
                UserName = xmlElement.Attributes["UserName"].Value;
                Password = xmlElement.Attributes["Password"].Value;
            }
        }

        /// <summary>
        /// Serializes the class to the specified xml document
        /// </summary>
        /// <param name="doc">The xml document</param>
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
            if (!string.IsNullOrEmpty(UserName))
            {
                add("UserName", UserName);
                add("Password", Password);
            }
            return e;
        }


        /// <summary>
        /// Checks whether the data item exists
        /// </summary>
        /// <returns></returns>
        public bool ExistsDataItem()
        {
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(new Uri(DataAddress));

            ftpRequest.Method = WebRequestMethods.Ftp.GetDateTimestamp;
            if (!string.IsNullOrEmpty(UserName))
                ftpRequest.Credentials = new NetworkCredential(UserName, Password);
            ftpRequest.UseBinary = true;
            ftpRequest.Proxy = null;

            try
            {                
                var response = ftpRequest.GetResponse();
                response.Close();
            }
            catch (WebException)
            {
                return false;
            }
            return true;
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
                Func<string, string> getFilename = ((x) => x.Substring(x.LastIndexOf("/") + 1));
                var filename = getFilename(path);
                FileLocation = new FileInfo(Path.Combine(workingDirectory, filename)).FullName;
            }
            return Path.Combine(workingDirectory, FileLocation);
        }

        private void MakeActualDownload(object data)
        {
            try
            {                
                var uri = ((DataToBePassedToThread)data).dataUri;
                var workingDirectory = ((DataToBePassedToThread)data).workingDirectory;
                var userName = ((DataToBePassedToThread)data).username;
                var password = ((DataToBePassedToThread)data).password;

                WebClient wc = new WebClient(); //Todo: change to FtpWebRequest and set timeout

                if (!string.IsNullOrEmpty(userName))
                    wc.Credentials = new NetworkCredential(userName, password);
                wc.DownloadFile(uri, GetFileLocation(workingDirectory));

                threadReturn = 1;
            }
            catch (Exception)
            {
                threadReturn = -1;
            }
        }

        /// <summary>
        /// Downloads to the specified working directory.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="token">The cancelation token to abort the download.</param>
        public void Download(string workingDirectory, CancellationTokenSource token)
        {
            Action<object> action = (object obj) =>
            {
                Thread thread = new Thread(MakeActualDownload);
                thread.Start(new DataToBePassedToThread { dataUri = DataAddress, workingDirectory = workingDirectory, username=UserName, password=Password });

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
        /// Uploads from the specified working directory.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        public void Upload(string workingDirectory)
        {
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(DataAddress);
            if (!string.IsNullOrEmpty(UserName))
                ftpRequest.Credentials = new NetworkCredential(UserName, Password);
            ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;
            ftpRequest.UseBinary = true;
            ftpRequest.Timeout = (int)BLOB_DEFAULT_TIMEOUT.TotalMilliseconds;

            var reqStream = ftpRequest.GetRequestStream();
            StreamReader stream = new StreamReader(GetFileLocation(workingDirectory));
            var fileBytes = Encoding.UTF8.GetBytes(stream.ReadToEnd());
            stream.Close();
            reqStream.Write(fileBytes, 0, fileBytes.Length);
            reqStream.Close();
            FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
            Console.WriteLine("ftp upload status: {0}", ftpResponse.StatusDescription);
            ftpResponse.Close();
        }

        /// <summary>
        /// Deletes the specified file from ftp.
        /// </summary>
        /// <returns></returns>
        public bool Delete()
        {
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(new Uri(DataAddress));

            ftpRequest.Method = WebRequestMethods.Ftp.DeleteFile;
            if (!string.IsNullOrEmpty(UserName))
                ftpRequest.Credentials = new NetworkCredential(UserName, Password);
            ftpRequest.UseBinary = true;
            ftpRequest.Proxy = null;

            try
            {
                var response = ftpRequest.GetResponse();
                response.Close();
            }
            catch (WebException)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Downloads the contents.
        /// </summary>
        /// <returns></returns>
        public byte[] DownloadContents()
        {
            WebClient wc = new WebClient();
            if (!string.IsNullOrEmpty(UserName))
                wc.Credentials = new NetworkCredential(UserName, Password);
            var downloadTask = wc.DownloadDataTask(DataAddress);

            downloadTask.Wait();

            return downloadTask.Result;
        }



        /// <summary>
        /// Gets the last modification date time of the referenced ftp file.
        /// </summary>
        /// <returns></returns>
        public DateTime GetLastModificationDateTime()
        {
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(new Uri(DataAddress));

            ftpRequest.Method = WebRequestMethods.Ftp.GetDateTimestamp;
            if (!string.IsNullOrEmpty(UserName))
                ftpRequest.Credentials = new NetworkCredential(UserName, Password);
            ftpRequest.UseBinary = true;
            ftpRequest.Proxy = null;

            try
            {
                var response = (FtpWebResponse) ftpRequest.GetResponse();
                var LastModified = response.LastModified;
                response.Close();
                return LastModified;
            }
            catch (WebException)
            {
                throw;
            }
        }
    }


}
