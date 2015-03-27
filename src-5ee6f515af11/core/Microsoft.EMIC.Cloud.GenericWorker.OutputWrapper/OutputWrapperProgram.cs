//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Microsoft.EMIC.Cloud.GenericWorker.OutputWrapper
{
    /// <summary>
    /// This application is a wrapper around a 2nd application. When starting a 
    /// process under a different Windows account, it is not possible to redirect
    /// stdout/stderr of the original Process instance. Therefore, the caller
    /// wraps the actual binary with a call to the OutputCatcher, where the caller
    /// indicates which files are used to collect stdout/stderr. 
    /// </summary>
    class OutputWrapperProgram
    {
        static void Main(string[] args)
        {
            var stdoutFile = args[0];
            var stderrile = args[1];
            var application = args[2];
            var argsStr = Encoding.UTF8.GetString(Convert.FromBase64String(args[3]));
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = application,
                    Arguments = argsStr,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                }
            };
            process.OutputDataReceived += (s, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
            process.ErrorDataReceived += (s, e) => { if (e.Data != null) stderr.AppendLine(e.Data);  };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            File.WriteAllText(stdoutFile, stdout.ToString());
            File.WriteAllText(stderrile, stderr.ToString());
        }
    }
}
