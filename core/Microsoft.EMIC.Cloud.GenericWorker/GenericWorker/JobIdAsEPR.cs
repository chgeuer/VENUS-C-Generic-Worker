//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Microsoft.EMIC.Cloud.GenericWorker
{
    using System;
    using System.Xml;
    using OGF.BES;

    /// <summary>
    /// Represents a job ID as an endpoint reference. 
    /// </summary>
    public class JobIdAsEPR
    {
        const string ElementName = "Id";

        /// <summary>
        /// Gets the value.
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JobIdAsEPR"/> class.
        /// </summary>
        /// <param name="jobId">The job id.</param>
        public JobIdAsEPR(string jobId)
        {
            this.Value = jobId;
        }

        /// <summary>
        /// Loads the specified end point.
        /// </summary>
        /// <param name="endPoint">The end point.</param>
        /// <param name="baseAddress">The base address.</param>
        /// <param name="runtimeEnvironment">The runtime environment.</param>
        /// <returns></returns>
        public static JobIdAsEPR Load(EndpointReferenceType endPoint, string baseAddress, IGWRuntimeEnvironment runtimeEnvironment)
        {
            if (endPoint.Address.Value.ToString() != baseAddress || endPoint.ReferenceParameters.Any[0].Name != ElementName) //convert them to extraction/creation/validation type
            {
                throw new NotSupportedException();
            }
            var internalJobId = endPoint.ReferenceParameters.Any[0].InnerText;

            return new JobIdAsEPR(internalJobId);
        }

        /// <summary>
        /// Returns this instance as an XML element.
        /// </summary>
        /// <returns></returns>
        public XmlElement AsXmlElement()
        {
            var jobIdAsElement = new XmlDocument().CreateElement(ElementName);
            jobIdAsElement.InnerText = Value;
            return jobIdAsElement;
        }
    }
}