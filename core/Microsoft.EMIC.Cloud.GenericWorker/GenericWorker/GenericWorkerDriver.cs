//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Microsoft.EMIC.Cloud.GenericWorker
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Ionic.Zip;
    using Microsoft.EMIC.Cloud.ApplicationRepository;
    using Microsoft.EMIC.Cloud.DataManagement;
    using Microsoft.EMIC.Cloud.Security;
    using Microsoft.EMIC.Cloud.UserAdministration;
    using System.Runtime.Serialization;
    using Microsoft.EMIC.Cloud.Notification;
    using System.Security.Principal;
    using System.Xml.Serialization;
    
    /// <summary>
    /// The core business logic of the generic worker. 
    /// </summary>
    [Export(typeof(GenericWorkerDriver))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class GenericWorkerDriver : IPartImportsSatisfiedNotification
    {
        /// <summary>
        /// Constant string used as prefix in the customerJobID field of hieararchical jobs
        /// </summary>
        public const string ENVIRONMENT_VARIABLE_JOBID = "jobId";

        /// <summary>
        /// Job submission end point used for submitting jobs locally
        /// </summary>
        public const string ENVIRONMENT_VARIABLE_JOBSUBMISSIONENDPOINT = "jobSubmissionEndpoint";

        /// <summary>
        /// Path information provided to the process started by the GW
        /// </summary>
        public const string ENVIRONMENT_VARIABLE_PATH = "PATH";

        /// <summary>
        /// Constant string prefix to distuingish process information from other logs
        /// </summary>
        private const string PROGRESS_TAG_START = "<GW_ProgressValue>";

        /// <summary>
        /// Constant string suffix to distuingish process information from other logs
        /// </summary>
        private const string PROGRESS_TAG_END = "</GW_ProgressValue>";

        /// <summary>
        /// Gets or sets the filesystem mapper.
        /// </summary>
        [Import(typeof(FilesystemMapper), RequiredCreationPolicy = CreationPolicy.NonShared)]
        public FilesystemMapper FilesystemMapper { get; set; }

        /// <summary>
        /// Gets or sets the runtime environment.
        /// </summary>
        [Import(typeof(IGWRuntimeEnvironment))] //, RequiredCreationPolicy = CreationPolicy.NonShared)]
        public IGWRuntimeEnvironment RuntimeEnvironment { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="GenericWorkerEventDispatcher"/> of the Generic Worker
        /// </summary>
        [Import]
        public GenericWorkerEventDispatcher GWEventDispatcher { get; set; }

        /// <summary>
        /// User identity in which the process works under is saved into the file. Gets or sets the name and path of the file
        /// </summary>
        [Import(CompositionIdentifiers.ProcessIdentityFilename)]
        public string ProcessIdentityFilename { get; set; }

        /// <summary>
        /// The <see cref="ArgumentRepository"/> to de-serialize <see cref="IArgument"/> objects.
        /// </summary>
        [Import]
        public ArgumentRepository ArgumentRepository { get; set; }

        /// <summary>
        /// Job submission end point used for submitting jobs locally
        /// </summary>
        [Import(CompositionIdentifiers.LocalJobSummissionEndPoint, RequiredCreationPolicy = CreationPolicy.NonShared)]
        public string LocalJobSubmissionEndPoint { get; set; }

        /// <summary>
        /// The unique identity of the VM
        /// </summary>
        [Import(CompositionIdentifiers.InstanceIdentity, RequiredCreationPolicy = CreationPolicy.NonShared)]
        public string InstanceIdentity { get; set; }


        /// <summary>
        /// Called when a part's imports have been satisfied and it is safe to use.
        /// </summary>
        void IPartImportsSatisfiedNotification.OnImportsSatisfied()
        {
            CreateProfileClient.CreateClientIdentityFileForService(ProcessIdentityFilename);
        }

       /// <summary>
       /// The main message pump of the generic worker. 
       /// </summary>
        /// <param name="ct">A <see cref="CancellationToken" /> to cancel the generic worker. </param>
        public void Run(CancellationToken ct)
        {
            int currentProgressValue = 0;
            //string envJobIdKey = string.Format("{0}_{1}", ENVIRONMENT_VARIABLE_JOBID, this.InstanceIdentity);
            string envJobIdKey = ENVIRONMENT_VARIABLE_JOBID;
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    #region Main loop

                    #region Claim job

                    // We sleep once before the polling, so that multiple workers starting at the same time don't collide. 
                    const int minSleepTimeMilliSeconds = 2500;
                    const int maxSleepTimeMilliSeconds = 6500;
                    Thread.Sleep(TimeSpan.FromMilliseconds(new Random().Next(minSleepTimeMilliSeconds, maxSleepTimeMilliSeconds)));

                    IJob job = null;
                    if (!this.RuntimeEnvironment.TryDequeueJob(out job))
                    {                        
                        continue;
                    }

                    #region Logging

                    var statusText = new StringBuilder();
                    var stdout = new StringBuilder();
                    var stderr = new StringBuilder();

                    Action<string> log = msg =>
                    {
                        // send to diagnostic traces
                        Trace.TraceInformation(string.Format("{0} {1} ",
                            this.InstanceIdentity, msg));

                        // send to status visible to researcher
                        lock (statusText)
                        {
                            statusText.AppendLine(msg);
                        }
                    };

                    var grabJobStart = DateTime.UtcNow;

                    Action<string> logJob = msg => log(string.Format(string.Format("{0} {1}{2}",
                        job.InternalJobID, msg, Environment.NewLine)));

                    #endregion

                    VENUSJobDescription jobDescription = job.GetVENUSJobDescription();

                    #region Claim job

                    if (!this.RuntimeEnvironment.MarkJobAsChekingInputData(job, this.InstanceIdentity))
                    {
                        // could not acquire job
                        continue;
                    }

                    logJob(string.Format("Fetching job took {0}", DateTime.UtcNow.Subtract(grabJobStart)));

                    #endregion

                    VENUSApplicationDescription appDescription = null;
                    if (jobDescription.AppDescReference == null || jobDescription.AppPkgReference== null || !jobDescription.AppDescReference.ExistsDataItem() || !jobDescription.AppPkgReference.ExistsDataItem() )
                    {
                        Trace.TraceInformation("Cannot run job {0} because application {1} is not in the repository or not referenced in jobdescription",
                            job.InternalJobID, job.ApplicationIdentificationURI);

                        var failureCount = 0;
                        const int maxRetries = 50;
                        while (!this.RuntimeEnvironment.MarkJobAsFailed(job, "Application is not uploaded or not correctly referenced in the job description with AppDescReference and AppPkgReference"))
                        {
                            if (failureCount++ > maxRetries)
                            {
                                throw new Exception(string.Format(ExceptionMessages.CanNotSetStatus,
                                    typeof(JobStatus).Name, JobStatus.Failed, maxRetries));
                            }
                            Thread.Sleep(TimeSpan.FromSeconds(1));
                        }

                        continue;
                    }

                    Action<string> resubmitJob = (errorMessage) =>
                    {
                        var failureCount = 0;
                        const int maxRetries = 50;
                        while (!this.RuntimeEnvironment.MarkJobAsSubmittedBack(job, errorMessage))
                        {
                            if (failureCount++ > maxRetries)
                            {
                                throw new Exception(string.Format(ExceptionMessages.CanNotResetStatus,
                                    typeof(JobStatus).Name, JobStatus.Submitted, maxRetries));
                            }
                            Thread.Sleep(TimeSpan.FromSeconds(1));
                        }

                        logJob("Resubmitting: " + errorMessage);
                    };

                    #region loading appDescription //Todo: dispose memstream (using)
                    var appDescriptionBits = new MemoryStream(jobDescription.AppDescReference.DownloadContents());
                    DataContractSerializer dcs = new DataContractSerializer(typeof(VENUSApplicationDescription));
                    appDescription = dcs.ReadObject(appDescriptionBits) as VENUSApplicationDescription;
                    if (appDescription == null)
                    {
                        var errorMessage = "appDescription == null";
                        Trace.TraceError(errorMessage);
                        resubmitJob(errorMessage);
                        continue;
                    }
                    #endregion

                    
                    #region Check availability of input data
                    if (!job.Ready(appDescription))
                    {
                        resubmitJob("Input data not available");

                        continue;
                    }
                    #endregion

                    #region Install application
                    try
                    {
                        logJob("Installing application as User: " + WindowsIdentity.GetCurrent().Name);
                        
                        this.FilesystemMapper.InstallApplicationIfNeeded(job.Owner, jobDescription, appDescription);
                        logJob("Application installed");
                    }
                    catch (Exception ex)
                    {
                        var errorMessage = string.Format("Installing application threw {0}: {1}", ex.GetType().Name, ex.Message);

                        Trace.TraceError(errorMessage);
                        resubmitJob(errorMessage);
                        continue;
                    }

                    #endregion

                    logJob("Started execution");
                    this.RuntimeEnvironment.UpdateJobOutputs(job, stdout, stderr, statusText);
                    if (!this.RuntimeEnvironment.MarkJobAsRunning(job, statusText.ToString(), stdout.ToString(), stderr.ToString()))
                    {
                        logJob("Job is being cancelled during cheking input data");
                        continue;
                    }
                    logJob("Running");

                    #endregion

                    try
                    {
                        #region Create User

                        ProfileData userProfile = CreateProfileClient.GetLocalUser(job.Owner,
                                this.FilesystemMapper.GenericWorkerDirectoryUserFolder);

                        logJob("User created: " + userProfile.UserName);

                        #endregion

                        #region Download data

                        DateTime downloadStart = DateTime.UtcNow;
                        
                        CommandLineBuilderResult commandLineBuilderResult = null;
                        //using (new Impersonator(userProfile.Domain, userProfile.UserName, userProfile.Password, impersonateImmediately: true))
                        //{
                            commandLineBuilderResult = CommandLineBuilder.DownloadFilesAndBuildCommandLine(
                                job, appDescription, userProfile, this.FilesystemMapper);

                            logJob("Download took " + DateTime.UtcNow.Subtract(downloadStart));

                            // Before starting the application, let's see if there is a input zip file
                            String inputZipFileName = jobDescription.InputZipFilename;
                            if (!String.IsNullOrEmpty(inputZipFileName))
                            {
                                var jobFolder = job.GetJobFolder(userProfile);
                                var inputZipFullFileName = Path.Combine(jobFolder, inputZipFileName);

                                // A input zip has been specified, but is it really there?
                                if (File.Exists(inputZipFullFileName))
                                {
                                    // Before extracting, we must be sure that the zip file is valid.
                                    if (ZipFile.IsZipFile(inputZipFullFileName, true))
                                    {
                                        try
                                        {
                                            using (ZipFile zip = new ZipFile(inputZipFullFileName))
                                            {
                                                zip.ExtractAll(jobFolder, ExtractExistingFileAction.OverwriteSilently);
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            logJob(String.Format("During extraction of zip file the following error occured: {0})", e.Message));
                                        }
                                    }
                                    else
                                    {
                                        logJob("An error occured during validation of input zip file.");
                                    }
                                }
                                else
                                {
                                    logJob("An input zip file has been specified but it is not available in the working directory.");
                                }
                            }
                        //}
                        
                        
                        logJob("The command line is ");
                        logJob(commandLineBuilderResult.ToString());



                        if (!commandLineBuilderResult.Success)
                        {
                            stderr.AppendLine(commandLineBuilderResult.ErrorDescription);

                            logJob("!commandLineBuilderResult.Success: " + commandLineBuilderResult.ErrorDescription);
                            this.RuntimeEnvironment.UpdateJobOutputs(job, stdout, stderr, statusText);
                            this.RuntimeEnvironment.MarkJobAsFailed(job, statusText.ToString(), stdout.ToString(), stderr.ToString());
                            continue;
                        }
                        GWEventDispatcher.Notify(new StorageEvent(JobStatus.CheckingInputData, JobStatus.Running, true, 
                            commandLineBuilderResult.numberOfFilesTransfered, commandLineBuilderResult.TransferredBytes, job, DateTime.Now.ToUniversalTime()));

                        #endregion

                        #region Execute job

                        var process = new Process
                        {
                            StartInfo = new ProcessStartInfo()
                            {
                                WorkingDirectory = commandLineBuilderResult.WorkingDirectory,
                                FileName = commandLineBuilderResult.Executable,
                                Arguments = commandLineBuilderResult.CommandLineArgs,
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true
                            },
                            EnableRaisingEvents = true
                        };

                        //give the necessary information to the process via environment variables
                        process.StartInfo.EnvironmentVariables.Add(envJobIdKey, job.CustomerJobID);
                        process.StartInfo.EnvironmentVariables.Add(ENVIRONMENT_VARIABLE_JOBSUBMISSIONENDPOINT, LocalJobSubmissionEndPoint);
                        //add own path to the path environment variable 

                        Action<string> AddToPath = (path) =>
                        {
                            if (!path.Contains("..")) //we do not want that user add paths outside of the application directory
                            {
                                var processPaths = process.StartInfo.EnvironmentVariables[ENVIRONMENT_VARIABLE_PATH];
                                processPaths = processPaths + ";" + path;
                                process.StartInfo.EnvironmentVariables[ENVIRONMENT_VARIABLE_PATH] = processPaths;
                            }

                        };
                        var myDir = Directory.GetParent(commandLineBuilderResult.Executable).FullName;
                        //add the applications ApplicationDir to the path
                        AddToPath(myDir);                        
                        //add all paths described in the appdescription to the path
                        if (appDescription.CommandTemplate.Path != null)
                        {
                            appDescription.CommandTemplate.Path.Split(';').ToList().ForEach(p => AddToPath(Path.Combine(myDir, p)));
                        }

                        //TODO: Impersonation should be checked or this code should be cleaned

                        process.OutputDataReceived += (s, a) => {
                            int newProgressValue = -1;
                            string output;
                            lock (stdout) 
                            { 
                                stdout.AppendLine(a.Data);
                                output=stdout.ToString();
                            }
                            newProgressValue = getProgressValue(output, PROGRESS_TAG_START, PROGRESS_TAG_END);
                            if (newProgressValue != -1 && newProgressValue > currentProgressValue)
                            {
                                currentProgressValue = newProgressValue;
                                GWEventDispatcher.Notify(new ProgressEvent(currentProgressValue, job, DateTime.UtcNow));
                            }
                        };
                        process.ErrorDataReceived += (s, a) => { lock (stderr) { stderr.AppendLine(a.Data); } };

                        bool IsProcessKilled = false;
                        Action checkForCancellationRequestAndKillJob = () =>
                        {
                            logJob(string.Format("{0}: Running", DateTime.UtcNow));

                            var rte = this.RuntimeEnvironment;
                            //Todo: lock outp vars

                            rte.UpdateJobOutputs(job, stdout, stderr, statusText);

                            if(!rte.MarkJobAsRunning(job, statusText.ToString(), stdout.ToString(), stderr.ToString()))
                            {
                                var status = rte.GetJobStatus(job.InternalJobID);
                                if (status.HasValue && status.Value == JobStatus.CancelRequested) 
                                {
                                    IsProcessKilled = true;
                                    KillProcessTree(process);
                                    while (!rte.MarkJobAsCancelled(job, statusText.ToString(), stdout.ToString(), stderr.ToString()))
                                    {
                                        Trace.TraceError(string.Format("Cannot mark {0} as cancelled, retrying", job.InternalJobID));
                                        Thread.Sleep(100);
                                    }
                                    GWEventDispatcher.Notify(new FinishEvent(job, DateTime.Now.ToUniversalTime()));
                                }
                            }                            
                        };

                        var timerCancelSrc = new CancellationTokenSource();
                        GenericWorkerTimer.Run(
                            () => TimeSpan.FromSeconds(20),
                            checkForCancellationRequestAndKillJob,
                            timerCancelSrc.Token);

                        logJob(string.Format("process.StartInfo.WorkingDirectory {0}", process.StartInfo.WorkingDirectory));
                        logJob(string.Format("process.StartInfo.FileName {0}", process.StartInfo.FileName));
                        logJob(string.Format("process.StartInfo.Arguments {0}", process.StartInfo.Arguments));
                        logJob(string.Format("process.StartInfo.LoadUserProfile {0}", process.StartInfo.LoadUserProfile));
                        logJob(string.Format("process.StartInfo.UserName {0}", process.StartInfo.UserName));
                        logJob(string.Format("process.StartInfo.Domain {0}", process.StartInfo.Domain));
                        logJob(string.Format("process.StartInfo.Password {0}", userProfile.Password));
                        logJob(string.Format("process.StartInfo.RedirectStandardError {0}", process.StartInfo.RedirectStandardError));
                        logJob(string.Format("process.StartInfo.RedirectStandardInput {0}", process.StartInfo.RedirectStandardInput));
                        logJob(string.Format("process.StartInfo.RedirectStandardOutput {0}", process.StartInfo.RedirectStandardOutput));
                        logJob(string.Format("process.StartInfo.UseShellExecute {0}", process.StartInfo.UseShellExecute));
                        logJob(string.Format("process.StartInfo.Verb {0}", process.StartInfo.Verb));
                        logJob(string.Format("process.StartInfo.WindowStyle {0}", process.StartInfo.WindowStyle.ToString()));
                        logJob(string.Format("process.EnableRaisingEvents {0}", process.EnableRaisingEvents));
                        logJob(string.Format("process.StartInfo.EnvironmentVariables[\"jobId\"] {0}", process.StartInfo.EnvironmentVariables[envJobIdKey]));
                        logJob(string.Format("process.StartInfo.EnvironmentVariables[\"jobSubmissionEndpoint\"] {0}", process.StartInfo.EnvironmentVariables[ENVIRONMENT_VARIABLE_JOBSUBMISSIONENDPOINT]));
                        logJob(string.Format("process.StartInfo.EnvironmentVariables[\"PATH\"] {0}", process.StartInfo.EnvironmentVariables[ENVIRONMENT_VARIABLE_PATH]));

                        // Before launching the application, the progressValue has to be resetted
                        currentProgressValue = 0;
                        try
                        {
                            process.Start();
                            process.BeginOutputReadLine();
                            process.BeginErrorReadLine();
                            process.WaitForExit();
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError("Error launching process");
                            for (var e = ex; e != null; e = e.InnerException)
                            {
                                Trace.TraceError(ex.Message);
                                Trace.TraceError(ex.StackTrace);
                            }

                            throw;
                        }
                        timerCancelSrc.Cancel();

                        if (!IsProcessKilled)
                        {
                            logJob("Application ended: " + process.StartInfo.FileName);
                            if (process.ExitCode != 0)
                            {
                                logJob("Job returned a non zero statuscode: " + process.ExitCode);
                                this.RuntimeEnvironment.UpdateJobOutputs(job, stdout, stderr, statusText);
                                this.RuntimeEnvironment.MarkJobAsFailed(job, statusText.ToString(), stdout.ToString(), stderr.ToString());
                            }

                        #endregion

                            #region Upload results
                            if (process.ExitCode == 0)
                            {
                                long transferredBytes = 0;
                                var uploadReferences = new List<IReferenceArgument>();
                                DateTime uploadStart = DateTime.UtcNow;
                                var allOutputFilesAvailable = true;
                                //using (new Impersonator(userProfile.Domain, userProfile.UserName, userProfile.Password, impersonateImmediately: true))
                                //{
                                    var jobFolder = new DirectoryInfo(job.GetJobFolder(userProfile));

                                    var resultsZip = jobDescription.ResultZipFilename;
                                    if (!string.IsNullOrWhiteSpace(resultsZip))
                                    {
                                        //TODO: also check if it is a valid path or better just handle the exception properly
                                        using (var zip = new ZipFile())
                                        {
                                            foreach (var file in jobFolder.EnumerateFiles())
                                            {
                                                if (!file.FullName.EndsWith(".exe") && !file.FullName.EndsWith(".dll"))
                                                    zip.AddFile(file.FullName, "");
                                            }
                                            var pathResultsZip = Path.Combine(jobFolder.FullName, resultsZip);
                                            zip.Save(pathResultsZip);
                                        }
                                    }
                                    foreach (IArgument argument in jobDescription.JobArgs)
                                    {
                                        var jobMultiReferenceArg = argument as ReferenceArray;
                                        if (jobMultiReferenceArg != null && jobMultiReferenceArg.IsOutput(appDescription))
                                        {
                                            uploadReferences.AddRange(jobMultiReferenceArg.GetSingleReferenceList());
                                            continue;
                                        }

                                        var refArgument = argument as IReferenceArgument;
                                        if (refArgument != null)
                                        {
                                            if (argument.IsOutput(appDescription))
                                            {
                                                uploadReferences.Add(refArgument);
                                            }
                                        }
                                    }
                                    uploadReferences.AddRange(jobDescription.Uploads.GetSingleReferenceList("upload"));
                                    ParallelOptions pop = new ParallelOptions();
                                    pop.MaxDegreeOfParallelism = 30;
                                    allOutputFilesAvailable =
                                        uploadReferences.All(r => File.Exists(r.GetFileLocation(jobFolder.FullName)));

                                    Parallel.ForEach(uploadReferences, pop, reference => 
                                    {
                                        // It might be the case, that an application terminates with exit code 0 but
                                        // didn't create all output files. If this happens, the files should be created
                                        // without any content (size = 0 bytes).
                                        string file = reference.GetFileLocation(jobFolder.FullName);
                                        if (!File.Exists(file))
                                        {
                                            if (jobDescription.MissingResultFilePolicy == MissingResultFilePolicy.GenerateZeroFiles)
                                            {
                                                using (File.Create(file)) { };
                                            }
                                            else
                                            {
                                                uploadReferences.Remove(reference);
                                                return;
                                            }                                            
                                        }
                                        reference.Upload(jobFolder.FullName);
                                    });

                                    uploadReferences.ForEach((r) =>
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
                                    logJob("Uploadsize in bytes: " + transferredBytes);

                                //}

                                logJob("Upload took " + DateTime.UtcNow.Subtract(uploadStart));
                                this.RuntimeEnvironment.UpdateJobOutputs(job, stdout, stderr, statusText);
                                if (jobDescription.MissingResultFilePolicy==MissingResultFilePolicy.Standard && !allOutputFilesAvailable)
                                {
                                    logJob("The executable executed without an error code, but did not create all specified result files, hence the job was marked as failed. In case it is an expected behaviour to have missing result files and you want the GW to autogenerate empty result files you have to explicitly specify this in the jobDescription using the MissingResultFilePolicy parameter"); //TODO: improve log message
                                    if (this.RuntimeEnvironment.MarkJobAsFailed(job, statusText.ToString(), stdout.ToString(), stderr.ToString()))
                                    {
                                        GWEventDispatcher.Notify(new StorageEvent(JobStatus.Running, JobStatus.Failed, true,
        uploadReferences.Count, transferredBytes, job, DateTime.Now.ToUniversalTime()));
                                    }
                                    else
                                    {
                                        logJob("Could not update status to failed");
                                    }
                                }
                                else if (!this.RuntimeEnvironment.MarkJobAsFinished(job, statusText.ToString(), stdout.ToString(), stderr.ToString()))
                                {
                                    logJob("Could not update status to  finished");
                                }
                                else
                                {
                                    if (jobDescription.MissingResultFilePolicy == MissingResultFilePolicy.GenerateZeroFiles && !allOutputFilesAvailable)
                                    {
                                        logJob("The executable executed without an error code, but did not create all specified result files. Because of the MissingResultFilePolicy specified in the jobDescription the missing files were generated with a length of zero, to not stop follow-up jobs that depend on this files."); 
                                    }
                                    GWEventDispatcher.Notify(new StorageEvent(JobStatus.Running, JobStatus.Finished, true,
                                        uploadReferences.Count, transferredBytes, job, DateTime.Now.ToUniversalTime()));
                                    logJob("Done");
                                }
                            }

                            GWEventDispatcher.Notify(new FinishEvent(job, DateTime.Now.ToUniversalTime()));
                        }
                        #endregion
                        this.RuntimeEnvironment.UpdateJobOutputs(job, stdout, stderr, statusText);
                        #region cleanup userfolder
                        Directory.Delete(job.GetJobFolder(userProfile), true);

                        #endregion
                    }
                    catch (Exception exceptionWhileRunningJob)
                    {
                        #region Error logging

                        try
                        {
                            var errorText = exceptionWhileRunningJob.GetFullDetailsConcatenated();

                            Trace.TraceError("General failure: " + errorText);
                            stderr.AppendLine(errorText);

                            const int retries = 20;
                            string failureInfo = stderr.ToString();
                            this.RuntimeEnvironment.UpdateJobOutputs(job, stdout, stderr, statusText);
                            for (int i = 0; i < retries; i++)
                            {
                                if (this.RuntimeEnvironment.MarkJobAsFailed(job, failureInfo))
                                {
                                    GWEventDispatcher.Notify(new FinishEvent(job, DateTime.Now.ToUniversalTime()));
                                    break;
                                }

                                Trace.TraceError(string.Format("Could not mark job {0} as failed", job.InternalJobID));
                            }
                        }
                        catch (Exception exceptionDuringCatch)
                        {
                            for (Exception e = exceptionDuringCatch; e != null; e = e.InnerException)
                            {
                                Trace.TraceError(exceptionDuringCatch.GetFullDetailsConcatenated());
                            }
                        }

                        #endregion
                    }

                    #endregion // Main Loop
                }
                catch (Exception unhandledInMainLoopException)
                {
                    Trace.TraceError("Unhandled exception in main loop: " + unhandledInMainLoopException.GetFullDetailsConcatenated());
                }
            }
        }


        /// <summary>
        /// Kill all the processes started by the parentProcess
        /// </summary>
        /// <param name="parentProcess">Parent process of the processes to be killed</param>
        public static void KillProcessTree(Process parentProcess)
        {
            var processesToKill = new List<Process>();
            processesToKill.Add(parentProcess);

            for (int position = 0; position < processesToKill.Count; position++)
            {
                foreach (var process in Process.GetProcesses())
                {
                    try
                    {
                        if (process.Parent().Id == processesToKill[position].Id)
                        {
                            processesToKill.Add(process);
                        }
                    }
                    catch
                    {
                        ; // silently swallow
                    }
                }
            }

            foreach (Process t in processesToKill)
            {
                try
                {
                    t.Kill();
                }
                catch
                {
                    ; // silently swallow
                }
            }
        }

        private int getProgressValue(string message, string progressTagStart, string progressTagEnd)
        {
            int progressValue = -1;
            int endProgressTag = message.LastIndexOf(progressTagEnd);
            if (endProgressTag != -1)
            {
                int startProgressTag = message.LastIndexOf(progressTagStart);
                if (startProgressTag != -1)
                {
                    int startIndex = startProgressTag + progressTagStart.Length;
                    int valueLength = endProgressTag - startIndex;
                    string progressValueString = message.Substring(startIndex, valueLength);

                    int tmpProgressValue = -1;
                    if (int.TryParse(progressValueString, out tmpProgressValue) && tmpProgressValue > 0)
                    {
                        progressValue = tmpProgressValue;
                    }
                }
            }

            return progressValue;
        }
    }
}