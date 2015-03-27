//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;
using Ionic.Zip;
using Microsoft.EMIC.Cloud.ApplicationRepository;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.Configuration;
using System.Web;
using System.Runtime.Serialization;
using Microsoft.EMIC.Cloud.GenericWorker;
using Microsoft.EMIC.Cloud.DataManagement;
using Microsoft.EMIC.Cloud.Storage.Azure;
using System.ServiceModel;
using Microsoft.EMIC.Cloud.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Threading;
using JobSubmissionSampleUI.Properties;
using System.Windows.Controls.Primitives;
using System.Diagnostics;
using OGF.BES;
using System.Text.RegularExpressions;

namespace JobSubmissionSampleUI
{
	/// <summary>
	/// This is a sample application that executes a selected executable with the selected input and output files and the specified command line arguments. 
	/// </summary>
	public partial class MainWindow : Window
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MainWindow"/> class.
		/// </summary>
		public MainWindow()
		{
			InitializeComponent();
			this.Closing += new System.ComponentModel.CancelEventHandler(MainWindow_Closing);

            txtGenericWorkerUrl.Text = Settings.Default.GenericWorkerUrl;
            txtConnectionString.Text = Settings.Default.ConnectionString;
		}

		private CancellationTokenSource lastCancellationToken;

		#region UI Events
		private void btnCancel_Click(object sender, RoutedEventArgs e)
		{
            if (lastCancellationToken != null || !lastCancellationToken.IsCancellationRequested)
            {
                lastCancellationToken.Cancel();
                btnCancel.IsEnabled = false;
            }
		}


		private void btnInputFiles_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();

			openFileDialog.Multiselect = true;

			openFileDialog.ShowDialog();

			foreach (string fileName in openFileDialog.FileNames)
			{
				if (!lstInputFiles.Items.Contains(fileName))
					lstInputFiles.Items.Add(fileName);
			}
		}

		private void btnOutputFiles_Click(object sender, RoutedEventArgs e)
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog();

			saveFileDialog.ShowDialog();

			foreach (string fileName in saveFileDialog.FileNames)
			{
				if (!lstOutputFiles.Items.Contains(fileName))
					lstOutputFiles.Items.Add(fileName);
			}
		}

        const string ListBoxString = "Please select the executable file...";
        private void btnAddApplicationFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Multiselect = true;

            if (openFileDialog.ShowDialog().Value)
            {
                foreach (string fileName in openFileDialog.FileNames)
                {
                    if (!lstApplication.Items.Contains(fileName))
                        lstApplication.Items.Add(fileName);

                    if (fileName.EndsWith(".exe") || fileName.EndsWith(".bat") || fileName.EndsWith(".cmd") || fileName.EndsWith(".exe") || fileName.EndsWith(".ps1"))
                    {
                        if (!cmbExecutable.Items.Contains(fileName))
                            cmbExecutable.Items.Add(fileName);
                    }
                }

                if (cmbExecutable.Items.Count > 1)
                {
                    cmbExecutable.Items.Insert(0, ListBoxString);
                    cmbExecutable.SelectedIndex = 0;
                }
                else
                {
                    cmbExecutable.SelectedIndex = 0;
                }
            }
        }

        private void btnRemoveApplicationFile_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < lstApplication.SelectedItems.Count; i++)
            {
                string fileName = (string)lstApplication.SelectedItems[i];
                lstApplication.Items.Remove(fileName);
                i--;

                if (cmbExecutable.Items.Contains(fileName))
                    cmbExecutable.Items.Remove(fileName);

                if (cmbExecutable.Items.Contains(ListBoxString))
                {
                    if (cmbExecutable.Items.Count <= 2)
                    {
                        cmbExecutable.Items.Remove(ListBoxString);
                        cmbExecutable.SelectedIndex = 0;
                    }
                }
            }
        }

		private void btnRemoveInputFile_Click(object sender, RoutedEventArgs e)
		{
			for (int i = 0; i < lstInputFiles.SelectedItems.Count; i++)
			{
				lstInputFiles.Items.Remove(lstInputFiles.SelectedItems[i]);
				i--;
			}
		}

		private void btnRemoveOutputFile_Click(object sender, RoutedEventArgs e)
		{
			for (int i = 0; i < lstOutputFiles.SelectedItems.Count; i++)
			{
				lstOutputFiles.Items.Remove(lstOutputFiles.SelectedItems[i]);
				i--;
			}
		}

		private void btnSubmitJob_Click(object sender, RoutedEventArgs e)
        {
            if (txtGenericWorkerUrl.Text.Length == 0)
            {
                MessageBox.Show("You have to specify the Generic Worker URL!", "Validation Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                txtGenericWorkerUrl.Focus();
                return;
            }
            if (txtConnectionString.Text.Length == 0)
            {
                MessageBox.Show("You have to specify the Connection String!", "Validation Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                txtConnectionString.Focus();
                return;
            }
			if (lstApplication.Items.Count == 0)
			{
				MessageBox.Show("You have to select the Application File(s)!", "Validation Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                lstApplication.Focus();
				return;
            }
            if (HasInvalidFiles())
            {
                MessageBox.Show("There is an invalid file in the Application File(s)!", "Validation Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                lstApplication.Focus();
                return;
            }
            if (cmbExecutable.Items.Count == 0 || (cmbExecutable.Items.Count > 1 && cmbExecutable.SelectedIndex == 0))
            {
                MessageBox.Show("You have to select the executable file!", "Validation Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                cmbExecutable.Focus();
                return;
            }

			lastCancellationToken = new CancellationTokenSource();

			Thread thread = new Thread(new ThreadStart(ExecuteJob));

			thread.Start();

			ChangeJobStatus(OGF.BES.ActivityStateEnumeration.Pending, lastCancellationToken.Token);
		}

        private bool HasInvalidFiles()
        {
            foreach (string file in lstApplication.Items)
            {
                if (!new FileInfo(file).Exists)
                    return true;
            }
            return false;
        }

		private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (!btnSubmitJob.IsEnabled)
			{
				if (MessageBox.Show("There is a job running. If you close, the output files will not be downloaded. Are you sure you want to close?", "Confirmation", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) == MessageBoxResult.Cancel)
					e.Cancel = true;
			}
		}

        private void statusBarItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(((StatusBarItem)sender).Content != null)
                MessageBox.Show(((StatusBarItem)sender).Content.ToString());
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(new Uri(new Uri(txtGenericWorkerUrl.Text), "JobManagement/").ToString()));
                e.Handled = true;
            }
            catch
            {
                MessageBox.Show("Invalid Generic Worker Url!", "Validation Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                txtGenericWorkerUrl.Focus();
            }
        }
		#endregion

		#region Private methods

		/// <summary>
		/// Executes the job, runs in a seperate thread, so it uses a dispatcher to get the user interface parameters
		/// </summary>
		private void ExecuteJob()
		{
            txtConnectionString.Dispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                           new Action(
                             delegate()
                             {
                                 Settings.Default.GenericWorkerUrl = txtGenericWorkerUrl.Text;
                                 Settings.Default.ConnectionString = txtConnectionString.Text;
                                 Settings.Default.Save();
                             }
                         ));

            var UserDataStoreConnectionString = Settings.Default.ConnectionString;

			lastCancellationToken = new CancellationTokenSource();

            WriteToStatusBar("Creating Package", false);

            CloudBlobContainer appDataContainer = null;
            try
            {
                // create Cloud Container object
                var account = CloudStorageAccount.Parse(UserDataStoreConnectionString);
                var blobClient = account.CreateCloudBlobClient();

                string containerKey = "jobsubmissioncontainer";

                appDataContainer = blobClient.GetContainerReference(containerKey + DateTime.Now.ToString("yyyyMMddhhmmss"));
                appDataContainer.CreateIfNotExist();
            }
            catch (Exception ex)
            {
                WriteToStatusBar(ex.ToString());

                ChangeJobStatus(OGF.BES.ActivityStateEnumeration.Failed, lastCancellationToken.Token);

                txtConnectionString.Dispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                           new Action(
                             delegate()
                             {
                                 txtConnectionString.Focus();
                             }
                         ));

                return;
            }
            WriteToStatusBar("Creating Package.", false);

			// Take the parameters from the user interface via a dispatcher, because this is a different thread
            ItemCollection lstApplicationItems = null;
			string txtCommandText = null;
			ItemCollection lstInputFilesItems = null;
			ItemCollection lstOutputFilesItems = null;
            string cmbExecutableText = null;
			lstApplication.Dispatcher.Invoke(
				System.Windows.Threading.DispatcherPriority.Normal,
						   new Action(
							 delegate()
							 {
                                 lstApplicationItems = lstApplication.Items;
								 txtCommandText = txtCommand.Text;
								 lstInputFilesItems = lstInputFiles.Items;
								 lstOutputFilesItems = lstOutputFiles.Items;
                                 cmbExecutableText = cmbExecutable.SelectedItem.ToString();
							 }
						 ));

            FileInfo executableFileInfo = new FileInfo(cmbExecutableText);
			string applicationName = executableFileInfo.Name.Replace(executableFileInfo.Extension, string.Empty);

			string applicationIdentificationURI = "http://www.microsoft.com/emic/cloud/demo/" + applicationName;

			#region ApplicationDescription

			VENUSApplicationDescription applicationDescription = new VENUSApplicationDescription()
			{
				ApplicationIdentificationURI = applicationIdentificationURI,
				CommandTemplate = new VENUSCommandTemplate()
				{
					Path = string.Empty,
					Executable = executableFileInfo.Name,
					Args = new List<CommandLineArgument>() 
						{
							new CommandLineArgument() 
							{ 
								Name = "command", 
								FormatString = "{0}", 
								Required = Required.Mandatory,
								CommandLineArgType = CommandLineArgType.SingleLiteralArgument
							}
						}
				}
			};
			#endregion
            
            WriteToStatusBar("Creating Package..", false);
            
            try
            {
                string[] applicationFiles = new string[lstApplicationItems.Count];
                lstApplicationItems.CopyTo(applicationFiles, 0);
                var applicationZippedBytes = CreateZippedApplicationPackage(applicationFiles);

                UploadPackage(applicationDescription, applicationZippedBytes, appDataContainer);
            }
            catch (Exception ex)
            {
                WriteToStatusBar(ex.ToString());

                ChangeJobStatus(OGF.BES.ActivityStateEnumeration.Failed, lastCancellationToken.Token);

                return;
            }


            WriteToStatusBar("Creating Package...", false);

			BlobContainerPermissions resultPermissions = appDataContainer.GetPermissions();
			resultPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
			appDataContainer.SetPermissions(resultPermissions);

			#region JobDescription
			var job = new VENUSJobDescription()
			{
				ApplicationIdentificationURI = applicationIdentificationURI,
				CustomerJobID = applicationName + DateTime.Now.ToLocalTime().ToString(),
				JobName = applicationName,
				AppPkgReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(applicationIdentificationURI) + "_App"), UserDataStoreConnectionString)),
				AppDescReference = new Reference(new AzureBlobReference(appDataContainer.GetBlobReference(HttpUtility.UrlEncode(applicationIdentificationURI) + "_Desc"), UserDataStoreConnectionString)),
				JobArgs = new ArgumentCollection()
				{
						new LiteralArgument
						{
							Name = "command",
							LiteralValue = txtCommandText
						}                        
				}
			};
			#endregion

			// add the input files as downloads
			foreach (string item in lstInputFilesItems)
			{
				var fileInfo = new FileInfo(item);
				var addressInputFile = UploadFile(fileInfo.Name, (string)item, appDataContainer);
				job.Downloads.Add(new Reference(new AzureBlobReference(addressInputFile, UserDataStoreConnectionString)));
			}

			// add the output files as uploads
			foreach (string item in lstOutputFilesItems)
			{
				var fileInfo = new FileInfo(item);
				job.Uploads.Add(new Reference(new AzureBlobReference(ComputeName(fileInfo.Name, appDataContainer), UserDataStoreConnectionString)));
			}

            WriteToStatusBar("Creating Package....", false);

            if (!lastCancellationToken.IsCancellationRequested)
            {
                try
                {
                    Task jobResultPoller = new Task(() => Poll(appDataContainer, lstOutputFilesItems.Cast<string>(),
                        lastCancellationToken.Token, TimeSpan.FromSeconds(1), job), lastCancellationToken.Token, TaskCreationOptions.AttachedToParent);

                    jobResultPoller.Start();
                    WriteToStatusBar("Submitting Job", false);
                    ChangeJobStatus(OGF.BES.ActivityStateEnumeration.Pending, lastCancellationToken.Token);
                }
                catch (Exception ex)
                {
                    WriteToStatusBar(ex.ToString());

                    ChangeJobStatus(OGF.BES.ActivityStateEnumeration.Failed, lastCancellationToken.Token);

                    return;
                }
            }
            else
            {
                ChangeJobStatus(OGF.BES.ActivityStateEnumeration.Cancelled, lastCancellationToken.Token);

                WriteToStatusBar("Creating Package Cancelled", false);
            }
		}

        private void WriteToStatusBar(string text)
        {
            WriteToStatusBar(text, true);
        }

        private void WriteToStatusBar(string text, bool isError)
        {
            statusBarItem.Dispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                           new Action(
                             delegate()
                             {
                                 statusBarItem.Content = text;
                                 if (isError)
                                     statusBarItem.Foreground = Brushes.DarkRed;
                                 else
                                     statusBarItem.Foreground = Brushes.DarkGreen;
                             }
                         ));
        }

		/// <summary>
		/// Polls for output files when the job execution finishes, if the job is cancelled the terminate request is sent
		/// </summary>
		/// <param name="container">blob container instance</param>
		/// <param name="fileNames">output file names</param>
		/// <param name="cts">cancellation token</param>
		/// <param name="interval">interval to check again</param>
		/// <param name="jobSubmissionClient">job submission client to recheck the job status</param>
		/// <param name="runningJob">the endpoint reference of the executed job (contains a single item even though it is a list)</param>
		private void Poll(CloudBlobContainer container, IEnumerable<string> fileNames, CancellationToken cts, TimeSpan interval, VENUSJobDescription job)
		{
			// rechecks every interval seconds if the task is completed

            CreateActivityResponse response = null;
            GenericWorkerJobManagementClient jobSubmissionClient = null;
            try
            {
                jobSubmissionClient = CreateUnprotectedClient();

                response = jobSubmissionClient.SubmitVENUSJob(job);
            }
            catch (Exception ex)
            {
                WriteToStatusBar(ex.ToString());

                ChangeJobStatus(OGF.BES.ActivityStateEnumeration.Failed, cts);

                txtGenericWorkerUrl.Dispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                           new Action(
                             delegate()
                             {
                                 txtGenericWorkerUrl.Focus();
                             }
                         ));

                return;
            }

            // get the endpoint reference of the submitted job
            List<OGF.BES.EndpointReferenceType> runningJob = new List<OGF.BES.EndpointReferenceType>();
            runningJob.Add(response.CreateActivityResponse1.ActivityIdentifier);

            bool cancellationRequestProcessed = false;
            int iterations = 0;

			while (true)
			{
                iterations++;
                if (cts.IsCancellationRequested)
                    WriteToStatusBar("Cancelling" + new string('.', iterations % 5), false);
                else
                    WriteToStatusBar("Polling" + new string('.', iterations % 5), false);

                if (cts.IsCancellationRequested && iterations % 60 == 0 && iterations > 0)
                {
                    if (MessageBox.Show("Operation took longer than expected. Do you want to continue waiting?", string.Empty, MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        WriteToStatusBar("Cancellation aborted");

                        ChangeJobStatus(ActivityStateEnumeration.Failed, cts);

                        return;
                    }
                }

                OGF.BES.ActivityStateEnumeration state;
                try
                {
                    var activityStatusResponse = jobSubmissionClient.GetActivityStatuses(runningJob).GetActivityStatusesResponse1.Response[0];
                    if (activityStatusResponse.Fault != null)
                    {
                        WriteToStatusBar("The job status can not be retrieved." + Environment.NewLine + activityStatusResponse.Fault.faultstring);

                        ChangeJobStatus(OGF.BES.ActivityStateEnumeration.Failed, cts);

                        return;
                    }
                    state = activityStatusResponse.ActivityStatus.state;
                }
                catch(Exception ex)
                {
                    WriteToStatusBar("The job status can not be retrieved." + Environment.NewLine + ex.ToString());

                    ChangeJobStatus(OGF.BES.ActivityStateEnumeration.Failed, cts);
                    
                    return;
                }

                ChangeJobStatus(state, cts);


                if (state == OGF.BES.ActivityStateEnumeration.Failed)
                {
                    WriteToStatusBar("Job Execution Failed. Check the Job Management Site for detailed information.");
                    return;
                }
				else if (!cts.IsCancellationRequested && state == OGF.BES.ActivityStateEnumeration.Finished)
				{
                    try
                    {
                        StringBuilder blobDownloadResult = new StringBuilder();
                        foreach (string fileName in fileNames)
                        {
                            CloudBlob blobRef = container.GetBlobReference(ComputeName(new FileInfo(fileName).Name, container));
                            
                            blobRef.DownloadToFile(fileName);
                        }

                        WriteToStatusBar("Job Execution Completed Successfully.", false);

                        return;
                    }
                    catch (StorageClientException ex)
                    {
                        WriteToStatusBar(ex.Message);

                        return;
                    }
				}
				else if (cts.IsCancellationRequested && state == OGF.BES.ActivityStateEnumeration.Running && !cancellationRequestProcessed)
				{
					// when the job cancellation is requested, terminate it
					try
					{
						var terminateActivitiesResponse = jobSubmissionClient.TerminateActivities(runningJob).TerminateActivitiesResponse1.Response[0];

                        if (terminateActivitiesResponse.Fault != null)
                        {
                            WriteToStatusBar(terminateActivitiesResponse.Fault.faultstring);
                        }
                        else
                        {
                            cancellationRequestProcessed = true;
                            iterations = 0;
                        }
					}
					catch (Exception ex)
					{
                        WriteToStatusBar(ex.Message);
					}
				}
                else if (cts.IsCancellationRequested && (state == OGF.BES.ActivityStateEnumeration.Finished || state == OGF.BES.ActivityStateEnumeration.Failed || state == OGF.BES.ActivityStateEnumeration.Cancelled))
                {
                    WriteToStatusBar("Cancellation Successful", false);

                    return;
                }

				Thread.Sleep(interval);
			}
		}

		/// <summary>
		/// Locks or unlocks the user interface depending on the new status and the status of the cancellation token
		/// </summary>
		/// <param name="newStatus">the new status of the UI</param>
		/// <param name="cts">the cancellation token.</param>
		private void ChangeJobStatus(OGF.BES.ActivityStateEnumeration newStatus, CancellationToken cts)
		{
			lblJobStatus.Dispatcher.Invoke(
						   System.Windows.Threading.DispatcherPriority.Normal,
						   new Action(
							 delegate()
							 {
								 bool isFinished =
									 (newStatus == OGF.BES.ActivityStateEnumeration.Finished) || (newStatus == OGF.BES.ActivityStateEnumeration.Cancelled
									 || newStatus == OGF.BES.ActivityStateEnumeration.Failed);

								 foreach (UIElement element in grid.Children)
								 {
									 element.IsEnabled = isFinished;
								 }
								 btnCancel.IsEnabled = !isFinished;
								 btnSubmitJob.IsEnabled = isFinished;
								 lblJobStatus.IsEnabled = true;
                                 if (cts.IsCancellationRequested && !isFinished)
                                 {
                                     lblJobStatus.Content = "Cancellation Requested";
                                     btnCancel.IsEnabled = false;
                                 }
                                 else
                                     lblJobStatus.Content = newStatus;

                                 if (newStatus == ActivityStateEnumeration.Failed)
                                     lblJobStatus.Foreground = Brushes.DarkRed;
                                 else
                                     lblJobStatus.Foreground = Brushes.DarkGreen;

                                 lnkJobManagement.IsEnabled = true;
							 }
						 ));
		}

		#endregion

		#region Helper methods (similar to those used in the UPVBioClient and Installer)
		private MemoryStream CreateZippedApplicationPackage(string[] filenameArray)
		{
			MemoryStream AppZipBytes = new MemoryStream();
			using (var zip = new ZipFile())
			{
				foreach (string file in filenameArray)
					zip.AddFile(file, "");
				zip.Save(AppZipBytes);
			}
			AppZipBytes.Seek(0L, SeekOrigin.Begin);
			return AppZipBytes;
		}

		private CloudBlob UploadAppplicationDescription(VENUSApplicationDescription appDescription, CloudBlobContainer appDataContainer)
		{
			var blobName = HttpUtility.UrlEncode(appDescription.ApplicationIdentificationURI) + "_Desc";
			DataContractSerializer dcs = new DataContractSerializer(typeof(VENUSApplicationDescription));
			MemoryStream msxml = new MemoryStream();
			dcs.WriteObject(msxml, appDescription);
			CloudBlob xmlBlob = appDataContainer.GetBlobReference(blobName);
			xmlBlob.Properties.ContentType = "text/xml";
			xmlBlob.UploadByteArray(msxml.ToArray());
			return xmlBlob;
		}

		private CloudBlob UploadApplication(string appURI, MemoryStream zipBytes, CloudBlobContainer appDataContainer)
		{
			var blobName = HttpUtility.UrlEncode(appURI) + "_App";
			CloudBlob applicationBlob = appDataContainer.GetBlobReference(blobName);
			applicationBlob.UploadByteArray(zipBytes.ToArray());

			return applicationBlob;
		}

		private void UploadPackage(VENUSApplicationDescription appDesc, MemoryStream zipBytes, CloudBlobContainer appDataContainer)
		{
			UploadApplication(appDesc.ApplicationIdentificationURI, zipBytes, appDataContainer);
			UploadAppplicationDescription(appDesc, appDataContainer);
		}

		private string UploadFile(string blobname, string filename, CloudBlobContainer userDataContainer)
		{
			var blob = userDataContainer.GetBlobReference(blobname);
			blob.UploadFile(filename);
			var blobAddress = blob.Uri.AbsoluteUri;

			return blobAddress;
		}

		private string ComputeName(string name, CloudBlobContainer userDataContainer)
		{
			var result = userDataContainer.GetBlobReference(name).Uri.AbsoluteUri;

			return result;
		}

        /// <summary>
        /// Creates the unprotected client.
        /// </summary>
        /// <param name="applicationStoreUrl">The application store URL.</param>
        /// <returns></returns>
        private GenericWorkerJobManagementClient CreateUnprotectedClient()
        {
            return new GenericWorkerJobManagementClient(
                                           new WS2007HttpBinding(SecurityMode.None, reliableSessionEnabled: false),
                                           new EndpointAddress(new Uri(new Uri(Settings.Default.GenericWorkerUrl), "JobSubmission/Service.svc")));
        }
        #endregion

        
	}
}
