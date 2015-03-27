//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.EMIC.DevelopmentFabric.Utils;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [TestClass]
    public class DevelopmentFabricUtilsTests
    {
        [TestMethod]
        public void CrossProcessBarrierTest()
        {
            IEnumerable<string> allNames = new List<string>
                                {
                                    "deployment(400).GenericWorkerRole.Cloud.WebRole.0_Web",
                                    "deployment(400).GenericWorkerRole.Cloud.WebRole.1_Web",
                                    "deployment(400).GenericWorkerRole.Cloud.WebRole.2_Web",
                                    "deployment(400).GenericWorkerRole.Cloud.WebRole.3_Web",
                                    "deployment(400).GenericWorkerRole.Cloud.WebRole.4_Web"
                                };

            Func<string, string> escapeMutexName = instanceId => instanceId.Replace("(", ".").Replace(")", ".").Replace(".", "");
            allNames = allNames.Select(escapeMutexName);

            var tasks = new List<Task>();
            foreach (var currentName in allNames)
            {
                var peerNames = new List<string>(allNames);
                peerNames.Remove(currentName);

                var c = CrossProcessBarrier.GetInstance(currentName, peerNames, TimeSpan.Zero);
                tasks.Add(Task.Factory.StartNew(c.Wait));
            }
            Task.WaitAll(tasks.ToArray());
        }
    }
}
