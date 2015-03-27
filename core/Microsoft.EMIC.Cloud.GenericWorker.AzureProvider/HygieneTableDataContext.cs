//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Microsoft.EMIC.Cloud.GenericWorker.AzureProvider
{
    /// <summary>
    /// The <see cref="TableServiceContext"/> to interact with the hygiene coordination table. 
    /// 
    /// The hygiene table only contains a single row, which helps multiple workers to coordinate who is 
    /// responsible for cleaning up the jobs tables. 
    /// </summary>
    class HygieneTableDataContext : TableServiceContext
    {
        public HygieneTableDataContext(string baseAddress, StorageCredentials credentials, string tableName)
            : base(baseAddress, credentials) 
        { 
            this.TableName = tableName; 
            this.IgnoreResourceNotFoundException = true; 
        }

        public string TableName { get; private set; }

        public IQueryable<HygieneTableSchedulingEntity> HygieneStatus
        {
            get { return this.CreateQuery<HygieneTableSchedulingEntity>(this.TableName); }
        }
    }
}