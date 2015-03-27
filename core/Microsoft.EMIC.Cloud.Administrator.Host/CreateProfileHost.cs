//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ServiceModel.Description;
using Microsoft.EMIC.Cloud.UserAdministration;

namespace Microsoft.EMIC.Cloud.Administrator.Host
{
    public static class CreateProfileHost
    {
        private static readonly Mutex ApplicationStartMutex = new Mutex(false, "{8F6F0AC4-B9A1-42FF-A8CF-73F04E7BDE8F}");

        public static Task CreateTask(string filename, CancellationTokenSource cts, HostState isReady)
        {
            var token = cts.Token;
            Action<object> action = state =>
            {
                bool lockAcquired = ApplicationStartMutex.WaitOne(TimeSpan.Zero, true);
                if (!lockAcquired)
                {
                    Trace.TraceError("Application is already running");
                    return;
                }

                ServiceHost sh = null;
                try
                {
                    var instance = new CreateProfileImpl(filename, cts);
                    sh = new ServiceHost(instance);
                    sh.AddServiceEndpoint(typeof(ICreateProfile), Settings.Binding, Settings.Address);
                    sh.Description.Behaviors.Find<ServiceDebugBehavior>().IncludeExceptionDetailInFaults = true;
                    Trace.TraceInformation(string.Format("Opening {0} host", typeof(CreateProfileImpl).Name));
                    sh.Open();
                    Trace.TraceInformation(string.Format("Opened {0} host", typeof(CreateProfileImpl).Name));
                    var hState = state as HostState;
                    if (hState != null)
                    {
                        hState.SetServiceHost(sh);
                    }

                    while (true)
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }
                        else
                        {
                            Thread.Sleep(1000);
                        }
                    }
                }
                finally
                {
                    Trace.TraceInformation("Releasing lock...");
                    ApplicationStartMutex.ReleaseMutex();

                    if (sh != null && sh.State == CommunicationState.Opened)
                    {
                        Trace.TraceInformation(string.Format("Closing {0} host", typeof(CreateProfileImpl).Name));
                        sh.Close();
                    }
                }
            };

            return new Task(action, isReady,token);
        }
    }
}