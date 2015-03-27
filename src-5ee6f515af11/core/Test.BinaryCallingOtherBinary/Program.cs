//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace Test.BinaryCallingOtherBinary
{
    class Program
    {
        static void Main(string[] args)
        {            
            if (args.Length != 1)
            {
                Console.WriteLine("You have to specify exactly one argument for the filename of the binary that has to be executed");
                Environment.Exit(1);
            }
            else
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        WorkingDirectory = @"C:\Temp",
                        FileName = args[0],
                        Arguments = ""
                    },
                    EnableRaisingEvents = true
                };
                bool started=false;
                try
                {
                    started = process.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Environment.Exit(2);
                }
                if (!started)
                {
                    Console.WriteLine("The specified executable could not be found in the path");
                    Environment.Exit(2);
                }
                Console.WriteLine("starting process with path={0}",process.StartInfo.EnvironmentVariables["PATH"]);
                process.WaitForExit();
                Console.WriteLine("Writing File");
                Trace.TraceInformation("Trace: Writing File");
                File.WriteAllText("BinaryCallBinary.txt", "successful");
            }
        }
    }
}
