//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Configuration;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.EMIC.Cloud.UserAdministration;

namespace Microsoft.EMIC.Cloud.Administrator.Host
{
    class Program
    {
        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(CtrlHandler handler, bool add);

        static readonly CancellationTokenSource Cts = new CancellationTokenSource();

        static void Main(string[] args)
        {
            Console.Title = "Admin host";

            var fn = ConfigurationManager.AppSettings["identityFile"];
            var exe = new FileInfo(typeof(Program).Assembly.Location);
            var fi = new FileInfo(Path.Combine(exe.DirectoryName, fn));
            Console.WriteLine(string.Format("Reading process identity from {0}", fi.FullName));

            Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);
            SetConsoleCtrlHandler(new CtrlHandler(ConsoleCtrlCheck), true);

            var task = CreateProfileHost.CreateTask(fi.FullName, Cts, new HostState());
            task.RunSynchronously();
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Cts.Cancel();
        }

        public delegate bool CtrlHandler(CtrlTypes CtrlType);
        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            Cts.Cancel();

            return true;
        }
    }

    public enum CtrlTypes
    {
        CTRL_C_EVENT = 0,
        CTRL_BREAK_EVENT,
        CTRL_CLOSE_EVENT,
        CTRL_LOGOFF_EVENT = 5,
        CTRL_SHUTDOWN_EVENT
    }

}