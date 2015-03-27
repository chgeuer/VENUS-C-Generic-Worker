//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.Configuration;

namespace Demo.Cleaner
{
    class Program
    {
        static void Main(string[] args)
        {            
            var connectionStr = "UseDevelopmentStorage=true";
            var sak1 = ConfigurationManager.AppSettings["genericworkerstorage"];
            var sak2 = ConfigurationManager.AppSettings["venusdemouserstorage"];

            var account = CloudStorageAccount.Parse(connectionStr);
            //var tc = account.CreateCloudTableClient();
            //tc.DeleteTableIfExist("AllJobs");
            //tc.DeleteTableIfExist("CurrentJobs");
            //tc.DeleteTableIfExist("appstoretable");
            //tc.DeleteTableIfExist("DataCatalogue");
            //Console.WriteLine("Deleted tables");
            //var blob = account.CreateCloudBlobClient();
            //blob.DeleteBlobContainerIfExist("appstoreblob");
            //blob.DeleteBlobContainerIfExist("datatableinternal");
            //blob.DeleteBlobContainerIfExist("gwblobstore");
            //blob.DeleteBlobContainerIfExist("userdata");
            //Console.WriteLine("Deleted blobs");

            var blobClient = account.CreateCloudBlobClient();
            var containers = blobClient.ListContainers();
            foreach (var container in containers)
            {
                container.Delete();
            }
            var tableClient = account.CreateCloudTableClient();
            var tables = tableClient.ListTables();
            foreach (var tableName in tables)
            {
                tableClient.DeleteTable(tableName);
            }

        }
    }

    public static class E
    {
        public static void DeleteBlobContainerIfExist(this CloudBlobClient blob, string containerAddress)
        {
            try
            {
                var c = blob.GetContainerReference(containerAddress);
                if (c != null)
                {
                c.CreateIfNotExist();
                    c.Delete();
                }
            }
            catch (Exception) { }
        }
    }
}
