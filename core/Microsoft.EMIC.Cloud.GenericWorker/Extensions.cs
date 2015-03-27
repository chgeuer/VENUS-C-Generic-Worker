//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using Microsoft.EMIC.Cloud.UserAdministration;
using Microsoft.EMIC.Cloud.DataManagement;
using Microsoft.EMIC.Cloud.GenericWorker;
using Microsoft.EMIC.Cloud.ApplicationRepository;

// ReSharper disable CheckNamespace
/// <summary>
/// Microsoft Generic Worker Extensions class
/// </summary>
public static class MicrosoftGenericWorkerExtensions
// ReSharper restore CheckNamespace
{
    #region System.String extensions

    /// <summary>
    /// Converts the input string to a secure string.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static SecureString ToSecureString(this string value)
    {
        var secureString = new SecureString();
        value.ToList().ForEach(secureString.AppendChar);
        return secureString;
    }

    /// <summary>
    /// Creates a substring from the given string that meets the size when encoded by the
    /// provided encoding. The substring is from the end of the given string, the beginning
    /// is cut off.
    /// </summary>
    /// <param name="value">The original string</param>
    /// <param name="sizeInBytes">Size in bytes for the returned string when using the given encoding</param>
    /// <param name="encoding">The encoding to be used for calculating the size of a string</param>
    /// <returns>A substring of the given string</returns>
    public static string limitSizeInBytes(this string value, int sizeInBytes, Encoding encoding)
    {
        if (encoding.GetByteCount(value) > sizeInBytes)
        {
            char[] tmpArray;
            if (value.Length - sizeInBytes - 10 < 0)
            {
                tmpArray = value.ToCharArray();
            }
            else
            {
                tmpArray = value.ToCharArray(value.Length - sizeInBytes - 10, sizeInBytes + 10);
            }

            var position = tmpArray.Length - 1;
            var counter = 0;
            var tmp = encoding.GetByteCount(tmpArray[position].ToString());
            while (counter + tmp <= sizeInBytes)
            {
                counter += tmp;
                position--;
                tmp = encoding.GetByteCount(tmpArray[position].ToString());
            }

            return new string(tmpArray, position + 1, tmpArray.Length - position - 1);
        }
        else
        {
            return value;
        }
    }

    /// <summary>
    /// Creates a substring from the given string that meets the size when encoded with UTF-8.
    /// The substring is from the end of the given string, the beginning is cut off.
    /// </summary>
    /// <param name="value">The original string</param>
    /// <param name="sizeInBytes">Size in bytes for the returned string when encoded in UTF-8</param>
    /// <returns>A substring of the given string</returns>
    public static string limitSizeInBytesUTF16(this string value, int sizeInBytes)
    {
        return limitSizeInBytes(value, sizeInBytes, Encoding.BigEndianUnicode);
    }

    #endregion

    #region System.Exception extensions

    /// <summary>
    /// Gets the full details concatenated.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns></returns>
    public static string GetFullDetailsConcatenated(this Exception exception)
    {
        return exception.GetFullDetail(
            e => String.Format("{0}: \"{1}\"", e.GetType().FullName, e.Message),
            e => e.StackTrace);
    }

    /// <summary>
    /// Gets the full detail.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <param name="selectors">The selectors.</param>
    /// <returns></returns>
    public static string GetFullDetail(this Exception exception, params Func<Exception, string>[] selectors)
    {
        var result = new StringBuilder();

        if (selectors == null)
        {
            selectors = new Func<Exception, string>[] { e => e.Message };
        }

        result.AppendLine("---------------------------------");
        for (Exception e = exception; e != null; e = e.InnerException)
        {
            foreach (var selector in selectors)
            {
                result.AppendLine(selector(e));
                result.AppendLine("---------------------------------");
            }
        }

        return result.ToString();
    }

    #endregion

    #region System.Diagnostics.Process extensions

    /// <summary>
    /// This function is needed to find unique name of the process
    /// </summary>
    /// <param name="pid">Unique id of the process</param>
    /// <returns></returns>
    private static string FindIndexedProcessName(int pid)
    {
        var processName = Process.GetProcessById(pid).ProcessName;
        var processesByName = Process.GetProcessesByName(processName);
        string processIndexdName = null;

        for (var index = 0; index < processesByName.Length; index++)
        {
            processIndexdName = index == 0 ? processName : processName + "#" + index;
            var processId = new PerformanceCounter("Process", "ID Process", processIndexdName);
            if ((int)processId.NextValue() == pid)
            {
                return processIndexdName;
            }
        }

        return processIndexdName;
    }

    /// <summary>
    /// Finds the name of the pid from indexed process.
    /// </summary>
    /// <param name="indexedProcessName">Name of the indexed process.</param>
    /// <returns></returns>
    private static Process FindPidFromIndexedProcessName(string indexedProcessName)
    {
        var parentId = new PerformanceCounter("Process", "Creating Process ID", indexedProcessName);
        return Process.GetProcessById((int)parentId.NextValue());
    }

    /// <summary>
    /// Parents the specified process.
    /// </summary>
    /// <param name="process">The process.</param>
    /// <returns></returns>
    public static Process Parent(this Process process)
    {
        return FindPidFromIndexedProcessName(FindIndexedProcessName(process.Id));
    }

    #endregion

    #region IArgument extensions

    /// <summary>
    /// Determines whether the specified argument is output.
    /// </summary>
    /// <param name="argument">The argument.</param>
    /// <param name="applicationDescription">The application description.</param>
    /// <returns>
    ///   <c>true</c> if the specified argument is output; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsOutput(this IArgument argument, VENUSApplicationDescription applicationDescription)
    {
        if (applicationDescription == null || applicationDescription.CommandTemplate == null)
            new ArgumentNullException("applicationDescription");

        return applicationDescription.CommandTemplate.Args.Any(cla => cla.Name == argument.Name
                            && (cla.CommandLineArgType == CommandLineArgType.SingleReferenceOutputArgument
                            || cla.CommandLineArgType == CommandLineArgType.MultipleReferenceOutputArgument
                            || cla.CommandLineArgType == CommandLineArgType.CatalogueReferenceOutputArgument));
    }

    /// <summary>
    /// Determines whether the specified argument is input.
    /// </summary>
    /// <param name="argument">The argument.</param>
    /// <param name="applicationDescription">The application description.</param>
    /// <returns>
    ///   <c>true</c> if the specified argument is input; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsInput(this IArgument argument, VENUSApplicationDescription applicationDescription)
    {
        if (applicationDescription == null || applicationDescription.CommandTemplate == null)
            new ArgumentNullException("applicationDescription");

        return applicationDescription.CommandTemplate.Args.Any(cla => cla.Name == argument.Name
                            && (cla.CommandLineArgType == CommandLineArgType.SingleReferenceInputArgument
                            || cla.CommandLineArgType == CommandLineArgType.MultipleReferenceInputArgument
                            || cla.CommandLineArgType == CommandLineArgType.CatalogueReferenceInputArgument));
    }

    #endregion

    #region IJob extensions

    /// <summary>
    /// Gets the job folder.
    /// </summary>
    /// <param name="job">The job.</param>
    /// <param name="userProfile">The user profile.</param>
    /// <returns></returns>
    public static string GetJobFolder(this IJob job, ProfileData userProfile)
    {
        return Path.Combine(userProfile.HomeDirectory, job.InternalJobID);
    }
    
    /// <summary>
    /// Determines whether all required input data for the given job is available. 
    /// </summary>
    /// <param name="iJob">The job.</param>
    /// <param name="appDescription">The application description</param>
    /// <returns></returns>
    public static bool Ready(this IJob iJob, VENUSApplicationDescription appDescription)
    {
        //
        // The LINQ statement below is short to write, but must check all 
        // IDataReferences. The longer implementation hops off as soon as 
        // there is missing data. 
        // 
        // return iJob.GetVENUSJobDescription().JobArgs.OfType<IReferenceArgument>()
        //    .Where(ra => ra.IsDownload && !ra.ExistsDataItem).Count() == 0;       
        try
        {
            var jobDesc = iJob.GetVENUSJobDescription();
            if (!jobDesc.Downloads.All(dla => dla.ExistsDataItem()))
                return false;
            var jobArgs = jobDesc.JobArgs;
            return jobArgs.OfType<IReferenceArgument>().All(
                    ra => appDescription.CommandTemplate.Args.FirstOrDefault(cla => ra.Name == cla.Name && (
                        cla.CommandLineArgType == CommandLineArgType.Switch
                        || cla.CommandLineArgType == CommandLineArgType.SingleLiteralArgument
                        || cla.CommandLineArgType == CommandLineArgType.SingleReferenceOutputArgument
                        || cla.CommandLineArgType == CommandLineArgType.MultipleReferenceOutputArgument
                        || cla.CommandLineArgType == CommandLineArgType.CatalogueReferenceOutputArgument
                        || ra.ExistsDataItem())) != null);
        }
        catch (Exception ex)
        {
            Trace.TraceError(String.Format(
                "Exception while checking IJob.Ready() status: \r\n{0}",
                ex.GetFullDetailsConcatenated()));

            return false;
        }
    }

    /// <summary>
    /// Gets the previous status for cancel requested job.
    /// </summary>
    /// <param name="job">The job.</param>
    /// <returns></returns>
    public static JobStatus GetPreviousStatusForCancelRequestedJob(this IJob job)
    {
        if (job.StatusText.Length == 0)
        {
            return JobStatus.Submitted;
        }
            
        return JobStatus.Running;
    }

    #endregion

    #region JobStatus extensions

    /// <summary>
    /// Determines whether the specified job status has ended.
    /// </summary>
    /// <param name="jobStatus">The job status.</param>
    /// <returns>
    ///   <c>true</c> if the specified job status has ended; otherwise, <c>false</c>.
    /// </returns>
    public static bool HasEnded(this JobStatus jobStatus)
    {
        return jobStatus == JobStatus.Finished ||
               jobStatus == JobStatus.Cancelled ||
               jobStatus == JobStatus.Failed;
    }

    #endregion

    #region System.IO.Stream extensions

    /// <summary>
    /// Pumps the specified input.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="output">The output.</param>
    public static void Pump(this Stream input, Stream output)
    {
        var bytes = new byte[4096];
        int n;
        while ((n = input.Read(bytes, 0, bytes.Length)) != 0)
        {
            output.Write(bytes, 0, n);
        }
    }

    #endregion
}
