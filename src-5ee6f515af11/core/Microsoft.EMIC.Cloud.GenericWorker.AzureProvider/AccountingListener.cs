//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Data.Services.Client;
using Microsoft.EMIC.Cloud.GenericWorker.AzureAccounting;
using Microsoft.EMIC.Cloud.Notification;

namespace Microsoft.EMIC.Cloud.GenericWorker.AzureProvider
{
    /// <summary>
    /// This class is notified on all kind of usage events. It handles these events and stores usage information in the accounting table.
    /// </summary>
    [Export(typeof(IUsageEventListener))]
    public class AccountingListener : IUsageEventListener, IPartImportsSatisfiedNotification
    {
        /// <summary>
        /// Gets or sets the generic worker cloud connection string.
        /// </summary>
        /// <value>
        /// The generic worker cloud connection string.
        /// </value>
        [Import(CompositionIdentifiers.GenericWorkerConnectionString)]
        public string GenericWorkerCloudConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the name of the generic worker accounting table.
        /// </summary>
        /// <value>
        /// The name of the generic worker accounting table.
        /// </value>
        [Import(CompositionIdentifiers.DevelopmentGenericWorkerAccountingTableName)]
        public string GenericWorkerAccountingTableName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [generic worker is accounting on].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [generic worker is accounting on]; otherwise, <c>false</c>.
        /// </value>
        [Import(CompositionIdentifiers.GenericWorkerIsAccountingOn)]
        public bool GenericWorkerIsAccountingOn { get; set; }

        private CloudStorageAccount _account;

        void IPartImportsSatisfiedNotification.OnImportsSatisfied()
        {
            this._account = CloudStorageAccount.Parse(this.GenericWorkerCloudConnectionString);
            var tc = this._account.CreateCloudTableClient();

            #region Tables

            if (!tc.DoesTableExist(this.GenericWorkerAccountingTableName))
            {
                tc.CreateTableIfNotExist(this.GenericWorkerAccountingTableName);
            }

            #endregion
        }

        private AccountingTableDataContext GetAll()
        {
            return new AccountingTableDataContext(
               this._account.TableEndpoint.AbsoluteUri,
               this._account.Credentials, this.GenericWorkerAccountingTableName);
        }

        /// <summary>
        /// This method is called by the EventDispatcher class to notify this listener about usage events.
        /// </summary>
        /// <param name="usageEvent">The usage event.</param>
        public void Notify(IUsageEvent usageEvent)
        {
            if (GenericWorkerIsAccountingOn)
            {
                var accountingTable = GetAll();

                AccountingTableEntity record;
                try
                {
                    record = accountingTable.AllAccountingInfo.Where(
                            info => info.PartitionKey == usageEvent.Job.Owner && info.RowKey == usageEvent.Job.InternalJobID).SingleOrDefault();
                }
                catch (DataServiceQueryException)
                {
                    record = null;
                }

                var statEvent = usageEvent as StatusEvent;
                if (statEvent != null)
                {
                    HandleStatusChangeEvent(statEvent, accountingTable, record);
                    return;
                }

                var networkEvent = usageEvent as NetworkEvent;
                if (networkEvent != null)
                {
                    HandleNetworkEvent(networkEvent, accountingTable, record);
                    return;
                }


                var storageEvent = usageEvent as StorageEvent;
                if (storageEvent != null)
                {
                    HandleStorageEvent(storageEvent, accountingTable, record);
                    return;
                }

                var finishEvent = usageEvent as FinishEvent;
                if (finishEvent != null)
                {
                    HandleFinishEvent(finishEvent, accountingTable, record);
                    return;
                }
            }
        }

        private void HandleStatusChangeEvent(StatusEvent statusEvent, AccountingTableDataContext accountingTable, AccountingTableEntity record)
        {
            //It should be a new item in the accounting table, if there exists one this one should be updated, because the VM which job was running is most probably "killed"
            if (statusEvent.NewStatus == JobStatus.Running && statusEvent.OldStatus != statusEvent.NewStatus)
            {
                if (record != null)
                {
                    if (!record.EndTime.HasValue)
                    {
                        throw new Exception(String.Format(ExceptionMessages.FinishedJobSavedInAccounting, statusEvent.Job.InternalJobID));
                    }
                    record.StartTime = statusEvent.NotificationTime;
                    record.InstanceID = statusEvent.Job.InstanceID;
                    accountingTable.UpdateAccountingInfo(record);
                }
                else
                {
                    record = new AccountingTableEntity(statusEvent.Job.Owner, statusEvent.Job.InternalJobID);
                    record.CustomerJobID = statusEvent.Job.CustomerJobID;
                    record.InstanceID = statusEvent.Job.InstanceID;
                    record.StartTime = statusEvent.NotificationTime;
                    record.IsFinished = false;
                    record.NumberofCores = Environment.ProcessorCount;
                    record.Status = statusEvent.NewStatus.ToString();
                    record.TriesToPush = 0;
                    accountingTable.AddAccountingInfo(record);
                }
            }
            else if (statusEvent.OldStatus == JobStatus.Running && (statusEvent.NewStatus == JobStatus.Finished || statusEvent.NewStatus == JobStatus.Failed))
            {
                if (record == null)
                {
                    throw new Exception(String.Format(ExceptionMessages.SettingStartTime, statusEvent.Job.InternalJobID));
                }
                record.EndTime = statusEvent.NotificationTime;
                record.Status = statusEvent.NewStatus.ToString();
                accountingTable.UpdateAccountingInfo(record);
            }
            else if (record != null && statusEvent.OldStatus == JobStatus.CancelRequested && statusEvent.NewStatus == JobStatus.Cancelled)
            {
                record.EndTime = statusEvent.NotificationTime;
                record.Status = statusEvent.NewStatus.ToString();
                accountingTable.UpdateAccountingInfo(record);
            }
            else if (statusEvent.OldStatus == JobStatus.Running && statusEvent.NewStatus == JobStatus.Submitted)
            {
                if (record == null)
                {
                    throw new Exception(String.Format(ExceptionMessages.ResettingData, statusEvent.Job.InternalJobID));
                }
                accountingTable.DeleteObject(record);
                accountingTable.SaveChangesWithRetries();
            }

            return;
        }

        private void HandleNetworkEvent(NetworkEvent networkEvent, AccountingTableDataContext accountingTable, AccountingTableEntity record)
        {
            //Decide whether to save it and whether it is a start event or not
            if (networkEvent.NewStatus == JobStatus.Running && networkEvent.OldStatus != networkEvent.NewStatus)
            {
                if (record == null)
                {
                    throw new Exception(String.Format(ExceptionMessages.SavingNetworkData, networkEvent.Job.InternalJobID));
                }
                //this is start
                record.ReceivedBytesStart = networkEvent.BytesReceived;
                record.SentBytesStart = networkEvent.BytesSent;
            }
            else if (networkEvent.OldStatus == JobStatus.Running && (networkEvent.NewStatus == JobStatus.Finished || networkEvent.NewStatus == JobStatus.Failed))
            {
                if (record == null)
                {
                    throw new Exception(String.Format(ExceptionMessages.SavingNetworkData, networkEvent.Job.InternalJobID));
                }
                //this End
                record.ReceivedBytesEnd = networkEvent.BytesReceived;
                record.SentBytesEnd = networkEvent.BytesSent;
            }
            else if (record != null && networkEvent.OldStatus == JobStatus.CancelRequested && networkEvent.NewStatus == JobStatus.Cancelled)
            {
                //this is end also
                record.ReceivedBytesEnd = networkEvent.BytesReceived;
                record.SentBytesEnd = networkEvent.BytesSent;
            }
            if (record != null)
            {
                record.Status = networkEvent.NewStatus.ToString();
                accountingTable.UpdateAccountingInfo(record);
            }
        }

        private void HandleStorageEvent(StorageEvent storageEvent, AccountingTableDataContext accountingTable, AccountingTableEntity record)
        {
            //Decide whether to save it and whether it is a start event or not
            if (storageEvent.NewStatus == JobStatus.Running && storageEvent.OldStatus != storageEvent.NewStatus)
            {
                if (record == null)
                {
                    throw new Exception(String.Format(ExceptionMessages.SavingStorageData, storageEvent.Job.InternalJobID));
                }
                //this is start
                record.NumberOfInputFiles = storageEvent.NumberofDocuments;
                record.SizeofInputFiles = storageEvent.BytesTransferred;
            }
            else if (storageEvent.OldStatus == JobStatus.Running && (storageEvent.NewStatus == JobStatus.Finished || storageEvent.NewStatus == JobStatus.Failed))
            {
                if (record == null)
                {
                    throw new Exception(String.Format(ExceptionMessages.SavingStorageData, storageEvent.Job.InternalJobID));
                }
                //this End
                record.NumberofOutputFiles = storageEvent.NumberofDocuments;
                record.SizeofOutputFiles = storageEvent.BytesTransferred;
            }
            else if (record != null && storageEvent.OldStatus == JobStatus.CancelRequested && storageEvent.NewStatus == JobStatus.Cancelled)
            {
                //this is end also
                record.NumberofOutputFiles = storageEvent.NumberofDocuments;
                record.SizeofOutputFiles = storageEvent.BytesTransferred;
            }
            if (record != null)
            {
                record.Status = storageEvent.NewStatus.ToString();
                accountingTable.UpdateAccountingInfo(record);
            }
        }

        private void HandleFinishEvent(FinishEvent finishEvent, AccountingTableDataContext accountingTable, AccountingTableEntity record)
        {
            //Decide whether to save it and whether it is a start event or not
            if (record == null)
            {
                throw new Exception(String.Format(ExceptionMessages.SavingStorageData, finishEvent.Job.InternalJobID));
            }

            record.IsFinished = true;
            accountingTable.UpdateAccountingInfo(record);
        }
    }
}
