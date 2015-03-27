//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.EMIC.Cloud.DataManagement;
using OGF.BES;
using OGF.JDSL;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Tests")]

namespace Microsoft.EMIC.Cloud.GenericWorker
{

    /// <summary>
    /// Enum to specify the handling of missing output files for the job by the GW
    /// </summary>
    public enum MissingResultFilePolicy
    {
        /// <summary>
        /// Standard result file policy
        /// </summary>
        Standard = 0,
        /// <summary>
        /// Creates files which are zero sized
        /// </summary>
        GenerateZeroFiles = 1
    }

    /// <summary>
    /// This class represents the profile for the generic worker and its integration with OGF BES/JSDL.
    /// </summary>
    /// 
    public class VENUSJobDescription
    {
        private const string GenericWorkerNamespace = "http://www.microsoft.com/emic/cloud/GW";

        /// <summary>
        /// Initializes a new instance of the <see cref="VENUSJobDescription"/> class.
        /// </summary>
        public VENUSJobDescription() 
        {            
            this.JobArgs = new ArgumentCollection();
            this.Uploads = new ReferenceCollection();
            this.Downloads = new ReferenceCollection();
            this.MissingResultFilePolicy = MissingResultFilePolicy.Standard;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VENUSJobDescription"/> class.
        /// </summary>
        /// <param name="createActivityRequest">The create activity.</param>
        /// <param name="argumentRepository">The argument repository.</param>
        public VENUSJobDescription(CreateActivityRequest createActivityRequest, ArgumentRepository argumentRepository) 
        {
            this.Load(createActivityRequest.CreateActivity, argumentRepository); 
        }

        private void Load(CreateActivityType value, ArgumentRepository argumentRepository)
        {
            if (value == null) { value = new CreateActivityType(); }
            if (value.ActivityDocument == null) { value.ActivityDocument = new ActivityDocumentType(); }
            if (value.ActivityDocument.JobDefinition == null) { value.ActivityDocument.JobDefinition = new JobDefinition_Type(); }
            if (value.ActivityDocument.JobDefinition.JobDescription == null) { value.ActivityDocument.JobDefinition.JobDescription = new JobDescription_Type(); }
            if (value.ActivityDocument.JobDefinition.JobDescription.Application == null) { value.ActivityDocument.JobDefinition.JobDescription.Application = new Application_Type(); }
            if (value.ActivityDocument.JobDefinition.JobDescription.DataStaging == null) { value.ActivityDocument.JobDefinition.JobDescription.DataStaging = new List<DataStaging_Type>(); }
            if (value.ActivityDocument.JobDefinition.JobDescription.JobIdentification == null) { value.ActivityDocument.JobDefinition.JobDescription.JobIdentification = new JobIdentification_Type(); }
            if (value.ActivityDocument.JobDefinition.JobDescription.JobIdentification.JobAnnotation == null) { value.ActivityDocument.JobDefinition.JobDescription.JobIdentification.JobAnnotation = new List<string>(); }
            if (value.ActivityDocument.JobDefinition.JobDescription.JobIdentification.JobProject == null) { value.ActivityDocument.JobDefinition.JobDescription.JobIdentification.JobProject = new List<string>(); }
            if (value.ActivityDocument.JobDefinition.JobDescription.Resources == null) { value.ActivityDocument.JobDefinition.JobDescription.Resources = new Resources_Type(); }
            if (value.ActivityDocument.JobDefinition.JobDescription.Resources.CandidateHosts == null) { value.ActivityDocument.JobDefinition.JobDescription.Resources.CandidateHosts = new List<string>(); }
            if (value.ActivityDocument.JobDefinition.JobDescription.Resources.FileSystem == null) { value.ActivityDocument.JobDefinition.JobDescription.Resources.FileSystem = new List<FileSystem_Type>(); }

            this.CustomerJobID = value.ActivityDocument.JobDefinition.id;
            this.ApplicationIdentificationURI = value.ActivityDocument.JobDefinition.JobDescription.Application.ApplicationName;
            this.JobName = value.ActivityDocument.JobDefinition.JobDescription.JobIdentification.JobName;
            this.JobArgs = new ArgumentCollection();
            this.Uploads = new ReferenceCollection();
            this.Downloads = new ReferenceCollection();
            this.MissingResultFilePolicy = MissingResultFilePolicy.Standard;
            Func<XmlElement, bool> filter = e => e.LocalName.Equals("Args") && e.NamespaceURI.Equals(GenericWorkerNamespace);
            var elem = value.ActivityDocument.JobDefinition.JobDescription.Application.Any.Where(filter).FirstOrDefault();
            if (elem != null)
            {
                var x = elem.ChildNodes.OfType<XmlElement>().Select(argumentRepository.Load).ToList();

                this.JobArgs = new ArgumentCollection();
                this.JobArgs.AddRange(x);
            }
            filter = e => e.LocalName.Equals("PkgRef") && e.NamespaceURI.Equals(GenericWorkerNamespace);
            var pkgRefElem = value.ActivityDocument.JobDefinition.JobDescription.Application.Any.Where(filter).FirstOrDefault();
            if (pkgRefElem != null)
            {
                this.AppPkgReference = argumentRepository.LoadRefEl((XmlElement)pkgRefElem.FirstChild);
            }
            filter = e => e.LocalName.Equals("DescRef") && e.NamespaceURI.Equals(GenericWorkerNamespace);
            var descRefElem = value.ActivityDocument.JobDefinition.JobDescription.Application.Any.Where(filter).FirstOrDefault();
            if (descRefElem != null)
            {
                this.AppDescReference = argumentRepository.LoadRefEl((XmlElement)descRefElem.FirstChild);
            }
            filter = e => e != null && e.LocalName.Equals("DownloadsRef") && e.NamespaceURI.Equals(GenericWorkerNamespace);
            var downloadsRefElem = value.ActivityDocument.JobDefinition.JobDescription.Any.Where(filter).FirstOrDefault();
            if (downloadsRefElem != null)
            {
                this.DownloadsReference = argumentRepository.LoadRefEl((XmlElement)downloadsRefElem.FirstChild);

                var refArrXml = System.Text.Encoding.Default.GetString(this.DownloadsReference.DownloadContents());
                var xd = new XmlDocument();
                xd.InnerXml = refArrXml;

                var x = xd.FirstChild.ChildNodes.OfType<XmlElement>().Select(argumentRepository.LoadRefEl).ToList().Cast<Reference>();
                this.Downloads.AddRange(x);
                this.DownloadsReference = null;
                value.ActivityDocument.JobDefinition.JobDescription.Any.Remove(downloadsRefElem);
            }
            filter = e => e != null && e.LocalName.Equals("UploadsRef") && e.NamespaceURI.Equals(GenericWorkerNamespace);
            var uploadsRefElem = value.ActivityDocument.JobDefinition.JobDescription.Any.Where(filter).FirstOrDefault();
            if (uploadsRefElem != null)
            {
                this.UploadsReference = argumentRepository.LoadRefEl((XmlElement)uploadsRefElem.FirstChild);

                var refArrXml = System.Text.Encoding.Default.GetString(this.UploadsReference.DownloadContents());
                var xd = new XmlDocument();
                xd.InnerXml = refArrXml;

                var x = xd.FirstChild.ChildNodes.OfType<XmlElement>().Select(argumentRepository.LoadRefEl).ToList().Cast<Reference>();

                this.Uploads.AddRange(x);
                this.UploadsReference = null;
                value.ActivityDocument.JobDefinition.JobDescription.Any.Remove(uploadsRefElem);
            }

            filter = e => e != null && e.LocalName.Equals("ClientVersion") && e.NamespaceURI.Equals(GenericWorkerNamespace);
            var clientVersion = value.ActivityDocument.JobDefinition.JobDescription.Any.Where(filter).FirstOrDefault();
            if (clientVersion != null)
            {
                this.ClientVersion = clientVersion.InnerText;
            }
            filter = e => e != null && e.LocalName.Equals("ResultFileName") && e.NamespaceURI.Equals(GenericWorkerNamespace);
            var resultFileNameElem = value.ActivityDocument.JobDefinition.JobDescription.Any.Where(filter).FirstOrDefault();
            if (resultFileNameElem != null)
            {
                this.ResultZipFilename = resultFileNameElem.InnerText;
            }
            filter = e => e != null && e.LocalName.Equals("InputFileName") && e.NamespaceURI.Equals(GenericWorkerNamespace);
            var inputFileNameElem = value.ActivityDocument.JobDefinition.JobDescription.Any.Where(filter).FirstOrDefault();
            if (inputFileNameElem != null)
            {
                this.InputZipFilename = inputFileNameElem.InnerText;
            }
            filter = e => e!=null && e.LocalName.Equals("MissingResultFilePolicy") && e.NamespaceURI.Equals(GenericWorkerNamespace);
            var missingResultFilePolicyElem = value.ActivityDocument.JobDefinition.JobDescription.Any.Where(filter).FirstOrDefault();
            if (missingResultFilePolicyElem != null)
            {
                this.MissingResultFilePolicy = (MissingResultFilePolicy)Enum.Parse(typeof(MissingResultFilePolicy), missingResultFilePolicyElem.InnerText); 
            }
            filter = e => e != null && e.LocalName.Equals("Uploads") && e.NamespaceURI.Equals(GenericWorkerNamespace);
            var upelem = value.ActivityDocument.JobDefinition.JobDescription.Any.Where(filter).FirstOrDefault();
            if (upelem != null)
            {
                var x = upelem.ChildNodes.OfType<XmlElement>().Select(argumentRepository.LoadRefEl).ToList().Cast<Reference>();

                this.Uploads.AddRange(x);
            }
            filter = e => e != null && e.LocalName.Equals("Downloads") && e.NamespaceURI.Equals(GenericWorkerNamespace);
            var downelem = value.ActivityDocument.JobDefinition.JobDescription.Any.Where(filter).FirstOrDefault();
            if (downelem != null)
            {
                var x = downelem.ChildNodes.OfType<XmlElement>().Select(argumentRepository.LoadRefEl).ToList().Cast<Reference>();

                this.Downloads.AddRange(x);
            }
        }

        /// <summary>
        /// Gets the create activity.
        /// </summary>
        /// <returns></returns>
        internal CreateActivityRequest GetCreateActivity () 
        {
            var fac = new XmlDocument();
            XmlElement argElem = fac.CreateElement("emic:Args", GenericWorkerNamespace);
            this.JobArgs.ForEach(a =>
            {
                var e = a.Serialize(fac);
                argElem.AppendChild(e);
            });
            XmlElement upElem = fac.CreateElement("emic:Uploads", GenericWorkerNamespace);
            this.Uploads.ForEach(a => { var e = a.Serialize(fac); upElem.AppendChild(e); });
            XmlElement downElem = fac.CreateElement("emic:Downloads", GenericWorkerNamespace);
            this.Downloads.ForEach(a => { var e = a.Serialize(fac); downElem.AppendChild(e); });
            XmlElement resultFileName = fac.CreateElement("emic:ResultFileName", GenericWorkerNamespace);
            resultFileName.InnerText = ResultZipFilename;
            XmlElement inputFileName = fac.CreateElement("emic:InputFileName", GenericWorkerNamespace);
            inputFileName.InnerText = InputZipFilename;
            XmlElement missingResultFilePolicy = fac.CreateElement("emic:MissingResultFilePolicy", GenericWorkerNamespace);
            missingResultFilePolicy.InnerText = Enum.GetName(typeof(MissingResultFilePolicy), MissingResultFilePolicy); 
            XmlElement pkgRef=null;
            if (this.AppPkgReference != null)
            {
                pkgRef = fac.CreateElement("emic:PkgRef", GenericWorkerNamespace);
                pkgRef.AppendChild(this.AppPkgReference.Serialize(fac));
            }
            XmlElement descRef=null;
            if (this.AppDescReference != null)
            {
                descRef = fac.CreateElement("emic:DescRef", GenericWorkerNamespace);
                descRef.AppendChild(this.AppDescReference.Serialize(fac));
            }
            XmlElement downloadsRef = null;
            if (this.DownloadsReference != null)
            {
                downloadsRef = fac.CreateElement("emic:DownloadsRef", GenericWorkerNamespace);
                downloadsRef.AppendChild(this.DownloadsReference.Serialize(fac));
            }
            XmlElement uploadsRef = null;
            if (this.UploadsReference != null)
            {
                uploadsRef = fac.CreateElement("emic:UploadsRef", GenericWorkerNamespace);
                uploadsRef.AppendChild(this.UploadsReference.Serialize(fac));
            }
            XmlElement clientVersion = null;
            if (this.ClientVersion != null)
            {
                clientVersion = fac.CreateElement("emic:ClientVersion", GenericWorkerNamespace);
                clientVersion.InnerText = this.ClientVersion;
            }
            return new CreateActivityRequest
            {
                CreateActivity = new CreateActivityType
                {
                    ActivityDocument = new ActivityDocumentType
                    {
                        JobDefinition = new JobDefinition_Type
                        {
                            id = this.CustomerJobID,
                            JobDescription = new JobDescription_Type
                            {
                                Application = new Application_Type
                                {
                                    ApplicationName = this.ApplicationIdentificationURI,
                                    Any = new List<XmlElement> { argElem, pkgRef, descRef }
                                },
                                JobIdentification = new JobIdentification_Type
                                {
                                    JobName = this.JobName
                                },
                                Any = new List<XmlElement> { resultFileName, inputFileName, upElem, downElem, downloadsRef, uploadsRef, clientVersion, missingResultFilePolicy }
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Gets or sets the customer job ID.
        /// </summary>
        /// <value>
        /// The customer job ID.
        /// </value>
        public string CustomerJobID { get; set; }

        /// <summary>
        /// Gets or sets the name of the job.
        /// </summary>
        /// <value>
        /// The name of the job.
        /// </value>
        public string JobName { get; set; }


        /// <summary>
        /// Gets or sets the result zip filename.
        /// </summary>
        /// <value>
        /// The result zip filename. The zip is only created if the zipfilename is not null, and a valid file name
        /// </value>
        public string ResultZipFilename { get; set; }

        /// <summary>
        /// Gets or sets the input zip filename.
        /// This zip file is extracted to the working directory before the application is started.
        /// </summary>
        /// <value>
        /// The input zip filename. The zip is only created if the zipfilename is not null, and a valid file name
        /// </value>
        public string InputZipFilename { get; set; }

        /// <summary>
        /// Gets or sets the job priority.
        /// </summary>
        /// <value>
        /// The job priority.
        /// </value>
        public JobPriority JobPrio { get; set; }
        /// <summary>
        /// Gets or sets the application identification URI.
        /// </summary>
        /// <value>
        /// The application identification URI.
        /// </value>
        public string ApplicationIdentificationURI { get; set; }

        /// <summary>
        /// Gets or sets the job arguments.
        /// </summary>
        /// <value>
        /// The job arguments.
        /// </value>
        public ArgumentCollection JobArgs { get; set; }

        /// <summary>
        /// Gets or sets the uploads. 
        /// </summary>
        /// <value>
        /// List of reference arguments for uploading resultfiles after job execution.
        /// The complete list can be accessed with the GetAllUploads() method.
        /// This is an easy way to define uploads. The more flexible way is the use of UploadsReference. Hence, this should be evtl. deprecated.
        /// </value>
        public ReferenceCollection Uploads { get; set; }

        /// <summary>
        /// Gets or sets the downloads. 
        /// </summary>
        /// <value>
        /// List of reference arguments for downloading inputfiles prior to job execution.
        /// The complete list can be accessed with the GetAllDownloads() method.
        /// This is an easy way to define downloads. The more flexible way is the use of DownloadsReference. Hence, this should be evtl. deprecated.
        /// </value>
        public ReferenceCollection Downloads { get; set; }

        /// <summary>
        /// Gets or sets the storage reference to a serialized list of upload references.
        /// </summary>
        /// <value>
        /// The upload reference.
        /// </value>
        public Reference UploadsReference { get; set; }

        /// <summary>
        /// Gets or sets the storage reference to a serialized list of download references.
        /// </summary>
        /// <value>
        /// The download reference.
        /// </value>
        public Reference DownloadsReference { get; set; }

        /// <summary>
        /// Gets or sets the app PKG reference.
        /// </summary>
        /// <value>
        /// The app PKG reference.
        /// </value>
        public Reference AppPkgReference { get; set; }

        /// <summary>
        /// Gets or sets the client version.
        /// </summary>
        /// <value>
        /// The client version.
        /// </value>
        public string ClientVersion { get; set; }

        /// <summary>
        /// Gets or sets the app desc reference.
        /// </summary>
        /// <value>
        /// The app desc reference.
        /// </value>
        public Reference AppDescReference { get; set; }

        /// <summary>
        /// Gets or sets the missing result file policy.
        /// </summary>
        /// <value>
        /// The missing result file policy.
        /// </value>
        public MissingResultFilePolicy MissingResultFilePolicy { get; set; }

        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(CreateActivityRequest));

        /// <summary>
        /// Factory method to load a <see cref="VENUSJobDescription"/> from a stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="argumentRepository">The argument repository.</param>
        /// <returns></returns>
        public static VENUSJobDescription FromStream(Stream stream, ArgumentRepository argumentRepository)
        {
            stream.Seek(0L, SeekOrigin.Begin);

            var ca = (CreateActivityRequest)Serializer.Deserialize(stream);

            return new VENUSJobDescription(ca, argumentRepository);
        }

        /// <summary>
        /// Serializes the  <see cref="VENUSJobDescription"/> into a  <see cref="System.IO.MemoryStream"/>.
        /// </summary>
        /// <returns></returns>
        public MemoryStream AsMemoryStream()
        {
            var stream = new MemoryStream();
            var activity = this.GetCreateActivity();
            Serializer.Serialize(stream, activity);
            stream.Seek(0L, SeekOrigin.Begin);
            return stream;
        }
    }
}