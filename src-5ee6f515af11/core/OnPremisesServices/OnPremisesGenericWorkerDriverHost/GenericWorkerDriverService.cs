//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.EMIC.Cloud.WindowsService;
using System.Threading;
using System.ComponentModel.Composition.Hosting;
using Microsoft.EMIC.Cloud.GenericWorker;
using Microsoft.EMIC.Cloud.GenericWorker.AzureProvider;
using Microsoft.EMIC.Cloud.Storage.Azure;
using Microsoft.EMIC.Cloud.GenericWorker.SampleNotifications;
using System.Security.Cryptography.X509Certificates;
using Microsoft.EMIC.Cloud;
using Microsoft.EMIC.Cloud.Security;

namespace OnPremisesGenericWorkerDriverHost
{
    public partial class GenericWorkerDriverService : WindowsService
    {
        public static void TraceLine(string msg)
        {
            msg = DateTime.Now + " " + msg;
            Trace.WriteLine(msg);
        }

        const string GenericWorkerDriverServiceName = "Microsoft.EMIC.Cloud.Generic Worker Driver Service";
        object _syncLock;
        CompositionContainer _container;
        GenericWorkerDriver _driver;
        CancellationTokenSource _cts;
        Process _process;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
            GenericWorkerDriverService service = new GenericWorkerDriverService();
            service.Run(args == null || args.Length == 0);
        }

        public GenericWorkerDriverService()
            : base(GenericWorkerDriverServiceName)
        {
            this._syncLock = new object();
            InitializeComponent();
        }

        public void Initialize()
        {
            _cts = new CancellationTokenSource();
            _container = GenericWorkerDriverHostContainerSettings.Settings.Container;

            try
            {
                _driver = _container.GetExportedValue<GenericWorkerDriver>();
                if (_driver == null)
                {
                    throw new Exception("Driver cannot be extracted from the container, please check the reference assemblies");
                }
            }
            catch (Exception e)
            {
                TraceLine(String.Format("Microsoft.EMIC.Cloud.Generic Worker Driver Service:MEF Error: Driver cannot be extracted from container Exception: {0}.", e));
                throw;
            }
            ThreadPool.QueueUserWorkItem(new WaitCallback(this.CopyTheNecessaryCertificates));
        }


        /// <summary>
        /// Starts the service.
        /// </summary>
        /// <returns></returns>
        public override bool StartService()
        {
            TraceLine("Start Service is called");
            ThreadPool.QueueUserWorkItem(new WaitCallback(this.StartAdministratorHostThread));
            ThreadPool.QueueUserWorkItem(new WaitCallback(this.StartupThreadProc));
            
            return true;
        }

        /// <summary>
        /// Stops the service.
        /// </summary>
        public override void StopService()
        {
            TraceLine("Stop Service is called");
            lock (this._syncLock)
            {
                try
                {
                    _cts.Cancel();
                    GenericWorkerDriver.KillProcessTree(_process);
                }
                catch (Exception e)
                {
                    TraceLine(String.Format("Microsoft.EMIC.Cloud.Generic Worker Driver Service: Unable to stop.  Exception: {0}.", e));
                    throw;
                }
            }
        }

        public void StartupThreadProc(object notUsed)
        {
            EventLog.WriteEntry(this.ServiceName, "Start Service is called", EventLogEntryType.Information);
            Initialize();
            EventLog.WriteEntry(this.ServiceName, "Successfully started.", EventLogEntryType.Information);
            _driver.Run(_cts.Token);
            EventLog.WriteEntry(this.ServiceName, "Cancellation Token is cancelled. Quiting...", EventLogEntryType.Information);
        }

        public void StartAdministratorHostThread(object notUsed)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = @"AdministratorHost\Microsoft.EMIC.Cloud.Administrator.Host.exe",
                WindowStyle = ProcessWindowStyle.Hidden 
            };

            _process = new Process() 
            {
                StartInfo = startInfo
            };

            TraceLine("CurrentDirectory:" + System.IO.Directory.GetCurrentDirectory());
            TraceLine("FileName:" + startInfo.FileName);

            try
            {
                _process.Start();
                _process.WaitForExit();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error launching process");
                for (var e = ex; e != null; e = e.InnerException)
                {
                    Trace.TraceError(ex.Message);
                    Trace.TraceError(ex.StackTrace);
                }

                throw;
            }
        }

        public void CopyTheNecessaryCertificates(object notUsed)
        {
            // For STS and / or self issued tokens, we need to trust the certificate.
            // To be sure we also trust self signed certificates, it needs to be copied to
            // TrustedPeople and CA store.
            var STSCertificateThumbprint = _container.GetExportedValue<string>(CompositionIdentifiers.STSCertificateThumbprint);
            var stsCert = X509Helper.GetX509Certificate2(StoreLocation.LocalMachine, StoreName.My, STSCertificateThumbprint, X509FindType.FindByThumbprint);
            if (stsCert != null)
            {
                var trustedRootCAStore = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                var trustedPeopleStore = new X509Store(StoreName.TrustedPeople, StoreLocation.LocalMachine);
                try
                {
                    trustedRootCAStore.Open(OpenFlags.ReadWrite);
                    trustedPeopleStore.Open(OpenFlags.ReadWrite);

                    trustedRootCAStore.Add(stsCert);
                    trustedPeopleStore.Add(stsCert);

                    trustedRootCAStore.Close();
                    trustedPeopleStore.Close();
                }
                catch (Exception ex)
                {
                    Trace.TraceInformation("Problem in certificate copying:" + ex.ToString());
                    throw;
                }
            }
            else
            {
                Trace.TraceInformation("OnStart(): STS certificate couldn't be found.");
                //throw new Exception("STS certificate cannot be found!");
            }
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