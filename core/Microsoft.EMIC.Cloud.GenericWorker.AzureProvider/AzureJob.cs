//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Diagnostics;
using System.IO;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.EMIC.Cloud.DataManagement;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Globalization;
using Microsoft.EMIC.Cloud.Storage.Azure;
using System.Text.RegularExpressions; //Blob Extension: Exists

namespace Microsoft.EMIC.Cloud.GenericWorker.AzureProvider
{
    /// <summary>
    /// The <see cref="IJob"/> implementation when using Windows Azure storage as coordination mechanism. 
    /// </summary>
    public class AzureJob : IJob
    {
        internal AzureJob(CloudBlobContainer container, ArgumentRepository argumentRepository, JobTableEntity entry) 
        {
            this._container = container;
            this._argumentRepository = argumentRepository;
            this.JobTableEntry = entry;
        }

        static internal AzureJob Create(CloudBlobContainer container, ArgumentRepository argumentRepository, JobTableEntity entry, VENUSJobDescription venusJobDescription)
        {
            var result = new AzureJob(container, argumentRepository, entry) 
            { 
                Status = JobStatus.Submitted 
            };
            result.SetVENUSJobDescription(venusJobDescription);
            return result;
        }

        private readonly ArgumentRepository _argumentRepository; 
        private readonly CloudBlobContainer _container;

        internal JobTableEntity JobTableEntry { get; private set; }
        /// <summary>
        /// Gets the owner.
        /// </summary>
        public string Owner { get { return this.JobTableEntry.PartitionKey; } }
        /// <summary>
        /// Gets the internal job ID.
        /// </summary>
        public string InternalJobID { get { return this.JobTableEntry.RowKey; } }
        /// <summary>
        /// Gets the customer job ID.
        /// </summary>
        public string CustomerJobID { get { return this.JobTableEntry.CustomerJobID; } }
        /// <summary>
        /// Gets the application identification URI.
        /// </summary>
        public string ApplicationIdentificationURI { get { return this.JobTableEntry.ApplicationIdentificationURI; } }


        /// <summary>
        /// Gets the name of the status text BLOB.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="internalJobId">The internal job id.</param>
        /// <returns></returns>
        internal static string GetStatusTextBlobName(string user, string internalJobId)
        {
            user = FilterOutInvalidChars(user);
            return string.Format("{0}-{1}-{2}", user, internalJobId, "statustext");
        }


        /// <summary>
        /// Gets the name of the status text BLOB.
        /// </summary>
        /// <returns></returns>
        internal string GetStatusTextBlobName()
        {
            return GetStatusTextBlobName(this.Owner, this.InternalJobID);
        }

        /// <summary>
        /// Gets or sets the status text.
        /// </summary>
        /// <value>
        /// The status text.
        /// </value>
        public string StatusText
        {
            get
            {
                var statusTextBlobName =  AzureJob.GetStatusTextBlobName(this.Owner, this.InternalJobID);
                var blob = _container.GetBlobReference(statusTextBlobName);
                if (blob.Exists())
                {
                    return (blob.DownloadText());
                }
                return "";
            }
        }

        /// <summary>
        /// Gets the status.
        /// </summary>
        public JobStatus Status
        {
            get { return this.JobTableEntry.GetStatus(); }
            internal set { this.JobTableEntry.SetStatus(value); }
        }

        /// <summary>
        /// Gets the last change.
        /// </summary>
        public DateTime LastChange
        {
            get { return this.JobTableEntry.LastChange.Value; }
            internal set { this.JobTableEntry.LastChange = value; }
        }

        /// <summary>
        /// Gets the reset counter.
        /// </summary>
        public int ResetCounter
        {
            get { return this.JobTableEntry.ResetCounter; }
        }

        /// <summary>
        /// Gets the instance ID.
        /// </summary>
        public string InstanceID 
        {
            get { return this.JobTableEntry.InstanceID; }
        }

        /// <summary>
        /// Gets the name of the STD out BLOB.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="internalJobId">The internal job id.</param>
        /// <returns></returns>
        internal static string GetStdOutBlobName(string user, string internalJobId)
        {
            user = FilterOutInvalidChars(user);
            return string.Format("{0}-{1}-{2}", user, internalJobId, "stdout");
        }

        /// <summary>
        /// Gets the name of the STD out BLOB.
        /// </summary>
        /// <returns></returns>
        internal string GetStdOutBlobName()
        {
            return GetStdOutBlobName(this.Owner, this.InternalJobID);
        }

        /// <summary>
        /// Gets the stdout.
        /// </summary>
        public string Stdout 
        {
            get 
            {
                var blob = _container.GetBlobReference(AzureJob.GetStdOutBlobName(this.Owner, this.InternalJobID));
                if (blob.Exists())
                {
                    return (blob.DownloadText());
                }
                return "";
            }
        }

        /// <summary>
        /// Gets the name of the STD err BLOB.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="internalJobId">The internal job id.</param>
        /// <returns></returns>
        public static string GetStdErrBlobName(string user, string internalJobId)
        {
            user = FilterOutInvalidChars(user);
            return string.Format("{0}-{1}-{2}", user, internalJobId, "stderr");
        }

        /// <summary>
        /// Gets the name of the STD err BLOB.
        /// </summary>
        /// <returns></returns>
        internal string GetStdErrBlobName()
        {
            return GetStdErrBlobName(this.Owner, this.InternalJobID);
        }

        private static string FilterOutInvalidChars(string text)
        {
            return Regex.Replace(text,"[^a-zA-Z0-9]","");
        }

        /// <summary>
        /// Gets the stderr.
        /// </summary>
        public string Stderr 
        {
            get
            {
                var blob = _container.GetBlobReference(AzureJob.GetStdErrBlobName(this.Owner, this.InternalJobID));
                if (blob.Exists())
                {
                    return (blob.DownloadText());
                }
                return "";
            }
        }

        /// <summary>
        /// Gets the submission.
        /// </summary>
        public DateTime? Submission 
        {
            get { return this.JobTableEntry.Submission; }
        }

        /// <summary>
        /// Gets the start.
        /// </summary>
        public DateTime? Start 
        {
            get { return this.JobTableEntry.Start; }
        }

        /// <summary>
        /// Gets the end.
        /// </summary>
        public DateTime? End 
        {
            get { return this.JobTableEntry.End; }
        }

        /// <summary>
        /// Gets the data checked.
        /// </summary>
        public DateTime? DataChecked 
        {
            get { return this.JobTableEntry.DataChecked; }
        }

        /// <summary>
        /// Gets the GUID of the parent.(Applicable to hierarchial jobs)
        /// </summary>
        public string ParentGUID 
        {
            get { return this.JobTableEntry.ParentGUID; }
        }


        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.JobTableEntry.ToString();
        }

        #region NotificationSubscriptions
        /// <summary>
        /// Gets the VENUS job description.
        /// </summary>
        /// <returns></returns>
        public List<Subscription> GetNotificationSubscriptions(CloudBlobContainer subscriptionsContainer)
        {
            if (subscriptionsContainer == null)
            {
                throw new ArgumentException(ExceptionMessages.SubscriptionContainerNull);
            }
            var newSubscriptions = new List<Subscription>();

            var xs = new XmlSerializer(typeof(List<Subscription>));

            var blob = subscriptionsContainer.GetBlobReference(this.InternalJobID); 
            if (blob.Exists())
            {               
                var subscriptions = blob.DownloadText();
                using (var msd = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(subscriptions)))
                {
                    var oldSubs = (List<Subscription>)xs.Deserialize(msd);
                    newSubscriptions.AddRange(oldSubs);
                }
            }
            return newSubscriptions;
        }

        /// <summary>
        /// Sets the VENUS job description.
        /// </summary>
        public void SetNotificationSubscriptions(List<Subscription> newSubscriptions, CloudBlobContainer subscriptionsContainer)
        {
            using (var mss = new MemoryStream())
            {
                var xs = new XmlSerializer(typeof(List<Subscription>));
                xs.Serialize(mss, newSubscriptions);
                mss.Seek(0L, SeekOrigin.Begin);
                var blob = subscriptionsContainer.GetBlobReference(this.InternalJobID);                
                blob.Properties.ContentType = "text/xml";
                blob.UploadFromStream(mss);
                Trace.TraceInformation(string.Format(CultureInfo.CurrentCulture, "Uploaded subscriptions to blob {0}", blob.Uri.AbsoluteUri));
            }
        }

        #endregion

        //TODO:consider to use Guids instead of strings
        #region Children Management 


        /// <summary>
        /// Gets the children of a job
        /// </summary>
        /// <returns></returns>
        public List<string> GetChildren()
        {
            var childrenContainer = _container;

            var childrenList = new List<string>();
            var blob = childrenContainer.GetBlobReference("children-"+this.InternalJobID);
            if (blob.Exists())
            {
                var children = blob.DownloadText().Split(';');
                childrenList.AddRange(children);
            }
            return childrenList;
        }


        /// <summary>
        /// Sets the children of a job.
        /// </summary>
        /// <param name="newChildrenList">The new children list.</param>
        public void SetChildren(List<string> newChildrenList)
        {
            var childrenContainer = _container;

            var blob = childrenContainer.GetBlobReference("children-" + this.InternalJobID);
            var children = string.Join(";", newChildrenList);
            blob.UploadText(children);
        }

        /// <summary>
        /// Adds a child to a job.
        /// </summary>
        /// <param name="childToAdd">The internal job id of the new child.</param>
        public void AddChild(string childToAdd) 
        {
            var children = GetChildren();
            children.Add(childToAdd);
            SetChildren(children);
        }

        /// <summary>
        /// Removes a child from a job
        /// </summary>
        /// <param name="childToRemove">The internal job id of the child to remove.</param>
        public void RemoveChild(string childToRemove)
        {
            var children = GetChildren();
            children.Remove(childToRemove);
            if (children.Count != 0)
            {
                SetChildren(children);
            }
            else
            {
                RemoveChildren();
            }
        }

        /// <summary>
        /// Removes all children of the job. The children blob is deleted from the storage.
        /// </summary>
        public void RemoveChildren()
        {
            var childrenContainer = _container;
            var blob = childrenContainer.GetBlobReference("children-" + this.InternalJobID);
            blob.DeleteIfExists();
        }

        /// <summary>
        /// Determines whether this instance has children.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance has children; otherwise, <c>false</c>.
        /// </returns>
        public bool HasChildren()
        {
            return (GetChildren().Count != 0);
        }

        #endregion
        /// <summary>
        /// Determines whether a job is terminated.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the job is terminated; otherwise, <c>false</c>.
        /// </returns>
        public bool IsTerminated()
        {
            var status = this.Status;
            return (!(status == JobStatus.Submitted ||
                            status == JobStatus.CheckingInputData ||
                            status == JobStatus.Running ||
                            status == JobStatus.CancelRequested));
        }

        /// <summary>
        /// Deletes the associated blobs of this job instance.
        /// </summary>
        public void DeleteAssociatedBlobs()
        {
            AzureJob.DeleteAssociatedBlobs(_container, this.Owner, this.InternalJobID);
        }

        /// <summary>
        /// Deletes the associated blobs of a specified job instance.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="username">The username.</param>
        /// <param name="internalJobId">The internal job id.</param>
        public static void DeleteAssociatedBlobs(CloudBlobContainer container, string username, string internalJobId)
        {
            username = FilterOutInvalidChars(username);

            //JobDescription
            container.GetBlobReference(internalJobId).DeleteIfExists();

            container.GetBlobReference(AzureJob.GetStdOutBlobName(username, internalJobId)).DeleteIfExists();
            container.GetBlobReference(AzureJob.GetStdErrBlobName(username, internalJobId)).DeleteIfExists();
            container.GetBlobReference(AzureJob.GetStatusTextBlobName(username, internalJobId)).DeleteIfExists();        
        }

        VENUSJobDescription _mVenusJobDescription;
        /// <summary>
        /// Gets the VENUS job description. Attention: This Operation perfoms a blob download. 
        /// The JobDescription is only once saved to blob store is not subject of modifications. Hence, in the same scope it should not be used multiple times.
        /// </summary>
        /// <returns></returns>
        public VENUSJobDescription GetVENUSJobDescription()
        {
            if (_mVenusJobDescription == null)
            {
                try
                {
                    CloudBlob blob = _container.GetBlobReference(this.InternalJobID);
                    var ms = new MemoryStream();
                    blob.DownloadToStream(ms);
                    ms.Seek(0L, SeekOrigin.Begin);

                    this._mVenusJobDescription = VENUSJobDescription.FromStream(ms, this._argumentRepository);
                }
                catch (StorageClientException ex)
                {
                    if (ex.ErrorCode != StorageErrorCode.BlobNotFound) throw;
                }
            }
            return _mVenusJobDescription;
        }

        /// <summary>
        /// Sets the VENUS job description.
        /// </summary>
        /// <param name="venusJobDescription">The venus job description.</param>
        public void SetVENUSJobDescription(VENUSJobDescription venusJobDescription)
        {
            var stream = venusJobDescription.AsMemoryStream();
            var blob = _container.GetBlobReference(this.InternalJobID);
            blob.Properties.ContentType = "text/xml";
            blob.UploadFromStream(stream);

            Trace.TraceInformation(string.Format(CultureInfo.CurrentCulture, "Uploaded job to blob {0}", blob.Uri.AbsoluteUri));

            _mVenusJobDescription = venusJobDescription;
        }
    }
}