//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.EMIC.Cloud.GenericWorker
{
    /// <summary>
    /// Timer class for the Generic Worker
    /// </summary>
    public class GenericWorkerTimer
    {
        /// <summary>
        /// Gets the interval.
        /// </summary>
        public Func<TimeSpan> Interval { get; private set; }

        /// <summary>
        /// Gets the action.
        /// </summary>
        public Action Action { get; private set; }
        
        private readonly Func<TimeSpan> _dueTime;

        private CancellationToken _mCt;

        internal static Task Run(Func<TimeSpan> interval, Action action, CancellationToken ct)
        {
            return Task.Factory.StartNew(() =>
            { 
                var timer = new GenericWorkerTimer(null, interval, action, ct);
                timer.Run(); 
            }, ct);
        }

        internal static Task Run(Func<TimeSpan> dueTime, Func<TimeSpan> interval, Action action, CancellationToken ct)
        {
            return Task.Factory.StartNew(() =>
            {
                var timer = new GenericWorkerTimer(dueTime, interval, action, ct);
                timer.Run();
            }, ct);
        }

        private GenericWorkerTimer(Func<TimeSpan> dueTime, Func<TimeSpan> interval, Action action, CancellationToken ct)
        {
            this.Interval = interval;
            this._dueTime = dueTime;
            this.Action = action;
            this._mCt = ct;
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        private void Run()
        {
            // if none cancels m_ct, this cycles forever
            Func<TimeSpan> currentInterval = 
                _dueTime == null ? this.Interval : this._dueTime;

            while (!this._mCt.WaitHandle.WaitOne(currentInterval()))
            {
                this.Action();

                currentInterval = this.Interval;
            }
        }
    }
}