//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure;

namespace Microsoft.EMIC.Cloud.GenericWorker.AzureAccounting
{
    public class AccountingTableDataContext : TableServiceContext
    {
        public AccountingTableDataContext(string baseAdddress, StorageCredentials credentials, string tableName)
            : base(baseAdddress, credentials)
        {
            this.TableName = tableName;
            this.IgnoreResourceNotFoundException = true;
        }

        public string TableName { get; private set; }

        public IQueryable<AccountingTableEntity> AllAccountingInfo
        {
            //get { return this.CreateQuery<AccountingTableEntity>(this.TableName); }
            // The old solution didn't support paging if there are two many result entities.
            get { return this.CreateQuery<AccountingTableEntity>(this.TableName).AsTableServiceQuery<AccountingTableEntity>(); }
        }

        public void AddAccountingInfo(AccountingTableEntity accountingTableEntity)
        {
            var maxRetries=5;
            for (int i = 0; i <= maxRetries; i++)
            {
                try
                {
                    this.AddObject(this.TableName, accountingTableEntity);
                }
                catch (InvalidOperationException)
                {
                    // This exception occurs, if the object has already been added to the TableServiceContext.
                    // Unfortunately there is no direct "contains" or a similar method, so let's just always
                    // add the object and ignore a possible exception.
                }
                try
                {
                    this.SaveChangesWithRetries();
                    break;
                }
                
                catch (StorageClientException)
                {
                    if (i == maxRetries)
                    {
                        throw;
                    }
                }
            }
        }

        public void UpdateAccountingInfo(AccountingTableEntity accountingTableEntity)
        {
            var maxRetries = 5;
            for (int i = 0; i <= maxRetries; i++)
            {
                try
                {
                    this.UpdateObject(accountingTableEntity);
                    this.SaveChangesWithRetries();
                    break;
                }
                catch (StorageClientException)
                {
                    if (i == maxRetries)
                    {
                        throw;
                    }
                }
            }
        }

        public void DeleteAccountingInfo(AccountingTableEntity accountingTableEntity)
        {
            var maxRetries = 5;
            for (int i = 0; i <= maxRetries; i++)
            {
                try
                {
                    this.DeleteObject(accountingTableEntity);
                    this.SaveChangesWithRetries();
                    break;
                }
                catch (StorageClientException)
                {
                    if (i == maxRetries)
                    {
                        throw;
                    }
                }
            }
        }
    }
}
