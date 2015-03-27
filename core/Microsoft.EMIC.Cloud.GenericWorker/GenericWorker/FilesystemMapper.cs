//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Ionic.Zip;
using Microsoft.EMIC.Cloud.ApplicationRepository;
using Microsoft.EMIC.Cloud.DataManagement;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Tests")]

namespace Microsoft.EMIC.Cloud.GenericWorker
{
    /// <summary>
    /// Abstract away underlying file system on the generic worker. 
    /// </summary>
    [Export(typeof(FilesystemMapper))]
    public class FilesystemMapper
    {
        /// <summary>
        /// Gets or sets the application install directory for the generic worker.
        /// </summary>
        [Import(CompositionIdentifiers.GenericWorkerDirectoryApplicationInstallation, 
            RequiredCreationPolicy = CreationPolicy.NonShared)]
        public string GenericWorkerDirectoryApplicationInstallation { get; set; }

        /// <summary>
        /// Gets or sets the user folder for the generic worker.
        /// </summary>
        [Import(CompositionIdentifiers.GenericWorkerDirectoryUserFolder)]
        public string GenericWorkerDirectoryUserFolder { get; set; }

        /// <summary>
        /// Gets or sets the generic worker directory reference data sets.
        /// </summary>
        /// <value>
        /// The generic worker directory reference data sets.
        /// </value>
        [Import(CompositionIdentifiers.GenericWorkerDirectoryReferenceDataSets)]
        public string GenericWorkerDirectoryReferenceDataSets { get; set; }

        /// <summary>
        /// Creates the specified directory.
        /// </summary>
        /// <param name="dir">The directory.</param>
        /// <returns></returns>
        private static DirectoryInfo Create(string dir)
        {
            if (!Directory.Exists(dir))
                return Directory.CreateDirectory(dir);

            return new DirectoryInfo(dir);
        }

        /// <summary>
        /// Gets the application folder.
        /// </summary>
        public DirectoryInfo ApplicationFolder
        {
            get { return Create(GenericWorkerDirectoryApplicationInstallation); }
        }

        /// <summary>
        /// Gets the user data set folder.
        /// </summary>
        public DirectoryInfo UserDataSetFolder
        {
            get { return Create(GenericWorkerDirectoryUserFolder); }
        }

        /// <summary>
        /// Gets the reference data set folder.
        /// </summary>
        public DirectoryInfo ReferenceDataSetFolder
        {
            get { return Create(GenericWorkerDirectoryReferenceDataSets); }
        }

        internal string GetApplicationFolder(string userName, VENUSApplicationDescription appDescription)
        {
            var folder = Path.Combine(Path.Combine(this.ApplicationFolder.FullName, userName), appDescription.ApplicationID);
            return folder;
        }


        internal bool IsRemoteAppNewer(string userName, VENUSJobDescription jobDescription, VENUSApplicationDescription appDescription)
        {
            var folder = GetApplicationFolder(userName, appDescription);
            var pathTimeStamp = Path.Combine(folder, "GenericWorkerTimeStamp.txt");
            if (File.Exists(pathTimeStamp))
            {
                var cloudPkgTimeStamp = jobDescription.AppPkgReference.ProviderSpecificReference.GetLastModificationDateTime().Ticks;
                var installedPkgTimeStamp = Int64.Parse(File.ReadAllText(pathTimeStamp));
                if (installedPkgTimeStamp >= cloudPkgTimeStamp)
                {
                    Trace.TraceInformation("installed app is up to date, no need for reinstalling it");
                    return false;
                }
            }
            return true;
        }

        internal void InstallApplicationIfNeeded(string userName, VENUSJobDescription jobDescription, VENUSApplicationDescription appDescription)
        {
            var folder = GetApplicationFolder(userName, appDescription);
            var pathTimeStampFile = Path.Combine(folder, "GenericWorkerTimeStamp.txt");
            var cloudPkgTimeStamp = jobDescription.AppPkgReference.ProviderSpecificReference.GetLastModificationDateTime().Ticks;

            if (!IsRemoteAppNewer(userName, jobDescription, appDescription))
            {
                Trace.TraceInformation("the application package on the instance is up to date, no need to reinstall the application");
                return;
            }

            if (Directory.Exists(folder))
            {
                Debug.Assert(File.Exists(pathTimeStampFile), "Did not find file with timestamp");                
                Trace.TraceInformation("application package at remote location has changed, reinstall the application");
                for (var retries = 0; retries < 20; retries++)
                {
                    try
                    {
                        Directory.Delete(folder, true);
                        Trace.TraceInformation("The dir could be deleted");
                        break;
                    }
                    catch (DirectoryNotFoundException)
                    {
                        break;
                    }
                    catch (Exception exc)
                    {
                        Trace.TraceError("directory delete try: " + retries + "exception: " + exc.Message);
                        Thread.Sleep(TimeSpan.FromSeconds(15));
                    }
                }
            }

            Directory.CreateDirectory(folder);
            Trace.TraceInformation(string.Format("Created folder {0} for application {1}",
                folder, appDescription.ApplicationIdentificationURI));
            File.WriteAllText(pathTimeStampFile, cloudPkgTimeStamp.ToString());            

#if DEBUG
            var mutex = new Mutex(false, appDescription.ApplicationIdentificationURI);
            bool lockAcquired = false;
            try
            {
                lockAcquired = mutex.WaitOne(TimeSpan.Zero, true);
#endif

                var bytes = jobDescription.AppPkgReference.DownloadContents();
                using (var zip = ZipFile.Read(bytes))
                {
                    zip.ExtractAll(folder, ExtractExistingFileAction.OverwriteSilently);
                }

#if DEBUG
            }
            finally
            {
                if (lockAcquired)
                {
                    mutex.ReleaseMutex();
                }
            }
#endif
        }
    }
}
