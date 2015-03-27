//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EMIC.Cloud.ApplicationRepository;
using Microsoft.EMIC.Cloud.DataManagement;
using Microsoft.EMIC.Cloud.UserAdministration;

namespace Microsoft.EMIC.Cloud.GenericWorker
{
    /// <summary>
    /// Builds the commandline with the executable and all its arguments
    /// </summary>
    public static class CommandLineBuilder
    {
        /// <summary>
        /// Determines whether all mandatory arguments are set through the specified argument collection
        /// </summary>
        /// <param name="requestArgs">The argument collection.</param>
        /// <param name="description">The application describtion.</param>
        /// <param name="errorDescription">The resulting error describtion.</param>
        public static bool HasAllMandatoryArguments(ArgumentCollection requestArgs, VENUSApplicationDescription description, out string errorDescription)
        {
            #region Request might lack mandatory arguments

            if (description.CommandTemplate != null)
            {
                foreach (var descriptionArg in description.CommandTemplate.Args)
                {
                    if (descriptionArg.Required != Required.Mandatory || requestArgs.Contains(descriptionArg)) 
                    {
                        continue;
                    }

                    errorDescription = string.Format("Argument {0} is mandatory, but does not show up in the request. ", descriptionArg.Name);
                    return false;
                }
            }

            #endregion

            #region Request might contain arguments which are not recognized by VENUSApplicationDescription

            foreach (var requestArg in requestArgs)
            {
                if (description.CommandTemplate.Args.Where(a => string.Equals(a.Name, requestArg.Name)).Count() == 0)
                {
                    errorDescription = string.Format("The request contains the argument {0} which is not recognized. ", requestArg.Name);
                    return false;
                }
            }

            #endregion

            errorDescription = string.Empty;
            return true;
        }

        /// <summary>
        /// Downloads the files and build command line.
        /// </summary>
        /// <param name="job">The job describtion.</param>
        /// <param name="applicationDescription">The application description.</param>
        /// <param name="userProfile">The user profile.</param>
        /// <param name="filesystemMapper">The filesystem mapper.</param>
        /// <returns></returns>
        public static CommandLineBuilderResult DownloadFilesAndBuildCommandLine(
            IJob job,
            VENUSApplicationDescription applicationDescription, 
            ProfileData userProfile, 
            FilesystemMapper filesystemMapper)
        {
            if (job == null) throw new ArgumentNullException("job");
            if (applicationDescription == null) throw new ArgumentNullException("applicationDescription");
            
            var result = new CommandLineBuilderResult()
            {
            	Success = false,
                ErrorDescription = "Not properly constructed", 
                CommandLineArgs = string.Empty, 
                Executable = string.Empty, 
                WorkingDirectory = string.Empty
            };

            // Implicit assumption: All files from one instance show up in the same folder...
            var jobFolder = new DirectoryInfo(job.GetJobFolder(userProfile));
            if (!jobFolder.Exists)
            {
                jobFolder.Create();
            }

            string errorDescription = string.Empty;
            var jobDescription = job.GetVENUSJobDescription();
            ArgumentCollection jobArgs = jobDescription.JobArgs;
            if (!HasAllMandatoryArguments(jobArgs, applicationDescription, out errorDescription))
            {
                result.ErrorDescription = errorDescription;
                return result;
            }

            #region Add the executable to the command line

            var appfolder = filesystemMapper.GetApplicationFolder(job.Owner, applicationDescription);
            result.Executable = new FileInfo(Path.Combine(appfolder, applicationDescription.CommandTemplate.Executable)).FullName;
            if (!File.Exists(result.Executable))
            {
                result.ErrorDescription = string.Format("Executable \"{0}\" is not installed", result.Executable);
                return result;
            }

            #endregion

            // maps a "INPUT" parameter name to the local file location
            var locationByName = new Dictionary<string, string>();
 
            #region Determine where each data set should be stored, and mark for download

            foreach (var descriptionArg in applicationDescription.CommandTemplate.Args)
            {
                IArgument jobArg = jobArgs[descriptionArg.Name];
                if (jobArg == null)
                {
                    if (descriptionArg.Required == Required.Mandatory)
                    {
                        result.ErrorDescription = string.Format("Missing mandatory argument {0}", descriptionArg.Name);
                        return result;
                    }
                    continue;
                }
                var jobMultiReferenceArg = jobArg as ReferenceArray;
                if (jobMultiReferenceArg != null)
                {
                    foreach (var singleRefArg in jobMultiReferenceArg.GetSingleReferenceList())
                    {
                        var fileLocation = singleRefArg.GetFileLocation(jobFolder.FullName);
                        locationByName.Add(singleRefArg.Name, fileLocation);
                    }
                    continue;
                }
                var jobReferenceArg = jobArg as IReferenceArgument;
                if (jobReferenceArg != null)
                {
                    var fileLocation = jobReferenceArg.GetFileLocation(jobFolder.FullName);
                    locationByName.Add(descriptionArg.Name, fileLocation);
                }
            }

            #endregion

            #region Download data sets

            var sb = new StringBuilder();
            var downloadArguments = new List<IReferenceArgument>();

            foreach (IArgument argument in jobArgs)
            {
                var jobMultiReferenceArg = argument as ReferenceArray;
                if (jobMultiReferenceArg != null && jobMultiReferenceArg.IsInput(applicationDescription))
                {
                    downloadArguments.AddRange(jobMultiReferenceArg.GetSingleReferenceList());
                    continue;
                }

                var refArgument = argument as IReferenceArgument;
                if (refArgument == null) continue;
                if (refArgument.IsInput(applicationDescription))
                {
                    downloadArguments.Add(refArgument);
                }
            }

            var tokenSource = new CancellationTokenSource();
                downloadArguments.AddRange(jobDescription.Downloads.GetSingleReferenceList("download"));
                ParallelOptions pop = new ParallelOptions();
                pop.MaxDegreeOfParallelism = 30;
                Parallel.ForEach(downloadArguments,pop, task => task.Download(jobFolder.FullName, tokenSource));
            long transferredBytes = 0;
            downloadArguments.ForEach((r) =>
            {
                var filePath = r.GetFileLocation(jobFolder.FullName);
                if (File.Exists(filePath))
                {
                    using (var fs = File.OpenRead(filePath))
                    {
                        transferredBytes += fs.Length;
                    };
                }
            });

            if (tokenSource.IsCancellationRequested)
            {
                locationByName.Values.ToList().ForEach(file =>
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                });

                result.ErrorDescription = "Could not fetch all files:\n " + sb.ToString();
                result.Success = false;
                return result;
            }
            result.TransferredBytes = transferredBytes;
            result.numberOfFilesTransfered = downloadArguments.Count;

            #endregion

            result.WorkingDirectory = job.GetJobFolder(userProfile);

            var arguments = new StringBuilder();

            foreach (var arg in applicationDescription.CommandTemplate.Args)
            {
                switch (arg.CommandLineArgType)
                {
                    case CommandLineArgType.SingleLiteralArgument:
                        {
                            var requestedArgument = jobArgs[arg.Name] as LiteralArgument;
                            if (requestedArgument != null)
                            {
                                string formatString = string.Format(" {0} ", arg.FormatString);
                                string segment = string.Format(formatString, requestedArgument.LiteralValue);
                                arguments.Append(segment);
                            }
                        }
                        break;
                    case CommandLineArgType.Switch:
                        {
                            var requestedArgument = jobArgs[arg.Name] as SwitchArgument;
                            if (requestedArgument != null && requestedArgument.Value)
                            {
                                arguments.Append(string.Format(" {0} ", arg.FormatString));
                            }
                        }
                        break;
                    case CommandLineArgType.SingleReferenceInputArgument:
                    case CommandLineArgType.SingleReferenceOutputArgument:
                        {
                            if (jobArgs[arg.Name] != null)
                            {
                                string formatString = string.Format(" {0} ", arg.FormatString);
                                var location = locationByName[arg.Name];
                                string segment = string.Format(formatString, location);
                                arguments.Append(segment);
                            }
                        }
                        break;
                    case CommandLineArgType.MultipleReferenceInputArgument:
                    case CommandLineArgType.MultipleReferenceOutputArgument:
                        {
                            string formatString = string.Format(" {0} ", arg.FormatString);
                            var refArray = jobArgs[arg.Name] as ReferenceArray;
                            if (refArray != null)
                            {
                                for (int i = 0; i < refArray.References.Count; i++)
                                {
                                    var location = locationByName[String.Format("{0}{1}", arg.Name, i)];
                                    string segment = string.Format(formatString, location);
                                    arguments.Append(segment);
                                }
                            }
                        }
                        break;
                    default:
                        throw new NotImplementedException(ExceptionMessages.UnhandledArgType);
                }
            }

            result.ErrorDescription = string.Empty;
            result.CommandLineArgs = arguments.ToString();
            result.Success = true;
            
            return result;
        }
    }
}