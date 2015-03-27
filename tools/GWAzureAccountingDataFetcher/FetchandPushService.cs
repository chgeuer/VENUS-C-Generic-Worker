//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using Microsoft.EMIC.Cloud.GenericWorker.AzureAccounting;
using Microsoft.WindowsAzure;
using Microsoft.EMIC.Cloud.WindowsService;
using Microsoft.WindowsAzure.StorageClient;

namespace GWAzureAccountingDataFetcher
{
    public partial class FetchAndPushService : WindowsService
    {
        public static void TraceLine(string msg)
        {
            msg = DateTime.Now + " " + msg;
            Trace.WriteLine(msg);
        }

        const string AccountingServiceName = "Venus-C Fetch and Push Accounting Data Service";
        object syncLock;
        static volatile bool serviceIsRunning = false;
        private CloudStorageAccount _account;
        private AccountingTableDataContext _accountingTbd;
        private AccountingTableDataContext _accountingPushedTbd;
        private AccountingTableDataContext _accountingErrorTbd;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            FetchAndPushService service = new FetchAndPushService();
            // Run as an application if there is at least one command line argument.
            service.Run(args == null || args.Length == 0);
        }

        public FetchAndPushService()
            : base(AccountingServiceName)
        {
            this._account = CloudStorageAccount.Parse(Properties.Settings.Default.GenericWorkerConnectionString);
            _accountingTbd = new AccountingTableDataContext(
               this._account.TableEndpoint.AbsoluteUri,
               this._account.Credentials, Properties.Settings.Default.GenericWorkerAccountingTableName);
            _accountingPushedTbd = new AccountingTableDataContext(
               this._account.TableEndpoint.AbsoluteUri,
               this._account.Credentials, Properties.Settings.Default.GenericWorkerAccountingPushedTableName);
            _accountingErrorTbd = new AccountingTableDataContext(
               this._account.TableEndpoint.AbsoluteUri,
               this._account.Credentials, Properties.Settings.Default.GenericWorkerAccountingErrorTableName);
            this.syncLock = new object();
            InitializeComponent();
        }


        /// <summary>
        /// Starts the service.
        /// </summary>
        /// <returns></returns>
        public override bool StartService()
        {
            CloudTableClient tc = this._account.CreateCloudTableClient();
            int retries = 0;
            for (retries = 0; retries < 5; retries++)
            {
                try
                {
                    if (!tc.DoesTableExist(Properties.Settings.Default.GenericWorkerAccountingPushedTableName))
                    {
                        tc.CreateTableIfNotExist(Properties.Settings.Default.GenericWorkerAccountingPushedTableName);
                    }

                    if (!tc.DoesTableExist(Properties.Settings.Default.GenericWorkerAccountingErrorTableName))
                    {
                        tc.CreateTableIfNotExist(Properties.Settings.Default.GenericWorkerAccountingErrorTableName);
                    }
                    break;
                }
                catch (StorageClientException)
                {
                    System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(200));
                }
            }
            if (retries >= 5)
            {
                return false;
            }
            TraceLine("Start Service is called");
            ThreadPool.QueueUserWorkItem(new WaitCallback(this.StartupThreadProc));
            return true;
        }

        /// <summary>
        /// Stops the service.
        /// </summary>
        public override void StopService()
        {
            TraceLine("Stop Service is called");
            lock (this.syncLock)
            {
                try
                {
                    serviceIsRunning = false;
                }
                catch (Exception e)
                {
                    TraceLine(String.Format("Venus-C Fetch and Push Accounting Data Service: Unable to stop.  Exception: {0}.", e));
                    throw;
                }
            }
        }

        private List<AccountingTableEntity> GetAllFinishedAndNotPushedAccountingInfo()
        {
            return _accountingTbd.AllAccountingInfo.Where(entity => entity.IsFinished == true).ToList();
        }

        private bool SaveIntothePushedTable(AccountingTableEntity accountingInfo)
        {
            for (int i = 0; i < 10; i++)
            { 
                try
                {
                    _accountingPushedTbd.AddAccountingInfo(accountingInfo.Clone());
                    break;
                }
                catch 
                {

                }
            }

            var data = _accountingPushedTbd.AllAccountingInfo.Where(x => x.PartitionKey == accountingInfo.PartitionKey && x.RowKey == accountingInfo.RowKey).SingleOrDefault();
            if (data != null)
            {
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        _accountingTbd.DeleteAccountingInfo(accountingInfo);
                        return true;
                    }
                    catch
                    {
                    }
                }
            }
            return false;
        }

        private void IncreaseTheNumberofTries(AccountingTableEntity accountingInfo)
        {
            accountingInfo.TriesToPush += 1;
            for (int i = 0; i < 10; i++)
            {
                try
                {

                    _accountingTbd.UpdateAccountingInfo(accountingInfo);
                    return;
                }
                catch
                {

                }
            }
        }

        private bool SaveIntotheErrorTable(AccountingTableEntity accountingInfo)
        {
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    _accountingErrorTbd.AddAccountingInfo(accountingInfo.Clone());
                    break;
                }
                catch
                {

                }
            }

            var data = _accountingErrorTbd.AllAccountingInfo.Where(entity => entity.RowKey == accountingInfo.RowKey && entity.RowKey == accountingInfo.RowKey).FirstOrDefault();
            if (data != null)
            {
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        _accountingTbd.DeleteAccountingInfo(accountingInfo);
                        return true;
                    }
                    catch
                    {
                    }
                }
            }
            return false;
        }


        public void StartupThreadProc(object notUsed)
        {
            TimeSpan updateFrequency = Properties.Settings.Default.UpdateFrequency;
            serviceIsRunning = true;
            VenusAccountingClient client  =  new VenusAccountingClient(Properties.Settings.Default.JobPostUrl, Properties.Settings.Default.UserName, Properties.Settings.Default.Password);
            do
            {
                int NotPushedItemCount = 0;
                try
                {
                    lock (this.syncLock)
                    {
                        //first fetch the data from the accounting table
                        var accountingList = GetAllFinishedAndNotPushedAccountingInfo();

                        //now convert them to a suitable format
                        foreach (var accountingInfo in accountingList)
                        {
                            //if something here encountered, move job directly to the error table
                            JobResourceUsage jobResourceUsage = new JobResourceUsage();
                            try
                            {   
                                jobResourceUsage.ConsumerID = accountingInfo.PartitionKey;
                                jobResourceUsage.CreateTime = accountingInfo.StartTime.Value;
                                jobResourceUsage.CPUDuration = 0;
                                jobResourceUsage.CreatorID = Properties.Settings.Default.CreatorID;
                                jobResourceUsage.Disk = 0;
                                jobResourceUsage.EndTime = accountingInfo.EndTime.Value;
                                jobResourceUsage.ID = accountingInfo.RowKey;
                                jobResourceUsage.JobEndTime = accountingInfo.EndTime.Value;
                                jobResourceUsage.JobID = accountingInfo.RowKey;
                                jobResourceUsage.JobName = accountingInfo.CustomerJobID;
                                jobResourceUsage.JobStartTime = accountingInfo.StartTime.Value;
                                jobResourceUsage.Memory = 0;
                                jobResourceUsage.NumberofCores = accountingInfo.NumberofCores;
                                jobResourceUsage.Processors = accountingInfo.NumberofCores;
                                jobResourceUsage.NetworkIn = accountingInfo.ReceivedBytesEnd - accountingInfo.ReceivedBytesStart;
                                jobResourceUsage.NetworkOut = accountingInfo.SentBytesEnd - accountingInfo.SentBytesStart;
                                jobResourceUsage.NumberOfInputFiles = accountingInfo.NumberOfInputFiles;
                                jobResourceUsage.NumberOfOutputFiles = accountingInfo.NumberofOutputFiles;
                                jobResourceUsage.refVM = accountingInfo.InstanceID;
                                jobResourceUsage.ResourceOwner = Properties.Settings.Default.ResourceOwner;
                                jobResourceUsage.ResourceType = "job";
                                jobResourceUsage.SizeOfInputFiles = accountingInfo.SizeofInputFiles;
                                jobResourceUsage.SizeOfOutputFiles = accountingInfo.SizeofOutputFiles;
                                jobResourceUsage.StartTime = accountingInfo.StartTime.Value;
                                jobResourceUsage.Status = accountingInfo.Status;
                                jobResourceUsage.WallDuration = 0;
                            }
                            catch (Exception malFormatEx)
                            {
                                SaveIntotheErrorTable(accountingInfo);
                                Trace.TraceError(String.Format("\r\n{0} : Item Moved to Error Table. MalFormatException in item: {2}\r\n\r\n.Please see the details:\r\n {1}\r\n", DateTime.Now, jobResourceUsage.ToString(), malFormatEx.ToString()));
                                continue;
                            }


                            bool succeded = false;
                            for (int i = 0; i < 10; i++)
                            {
                                try
                                {
                                    succeded = client.Post(jobResourceUsage);
                                }
                                catch(Exception postEx)
                                {
                                    if (i == 9)
                                    { 
                                        Trace.TraceError(String.Format("\r\n{0} : An error encountered while posting the item: {2}\r\n\r\n.Please see the details:\r\n {1}\r\n", DateTime.Now, jobResourceUsage.ToString(), postEx.ToString()));
                                    }
                                }

                                if (succeded)
                                {
                                    break;
                                }
                                Thread.Sleep(1000);
                            }

                            if (!succeded)
                            {
                                NotPushedItemCount++;
                                IncreaseTheNumberofTries(accountingInfo);
                                if (accountingInfo.TriesToPush >= 10)
                                {
                                    SaveIntotheErrorTable(accountingInfo);
                                }
                                continue;
                            }

                            if (!SaveIntothePushedTable(accountingInfo))
                            {
                                //ROLLBACK the Information
                                for (int i = 0; i < 5; i++)
                                {
                                    try
                                    {
                                        if (client.DeleteRecord(jobResourceUsage))
                                        {
                                            break;
                                        }
                                        Thread.Sleep(1000);
                                    }
                                    catch (Exception exx)
                                    {
                                        Trace.TraceError(String.Format("\r\n{0} : An error encountered in the service.Please see the details:\r\n {1}\r\n", DateTime.Now, exx.ToString()));
                                    }
                                }
                            }
                        }

                        Trace.TraceInformation(String.Format("INFO: {0} : {1} jobs' accounting data has been posted to the Accounting and Billing Service\r\n, {2} jobs' accounting data has been move to error table to be investigated\r\n", DateTime.Now, accountingList.Count - NotPushedItemCount, NotPushedItemCount));
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError(String.Format("\r\n{0} : An error encountered in the service.Please see the details:\r\n {1}\r\n", DateTime.Now, ex.ToString()));
                }
                Thread.Sleep(updateFrequency);
            }
            while (serviceIsRunning);
            EventLog.WriteEntry(this.ServiceName, "Successfully started.", EventLogEntryType.Information);
        }

        public override void WaitForFinish()
        {
            Console.WriteLine("Press any key to exit");
            Console.Read();
            this.StopService();
            TraceLine("Stop Service is called");
        }
    }
}
