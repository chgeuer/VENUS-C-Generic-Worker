//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Microsoft.EMIC.DevelopmentFabric.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// A cross-process barrier based on <see cref="Mutex"/>es. 
    /// </summary>
    public class CrossProcessBarrier
    {
        /// <summary>
        /// Gets the key of the last created instance.
        /// </summary>
        public string Current { get; private set; }
        /// <summary>
        /// Gets the peers of the last created instance.
        /// </summary>
        public IEnumerable<string> Peers { get; private set; }
        /// <summary>
        /// Gets the waiting time of the last created instance.
        /// </summary>
        public TimeSpan WaitingTime { get; private set; }
        private readonly Mutex _selfMutex;
        private const string GlobalMutexName = "MicrosoftEMICCrossProcessBarrierGlobalMutex";

        /// <summary>
        /// This dictionary is highly critical to ensure that the <see cref="CrossProcessBarrier"/> objects (which contain
        /// the <see cref="Mutex"/>w) are never retrieved by the garbage collector. Otherwise, one process might have passed 
        /// the barrier, removes the selfMutex, and then the other processes assume it has not yet reached the barrier. 
        /// </summary>
        private readonly static Dictionary<string, CrossProcessBarrier> Instances = new Dictionary<string, CrossProcessBarrier>();  
        private readonly static object ThisLock = new object();

        /// <summary>
        /// Gets a new instance of the <see cref="CrossProcessBarrier"/> class with the specified parameters
        /// </summary>
        /// <param name="current">The current.</param>
        /// <param name="peers">The peers.</param>
        /// <param name="waitingTime">The waiting time.</param>
        /// <returns></returns>
        public static CrossProcessBarrier GetInstance(string current, IEnumerable<string> peers, TimeSpan waitingTime)
        {
            if (!Instances.ContainsKey(current))
            {
                lock (ThisLock)
                {
                    if (!Instances.ContainsKey(current))
                    {
                        Instances.Add(current, new CrossProcessBarrier(current, peers, waitingTime));
                    }
                }
            }

            return Instances[current];
        }

        private CrossProcessBarrier(string current, IEnumerable<string> peers, TimeSpan waitingTime)
        {
            if (current == null) throw new ArgumentNullException("current");
            if (peers == null) throw new ArgumentNullException("peers");

            Current = current;
            Peers = peers;
            WaitingTime = waitingTime;

            _selfMutex = Open(Current);
        }

        /// <summary>
        /// This method is called to enter the barrier. The thread is released, if all peers entered also entered the barrier.
        /// </summary>
        public void Wait()
        {
            #region comment
            // The assumption for this section is the following. For 5 role instances in the development fabric, we will have 6 Mutex objects:
            // 
            // The overall Mutex called 
            // 
            //                    MicrosoftCorpnetAuthenticationFixerWaiter 
            //
            // The selfMutex for the current role instance called
            // 
            //                    deployment(400).GenericWorkerRole.Cloud.WebRole.2
            //
            // The peerMutex collection containing 
            //
            //                    deployment(400).GenericWorkerRole.Cloud.WebRole.0
            //                    deployment(400).GenericWorkerRole.Cloud.WebRole.1
            //                    deployment(400).GenericWorkerRole.Cloud.WebRole.3
            //                    deployment(400).GenericWorkerRole.Cloud.WebRole.4
            //
            // Once an instance starts, it locks it's own selfMutex. Then, gated by the overallMutex, the an instance tests
            // whether all peerMutexes are claimed. If that's the case, we assume that all instances are now syncronized in 
            // the development fabric and we can start configuring the application pools. 
            //
            #endregion
            
            var selfMutexClaimed = _selfMutex.WaitOne();
            if (!selfMutexClaimed)
                throw new Exception("Cannot claim Mutex " + Current);

            using (Mutex overallMutex = Open(GlobalMutexName))
            {
                var allPeersRunning = false;
                while (!allPeersRunning)
                {
                    overallMutex.WaitOne();
                    allPeersRunning = !Peers.Any(DoesNotExist); // all peers running when there are not any peers that do not exist
                    overallMutex.ReleaseMutex();

                    if (!allPeersRunning)
                    {
                        Thread.Sleep(WaitingTime); //  before trying again
                    }
                }
            }

            _selfMutex.ReleaseMutex();
        }

        private static Mutex Open(string name)
        {
            try
            {
                return Mutex.OpenExisting(name);
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                try
                {
                    // we try to create a new one, becaus it did not exist. But that could 
                    // actually fail, because a second process news up it faster than we ...
                    //
                    return new Mutex(false, name);
                }
                catch (WaitHandleCannotBeOpenedException)
                {
                    // ... so that we then just open the existing one
                    //
                    return Mutex.OpenExisting(name);
                }
            }
        }

        private static bool DoesNotExist(string name)
        {
            try
            {
                using (Mutex.OpenExisting(name))
                {
                    return false;
                }
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                return true;
            }
        }
    }
}