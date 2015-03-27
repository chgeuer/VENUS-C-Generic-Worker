//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;

namespace Microsoft.EMIC.Cloud.GenericWorker
{
    /// <summary>
    /// Interface for Job objects
    /// </summary>
    public interface IJob
    {
        /// <summary>
        /// Gets the owner.
        /// </summary>
        string Owner { get; }

        /// <summary>
        /// Gets the internal job ID.
        /// </summary>
        string InternalJobID { get; }

        /// <summary>
        /// Gets the customer job ID.
        /// </summary>
        string CustomerJobID { get; }

        /// <summary>
        /// Gets or sets the status text.
        /// </summary>
        /// <value>
        /// The status text.
        /// </value>
        string StatusText { get; }

        /// <summary>
        /// Gets the application identification URI.
        /// </summary>
        string ApplicationIdentificationURI { get; }

        /// <summary>
        /// Gets the status.
        /// </summary>
        JobStatus Status { get; }

        /// <summary>
        /// Gets the last change.
        /// </summary>
        DateTime LastChange { get; }

        /// <summary>
        /// Gets the reset counter.
        /// </summary>
        int ResetCounter { get; }

        /// <summary>
        /// Gets the VENUS job description.
        /// </summary>
        /// <returns></returns>
        VENUSJobDescription GetVENUSJobDescription();

        /// <summary>
        /// Gets the instance ID.
        /// </summary>
        string InstanceID { get; }

        /// <summary>
        /// Gets the stdout.
        /// </summary>
        string Stdout { get; }

        /// <summary>
        /// Gets the stderr.
        /// </summary>
        string Stderr { get; }

        /// <summary>
        /// Gets the submission.
        /// </summary>
        DateTime? Submission { get; }

        /// <summary>
        /// Gets the start.
        /// </summary>
        DateTime? Start { get; }

        /// <summary>
        /// Gets the end.
        /// </summary>
        DateTime? End { get; }

        /// <summary>
        /// Gets the data checked.
        /// </summary>
        DateTime? DataChecked { get;  }
    }
}