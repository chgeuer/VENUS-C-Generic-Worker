//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Net.Cache;

namespace Microsoft.EMIC.Cloud.GenericWorker.AzureAccounting
{
    /// <summary>
    /// Describes a .NET client for VENUS-C REST web services.
    /// </summary>
    public class VenusAccountingClient
    {
        #region Members

        string _serverUrl = string.Empty;
        string _username = string.Empty;
        string _password = string.Empty;

        #endregion

        #region Public methods

        /// <summary>
        /// Makes an HTTP GET web request and returns the response in a string format.
        /// </summary>
        /// <returns>A string representation of the web response.</returns>
        public List<UsageRecord> GetAll()
        {
            // Create a request for the URL. 
            WebRequest request = WebRequest.Create(_serverUrl);

            if (_username != string.Empty && _password != string.Empty)
            {
                request.Credentials = new NetworkCredential(_username, _password);
            }

            HttpRequestCachePolicy noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            request.CachePolicy = noCachePolicy;


            request.ContentType = "application/xml";
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();
            reader.Close();
            response.Close();

            return XmlManager.ParseXML(responseFromServer);
        }

        /// <summary>
        /// Makes an HTTP POST web request.
        /// </summary>
        /// <param name="usageRecord">The usage record to insert.</param>
        /// <returns>True if data posted successfully. False otherwise.</returns>
        public bool Post(UsageRecord usageRecord)
        {
            string xml = XmlManager.CreateXml(usageRecord);

            WebRequest request = WebRequest.Create(_serverUrl);
            request.Method = "POST";
            request.ContentType = "application/xml";

            if (_username != string.Empty && _password != string.Empty)
            {
                request.Credentials = new NetworkCredential(_username, _password);
            }

            byte[] byteArray = Encoding.UTF8.GetBytes(xml);
            request.ContentLength = byteArray.Length;
            // Get the request stream.
            Stream dataStream = request.GetRequestStream();
            // Write the data to the request stream.
            dataStream.Write(byteArray, 0, byteArray.Length);
            // Close the Stream object.
            dataStream.Close();
            // Get the response.
            WebResponse response = request.GetResponse();

            dataStream.Close();
            response.Close();

            return StatusAccepted(((HttpWebResponse)response).StatusCode);
        }

        /// <summary>
        /// Makes an HTTP PUT web request.
        /// </summary>
        /// <param name="usageRecord">The usage record to update.</param>
        /// <returns>True if data put successfully. False otherwise.</returns>
        public bool Put(UsageRecord usageRecord)
        {
            string xml = XmlManager.CreateXml(usageRecord);
            WebRequest request = WebRequest.Create(_serverUrl + "/" + usageRecord.ID);
            request.Method = "PUT";
            request.ContentType = "application/xml";

            if (_username != string.Empty && _password != string.Empty)
            {
                request.Credentials = new NetworkCredential(_username, _password);
            }

            byte[] byteArray = Encoding.UTF8.GetBytes(xml);
            request.ContentLength = byteArray.Length;
            // Get the request stream.
            Stream dataStream = request.GetRequestStream();
            // Write the data to the request stream.
            dataStream.Write(byteArray, 0, byteArray.Length);
            // Close the Stream object.
            dataStream.Close();
            // Get the response.
            WebResponse response = request.GetResponse();


            dataStream.Close();
            response.Close();

            return StatusAccepted(((HttpWebResponse)response).StatusCode);
        }

        /// <summary>
        /// Makes an HTTP DELETE web request. Deletes all records.
        /// </summary>
        /// <returns>True if data deleted successfully. False otherwise.</returns>
        public bool DeleteAll()
        {
            WebRequest request = WebRequest.Create(_serverUrl);
            request.Method = "DELETE";
            request.ContentType = "application/xml";

            if (_username != string.Empty && _password != string.Empty)
            {
                request.Credentials = new NetworkCredential(_username, _password);
            }

            WebResponse response = request.GetResponse();
            response.Close();

            return StatusAccepted(((HttpWebResponse)response).StatusCode);
        }

        /// <summary>
        /// Makes an HTTP DELETE web request. Deletes the specified record only.
        /// </summary>
        /// <param name="record">The record to be deleted</param>
        /// <returns>True if data deleted successfully. False otherwise.</returns>
        public bool DeleteRecord(UsageRecord record)
        {
            WebRequest request = WebRequest.Create(_serverUrl + "/" + record.ID);
            request.Method = "DELETE";
            request.ContentType = "application/xml";

            if (_username != string.Empty && _password != string.Empty)
            {
                request.Credentials = new NetworkCredential(_username, _password);
            }

            WebResponse response = request.GetResponse();
            response.Close();

            return StatusAccepted(((HttpWebResponse)response).StatusCode);
        }

        #endregion

        #region Constructors

        public VenusAccountingClient(string serverUrl, string username, string password)
        {
            _serverUrl = serverUrl;
            _username = username;
            _password = password;
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Determines whether the specified status code shows that no errors occurred.
        /// </summary>
        /// <param name="code">Status code</param>
        /// <returns>True if no problems occurred. False otherwise.</returns>
        internal static bool StatusAccepted(HttpStatusCode code)
        {
            if (code == HttpStatusCode.Accepted ||
                code == HttpStatusCode.Continue ||
                code == HttpStatusCode.Created ||
                code == HttpStatusCode.Found ||
                code == HttpStatusCode.NoContent ||
                code == HttpStatusCode.OK)
            {
                return true;
            }

            return false;
        }

        #endregion
    }
}
