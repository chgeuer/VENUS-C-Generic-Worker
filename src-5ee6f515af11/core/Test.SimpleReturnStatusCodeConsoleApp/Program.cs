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

namespace SimpleReturnStatusCodeConsoleApp
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

            string statusCodeArg = GetArg("-statuscode");
            
            var outfileName = GetArg("-outfile");
            Console.WriteLine("output filename: {0}", outfileName);

            File.WriteAllText(outfileName,"some results\nstatuscode:"+statusCodeArg);

            Environment.Exit(int.Parse(statusCodeArg));
        }
    }
}
