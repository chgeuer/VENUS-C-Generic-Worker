//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using System.Xml;
using System.IO;
using Microsoft.EMIC.Cloud.DataManagement;

namespace KTH.GenericWorker.CDMI
{
    [ProviderSpecificArgumentExport(
        LocalName = CDMIBlobReference.LocalName,
        NamespaceURI = CDMIBlobReference.NamespaceURI,
        Type = typeof(CDMIBlobReference))]
    public class CDMIBlobReference : IProviderSpecificReference
    {
        internal const string LocalName = "CDMIReference";
        internal const string NamespaceURI = "http://www.pdc.kth.se/VENUS-C/CDMI";

        public CDMIBlobReference() 
        {
            this.ContentType = "application/binary";
            this.RequestFactory = (url) => (HttpWebRequest)WebRequest.Create(url);
        }

        #region Our real data

        public string FileLocation { get; set; }
        public string ContentType { get; set; }
        public string URI { get; set; }
        public NetworkCredential Credentials { get; set; }
        public Func<string, HttpWebRequest> RequestFactory { get; set; }

        // Timespan to define to timeout for download / upload operations
        private TimeSpan BLOB_DEFAULT_TIMEOUT = TimeSpan.FromHours(2); //Todo: make configurable

        #endregion

        #region IProviderSpecificReference

        public void Upload(string workingDirectory)
        {
            var fullPath = GetFileLocation(workingDirectory);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("Missing local file", fullPath);
            }

            var req = this.RequestFactory(this.URI);
            req.Credentials = this.Credentials;
            req.Method = "PUT";
            req.ContentType = this.ContentType;            
            req.Timeout = (int)BLOB_DEFAULT_TIMEOUT.TotalMilliseconds;

            using (var localFileStream = File.Open(fullPath, FileMode.Open, FileAccess.Read))
            {
                var requestStream = req.GetRequestStream();
                localFileStream.CopyTo(requestStream);
            }
            using (var response = (HttpWebResponse)req.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK &&
                    response.StatusCode != HttpStatusCode.Created)
                {
                    throw new InvalidOperationException(
                        string.Format("CDMI Upload Problem. HTTP Status code {0}",
                            response.StatusCode.ToString()));
                }
            }
        }

        public void Download(string workingDirectory, CancellationTokenSource tokenSource)
        {
            var fullPath = GetFileLocation(workingDirectory);
            DownloadToStream(() => File.Open(fullPath, FileMode.Create, FileAccess.Write));
        }

        public byte[] DownloadContents()
        {
            var ms = new MemoryStream();
            DownloadToStream(() => ms);
            return ms.ToArray();
        }

        public void Delete()
        {
            var req = this.RequestFactory(this.URI);
            req.Credentials = this.Credentials;
            req.Method = "DELETE";
            req.ContentType = "application/cdmi-object";

            using (var response = (HttpWebResponse)req.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.NoContent)
                {
                    throw new InvalidOperationException(
                        string.Format("CDMI Delete Problem. HTTP Status code {0}",
                            response.StatusCode.ToString()));
                }
            };
        }

        private void DownloadToStream(Func<Stream> GetStream)
        {
            var req = this.RequestFactory(this.URI);
            req.Credentials = this.Credentials;
            req.Method = "GET";
            req.Timeout = (int)BLOB_DEFAULT_TIMEOUT.TotalMilliseconds;

            using (var response = (HttpWebResponse)req.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new InvalidOperationException(
                        string.Format("CDMI Download Problem. HTTP Status code {0}",
                            response.StatusCode.ToString()));
                }
                using (var localFileStream = GetStream())
                {
                    var responseStr = response.GetResponseStream();
                    responseStr.CopyTo(localFileStream);
                }
            }
        }

        public bool ExistsDataItem()
        {
            var req = this.RequestFactory(this.URI);
            req.Credentials = this.Credentials;
            req.Method = "HEAD";
            try
            {
                using (var response = (HttpWebResponse)req.GetResponse())
                {
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (WebException we)
            {
                if (((HttpWebResponse)we.Response).StatusCode  == HttpStatusCode.NotFound) 
                    return false;

                throw;
            }
        }

        /// <summary>
        /// Gets the last modification date time of the referenced blob.
        /// </summary>
        /// <returns></returns>
        public DateTime GetLastModificationDateTime()
        {
            var req = this.RequestFactory(this.URI);
            req.Credentials = this.Credentials;
            req.Method = "HEAD";
            try
            {
                using (var response = (HttpWebResponse)req.GetResponse())
                {
                    return response.LastModified;
                }
            }
            catch (WebException)
            {
                throw;
            }
        }

        public string GetFileLocation(string workingDirectory)
        {
            return Path.Combine(workingDirectory, this.FileLocation);
        }

        #region Serialization

        private class N
        {
            internal const string URI = "URI";
            internal const string Username = "Username";
            internal const string Password = "Password";
            internal const string ContentType = "ContentType";
            internal const string LocalFileName = "LocalFileName";
        }

        public XmlElement Serialize(XmlDocument doc)
        {
            //  <CDMIReference xmlns="http://www.pdc....."   Username="Bob" Password="test123" URI="http://..." />
 
            var e = doc.CreateElement(LocalName, NamespaceURI);

            Action<string, string> add = (name, value) =>
            {
                if (!string.IsNullOrEmpty(value))
                {
                    var a = doc.CreateAttribute(name);
                    a.Value = value;
                    e.Attributes.Append(a);
                }
            };

            add(N.Username, this.Credentials.UserName);
            add(N.Password, this.Credentials.Password);
            add(N.URI, this.URI);
            add(N.ContentType, this.ContentType);
            add(N.LocalFileName, this.FileLocation);

            return e;
        }

        public void LoadContents(XmlElement xmlElement)
        {
            Func<string, bool, string> get = (name, isOptional) =>
            {
                var attr = xmlElement.Attributes[name];
                if (attr == null)
                {
                    if (isOptional) 
                        return null;
                    
                    throw new NotSupportedException(string.Format("Missing attribute \"{0}\"", name));
                }
                return attr.Value;
            };
            Func<string, string, string> getOptional = (name, defaultValue) => 
            {
                var res = get(name, true);
                if (string.IsNullOrEmpty(res)) 
                    return defaultValue;
                return res;
            };
            Func<string, string> getRequired = (name) => get(name, false);

            this.URI = getRequired(N.URI);
            if (this.URI.EndsWith("/"))
            { 
                throw new ArgumentException(
                    "CDMI blob references must not end with a '/' character (which would be a container)...", 
                    N.URI);
            }

            this.Credentials = new NetworkCredential(getRequired(N.Username), getRequired(N.Password));
            this.ContentType = getOptional(N.ContentType, "application/binary");

            this.FileLocation = getOptional(N.LocalFileName, 
                this.URI.Substring(this.URI.LastIndexOf("/") + 1));
        }


        #endregion

        #endregion

        private static void HiddenTestCodeWhichWouldBeWrittenByAUser()
        {
            var r = new CDMIBlobReference() 
            { 
                URI = "http://...", 
                Credentials = new NetworkCredential("Bob", "test"), 
                ContentType =  "plain/text", 
                FileLocation = "1.txt" 
            };
        }

    }

}



/*


 * <soap:Body>
 * <ogf:BES>
 * <jsdl:Arguments>
 * <CDMIReference xmlns="http://www.pdc.....">
 * <Address>http://cdmi.upv.venus-c.eu/azure/data1</Address>
 * <Password></Password>
 * 
 * </CDMIReference>
 * 
 * 

 var theCDMIReference = new Reference()
{
    LocalFileName = localFileName,
    ProviderSpecificReference = new KTH.GenericWorker.CDMI.CDMIBlobReference()
    {
        LocalFileName = localFileName,
        URI = string.Format("https://169.254.233.222:{0}/{1}-seqfileAnewOne.sqf",
            port.ToString(), Guid.NewGuid().ToString()),
        Credentials = new NetworkCredential("aaa", "aaa"),
        RequestFactory = (url) => 
        { 
            var request = (HttpWebRequest)WebRequest.Create(url);

            return request;
        }
    }
};

*/