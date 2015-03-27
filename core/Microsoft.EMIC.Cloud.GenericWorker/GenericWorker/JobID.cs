//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Microsoft.EMIC.Cloud.GenericWorker
{
    using System;

    /// <summary>
    /// Class for an Job ID
    /// </summary>
    public class JobID
    {
        /// <summary>
        /// Gets the current job ID.
        /// </summary>
        public static JobID CurrentJobID
        {
            get
            {
                var s = Environment.GetEnvironmentVariable(GenericWorkerDriver.ENVIRONMENT_VARIABLE_JOBID);
                
                return string.IsNullOrEmpty(s) ? null : new JobID(s);
            }
        }
        /// <summary>
        /// Gets the value.
        /// </summary>
        public Uri Value { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JobID"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public JobID(Uri value)
        {
            if (value.Scheme != "jobid")
            {
                throw new NotSupportedException();
            }

            this.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JobID"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public JobID(string value)
        {
            Uri uriValue;
            if (!Uri.TryCreate(value, UriKind.Absolute, out uriValue))
            {
                throw new NotSupportedException();
            }

            if (uriValue.Scheme != "jobid")
            {
                throw new NotSupportedException();
            }

            this.Value = uriValue;
        }


        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.Value.ToString();
        }


        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }

        /// <summary>
        /// Determines whether [is part of] [the specified potential parent].
        /// </summary>
        /// <param name="potentialAncestor">The potential ancestor.</param>
        /// <returns>
        ///   <c>true</c> if [is part of] [the specified potential ancestor]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsPartOf(JobID potentialAncestor)
        {
            return this.Value.AbsoluteUri.StartsWith(potentialAncestor.Value.AbsoluteUri);
        }

        /// <summary>
        /// Determines whether the specified potential parent.
        /// </summary>
        /// <param name="potentialParent">The potential parent.</param>
        /// <returns>
        ///   <c>true</c> if [is part of] [the specified potential parent]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsDirectChildOf(JobID potentialParent)
        {
            if (this.Value.AbsoluteUri.StartsWith(potentialParent.Value.AbsoluteUri) && this.Value.AbsoluteUri.Length> potentialParent.Value.AbsoluteUri.Length)
            {
                return !this.Value.AbsoluteUri.Remove(0, potentialParent.Value.AbsoluteUri.Length + 1).Contains("/");
            }
            return false;
        }

        internal bool IsRoot()
        {
            return !this.Value.AbsoluteUri.Remove(0,8).Contains("/");

        }

        /// <summary>
        /// Checks wheather the passed job is a hierarchical job
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="jobid">The jobid.</param>
        /// <returns></returns>
        public static bool TryParse(string value, out JobID jobid)
        {
            Uri uriValue;
            if ((!Uri.TryCreate(value, UriKind.Absolute, out uriValue) || !value.StartsWith("jobid://")) || value.StartsWith("GroupID"))
            {
                jobid = null;
                return false;
            }
            else
            {
                jobid = new JobID(value);
                return true;
            }
        }

        /// <summary>
        /// Determines whether the specified customer job id belong to a group head.
        /// </summary>
        /// <param name="customerJobID">The customer job ID.</param>
        /// <returns>
        ///   <c>true</c> if [is group head] [the specified customer job ID]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsGroupHead(string customerJobID)
        {
            Uri uriValue;

            var isUri = Uri.TryCreate(customerJobID, UriKind.Absolute, out uriValue);
            if (isUri && customerJobID.StartsWith("GroupID"))
            {
                var segments = customerJobID.Split('/');
                return (segments.Length == 3 && !string.IsNullOrWhiteSpace(segments[2]));
            }
            return false;
        }

        /// <summary>
        /// Checks wheather a job belongs to a group and supplies groupName and jobName
        /// </summary>
        /// <param name="customerJobID">The customer job ID.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <param name="jobName">Name of the job.</param>
        /// <returns></returns>
        public static bool TryParseGroup(string customerJobID, out string groupName, out string jobName)
        {
            Uri uriValue;
            groupName = string.Empty;
            jobName = string.Empty;

            var isUri = Uri.TryCreate(customerJobID, UriKind.Absolute, out uriValue); 
            if (isUri && customerJobID.StartsWith("GroupID"))
            {
                var segments = customerJobID.Split('/');
                if (segments.Length == 4)
                {
                    groupName = segments[2];
                    jobName = segments[3];
                    if (!string.IsNullOrWhiteSpace(groupName) && !string.IsNullOrWhiteSpace(jobName))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Creates the child job.
        /// </summary>
        /// <param name="childSegment">The child segment.</param>
        /// <returns></returns>
        public JobID CreateChildJob(string childSegment)
        {
            return new JobID(new Uri(this.Value, childSegment));
        }
    }
}
