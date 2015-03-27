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
using System.ServiceProcess;
using System.Threading;
using System.Runtime.InteropServices;

namespace Microsoft.EMIC.Cloud.WindowsService
{
    /// <summary>
    /// This class can be used to turn a application into a windows service.
    /// Just derive your program class from this class and override the StartService and StopService functions accordingly.
    /// By calling Initialize, the user can determine by the value of the parameter "runAsService" if the program should be
    /// run as a windows service, or as a normal application.
    /// </summary>
    public class WindowsService : ServiceBase
    {
        private Thread serviceWorkerThread;
        private AutoResetEvent serviceWorkerThreadStop;
        private string serviceName;
        private NativeMethods nativeMethods = new NativeMethods();
        private bool runAsService;
        private bool enableLogging = true;

        public bool RunAsService
        {
            get { return runAsService; }
        }

        public bool EnableLogging
        {
            get { return enableLogging; }
            set { enableLogging = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsService"/> class.
        /// </summary>
        /// <param name="serviceName">Name of the service which is used for event log tracing.</param>
        public WindowsService(string serviceName)
        {
            this.serviceName = serviceName;
        }

        /// <summary>
        /// This function needs to be overridden by the deriving class.
        /// All the startup code must be placed here. The startup code
        /// should initialize and all background processes that need to be there for the service/app to
        /// be fully operational. This function should not block.
        /// </summary>
        /// <returns>true if successful, false otherwise</returns>
        virtual public bool StartService()
        {
            return true;
        }

        /// <summary>
        /// This function needs to be overridden by the deriving class.
        /// All the cleanup code must be placed here. The cleanup code
        /// should deinitialize and stop all background processes that were started in StartService.
        /// </summary>
        virtual public void StopService()
        {
        }

        /// <summary>
        /// This function needs to be overridden by the deriving class.
        /// It waits for the application to finish. This can be done i.e.
        /// by just waiting for the user to press "ENTER" in a console based application.
        /// This function is only used for the non-service startup of the program.
        /// </summary>
        virtual public void WaitForFinish()
        {
        }

        /// <summary>
        /// Initializes the program and starts it as a windows service or an application.
        /// </summary>
        /// <param name="runAsService">if set to <c>true</c> [run as service].</param>
        public void Run(bool runAsService)
        {
            TextWriterTraceListener textWriterTraceListener = null;

            // Store the boolean switch.
            this.runAsService = runAsService;

            // Check if we should start as an application, not as a service.
            if (!runAsService)
            {
                // Trace listeners are only used if they are switched on.
                if (enableLogging)
                {
                    textWriterTraceListener = new TextWriterTraceListener(Console.Out);
                    // Enable command line logging for the start as an console application.
                    Trace.Listeners.Add(textWriterTraceListener);
                }

                try
                {
                    if (StartService())
                    {
                        // Now run until <return> has been pressed
                        WaitForFinish();
                    }
                    StopService();
                }
                catch (Exception e)
                {
                    // Error starting the service
                    Trace.TraceError("Error starting the service: " + e.Message);
                }

                // Trace listeners are only used if they are switched on.
                if (enableLogging)
                {
                    // Cleanup tracing
                    Trace.Listeners.Remove(textWriterTraceListener.Name);
                    textWriterTraceListener.Dispose();
                }
            }
            else
            {
                // We should start as a windows service
                Run(this);
            }
        }

        /// <summary>
        /// Entry point when the windows service is started
        /// </summary>
        /// <param name="arguments">Arguments passed to the windows service.</param>
        protected override void OnStart(string[] arguments)
        {
            // Signal the windows service is starting up
            IntPtr handle = this.ServiceHandle;
            this.nativeMethods.myServiceStatus.currentState = (int)NativeMethods.State.SERVICE_START_PENDING;
            NativeMethods.SetServiceStatus(handle, ref nativeMethods.myServiceStatus);

            // Check if the service worker thread is not already running
            if (serviceWorkerThread == null)
            {
                // Create a new auto reset event and start the thread
                this.serviceWorkerThreadStop = new AutoResetEvent(false);
                this.serviceWorkerThread = new Thread(new ThreadStart(ServiceWorkerThreadFunction));
                this.serviceWorkerThread.Start();
            }

            // Signal the windows service has started
            nativeMethods.myServiceStatus.currentState = (int)NativeMethods.State.SERVICE_RUNNING;
            NativeMethods.SetServiceStatus(handle, ref nativeMethods.myServiceStatus);
        }

        /// <summary>
        /// Entry point for the windows service if the service should be stopped.
        /// </summary>
        protected override void OnStop()
        {
            // Signal that the windows service is shutting down
            IntPtr handle = this.ServiceHandle;
            this.nativeMethods.myServiceStatus.currentState = (int)NativeMethods.State.SERVICE_STOP_PENDING;
            NativeMethods.SetServiceStatus(handle, ref nativeMethods.myServiceStatus);

            // Check if the worker thread is running
            if (serviceWorkerThread != null)
            {
                // Signal the worker thread to stop
                this.serviceWorkerThreadStop.Set();
                // Wait for the thread to shut down
                while (!serviceWorkerThread.Join(1000))
                    this.RequestAdditionalTime(1500);
                serviceWorkerThread = null;
                // Shut down the service
                StopService();
            }

            // Signal that the windows service has shutted down
            nativeMethods.myServiceStatus.currentState = (int)NativeMethods.State.SERVICE_STOPPED;
            NativeMethods.SetServiceStatus(handle, ref nativeMethods.myServiceStatus);
        }

        /// <summary>
        /// Worker thread working function.
        /// </summary>
        private void ServiceWorkerThreadFunction()
        {
            if (StartService())
            {
                this.serviceWorkerThreadStop.WaitOne();
            }
        }

    }

    /// <summary>
    /// All native pinvoke stuff is placed into this separate class.
    /// </summary>
    class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct SERVICE_STATUS
        {
            public int serviceType;
            public int currentState;
            public int controlsAccepted;
            public int win32ExitCode;
            public int serviceSpecificExitCode;
            public int checkPoint;
            public int waitHint;
        }

        internal enum State
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }

        [DllImport("ADVAPI32.DLL", EntryPoint = "SetServiceStatus")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetServiceStatus(IntPtr hServiceStatus, ref SERVICE_STATUS lpServiceStatus);

        internal SERVICE_STATUS myServiceStatus;
    }
}
