//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Security.Principal;

namespace SimpleSleepConsoleApp
{
    class Program
    {
        private const string USAGE = @"SimpleSleepConsoleApp.exe -outfile <filename>";

        static void Main(string[] args)
        {
            Console.WriteLine("Running as user {0}", WindowsIdentity.GetCurrent().Name);

            Func<string, string> GetArg = (name) =>
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (string.Equals(name, args[i]))
                        return args[i + 1];
                }

                return string.Empty;
            };

            string sleepingTimeArg = GetArg("-sleeptime");

            var outfileName = GetArg("-outfile");
            Console.WriteLine("output filename: {0}", outfileName);

            int sleepingTime;
            int napTime = 10000; //10 seconds
            if (int.TryParse(sleepingTimeArg, out sleepingTime))
            {
                Console.WriteLine("Going to sleep for {0} seconds", sleepingTime / 1000);
                while (sleepingTime > napTime)
                {
                    sleepingTime -= napTime;
                    Thread.Sleep(napTime);
                    Console.WriteLine("still sleeping ...");
                }
                if (sleepingTime>0) 
                    Thread.Sleep(sleepingTime);
            }

            Console.WriteLine("Now I feel rested");
            File.WriteAllText(outfileName,"Now I feel rested");            
        }
    }
}
