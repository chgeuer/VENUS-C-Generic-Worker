//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.EMIC.Cloud.GenericWorker
{
    /// <summary>
    /// A utility engine to keep the internal data structures in sync. 
    /// </summary>
    public class Hygiene
    {
        private IGWRuntimeEnvironment RuntimeEnvironment;
        private CancellationToken ct;

        /// <summary>
        /// TimeSpan object of the intervall between runs
        /// </summary>
        public static readonly TimeSpan INTERVALL_BETWEEN_RUNS;
        /// <summary>
        /// TimeSpan object of the intervall hygiene checks
        /// </summary>
        public static readonly TimeSpan INTERVALL_HYGIENE_CHECK;
        /// <summary>
        /// TimeSpan object of the intervall running timeout
        /// </summary>
        public static readonly TimeSpan INTERVALL_RUNNING_TIMEOUT;

        /// <summary>
        /// Initializes the <see cref="Hygiene"/> class.
        /// </summary>
        static Hygiene()
        {
            // Normal operation
            INTERVALL_BETWEEN_RUNS = TimeSpan.FromMinutes(5);
            INTERVALL_HYGIENE_CHECK = TimeSpan.FromMinutes(5);
            INTERVALL_RUNNING_TIMEOUT = TimeSpan.FromHours(1);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Hygiene"/> class.
        /// </summary>
        /// <param name="runtime">The runtime.</param>
        /// <param name="ct">The ct.</param>
        public Hygiene(IGWRuntimeEnvironment runtime, CancellationToken ct)
        {
            if (runtime == null)
            {
                throw new ArgumentException(ExceptionMessages.RuntimeNull);
            }
            this.RuntimeEnvironment = runtime;
            this.ct = ct;
        }

        /// <summary>
        /// Start the Hygiene Task
        /// </summary>
        /// <param name="runtime">Runtime Environment plugged in the GW</param>
        /// <param name="ct">CancellationToken used to cancel the hygiene task when needed</param>
        /// <returns>Hygiene Task </returns>
        public static Task Start(IGWRuntimeEnvironment runtime, CancellationToken ct)
        {
            var hygiene = new Hygiene(runtime, ct);

            return GenericWorkerTimer.Run(hygiene.DueEvery, hygiene.Interval, hygiene.DoHygiene, ct);
        }


        private Func<TimeSpan> DueEvery 
        { 
            get 
            {
                var random = new Random();
                int min = (int)Hygiene.INTERVALL_HYGIENE_CHECK.TotalSeconds / 10;
                int max = (int)Hygiene.INTERVALL_HYGIENE_CHECK.TotalSeconds;

                return () => TimeSpan.FromSeconds(random.Next(min, max)); 
            } 
        }

        private Func<TimeSpan> Interval
        {
            get
            {
                return () => Hygiene.INTERVALL_HYGIENE_CHECK;
            }
        }

        /// <summary>
        /// Make Hygiene Task in each predifined interval.
        /// It checks for the jobs in unstable states -- Running, CheckingInputData and CancelRequested. If the jobs have not been updated for a given time period, 
        /// the jobs which are in the running and checkinginputdata state are moved to submitted and the ones in cancelrequested are moved to cancelled state
        /// </summary>
        private void DoHygiene()
        {
            if (this.RuntimeEnvironment.MarkHygieneAsRunning(INTERVALL_BETWEEN_RUNS))
            {
                foreach (var job in this.RuntimeEnvironment.CurrentJobs.Where(j => j.Status == JobStatus.Running ||  j.Status ==JobStatus.CheckingInputData))
                {
                    if (job.LastChange < DateTime.UtcNow.Subtract(INTERVALL_RUNNING_TIMEOUT))
                    {
                        if (job.ResetCounter >= 3)
                        {
                            this.RuntimeEnvironment.MarkJobAsFailed(job, "Job was resetted three times");
                        }
                        else
                        {
                            this.RuntimeEnvironment.MarkJobAsSubmittedBack(job, "Resubmit after watchdog failure", true);
                        }
                    }
                }

                foreach (var job in this.RuntimeEnvironment.CurrentJobs.Where(j => j.Status == JobStatus.CancelRequested))
                {
                    if (job.LastChange < DateTime.UtcNow.Subtract(INTERVALL_RUNNING_TIMEOUT))
                    {
                        this.RuntimeEnvironment.MarkJobAsCancelled(job, "job Cancelled by Hygiene");
                    }
                }

                while (!this.RuntimeEnvironment.MarkHygieneAsFinished())
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(500));
                }
            }
        }
    }
}