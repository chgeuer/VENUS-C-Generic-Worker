//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Principal;
using System.Threading;

namespace SimpleMathConsoleApp
{
    class Program
    {
        /// <summary>
        /// this simple sample reads a csv input file with integers, builds the sum and writes an outputfile
        /// </summary>
        /// <param name="args"></param>
         
        //The parameter -mul is just for documentation purposes (LiteralArgument) and is not used in any test scenario
        #region UsedAlsoForDocumentation - SimpleMath Usage
        private const string USAGE = @"SimpleMathConsoleApp.exe -sum -mul <integer> -infile <filename> -outfile <filename> -wait <timeinms>";
        #endregion

        public static void Main(string[] args)
        {
            try
            {
                Console.Error.Write("Just to create an artificial error to check whether stderror is being read");

                Console.WriteLine("Running as user {0}", WindowsIdentity.GetCurrent().Name);

                for (int i = 0; i < args.Length; i++)
                {
                    Console.WriteLine("Argument {0}: \"{1}\"", i, args[i]);
                }

                #region UsedAlsoForDocumentation - SimpleMath main part

                Func<string, string> GetArg = (name) =>
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (string.Equals(name, args[i]))
                            return args[i + 1];
                    }

                    return string.Empty;
                };

                bool shouldWait = args.Contains("-wait");
                if (shouldWait)
                {
                    int waitTime;
                    if (Int32.TryParse(GetArg("-wait"), out waitTime))
                    {
                        Thread.Sleep(waitTime);
                    }
                }

                var infileName = GetArg("-infile");
                var outfileName = GetArg("-outfile");
                bool sumSwitch = args.Contains("-sum");

                int sum = File.ReadAllText(infileName).Split(',').AsEnumerable().ParseWhere().Sum();
                
                if (args.Contains("-mul"))
                {
                    int multiBy = int.Parse(GetArg("-mul"));
                    sum *= multiBy;
                }

                File.WriteAllText(outfileName, sum.ToString());
            }
            catch
            {
                Environment.Exit(-1);
            }

            #endregion

        }
    }
    public static class ParseExtensions
    {
        public static IEnumerable<int> ParseWhere(this IEnumerable<string> values)
        {
            foreach (var s in values)
            {
                int value = int.Parse(s);
                yield return value;
            }
        }
    }
}