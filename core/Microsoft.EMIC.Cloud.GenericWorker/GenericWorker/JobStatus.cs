//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Microsoft.EMIC.Cloud.GenericWorker
{
    /// <summary>
    /// The execution status of an <see cref="IJob"/>.
    /// </summary>
    public enum JobStatus
    {
        /// <summary>
        /// The job is in the system and available for processing. 
        /// </summary>
        Submitted = 0,

        /// <summary>
        /// The job is ready to be processed, and a worker node checks whether all necessary input data is available. 
        /// </summary>
        CheckingInputData = 1,

        /// <summary>
        /// The job is actively processed by a worker. 
        /// </summary>
        Running = 2,

        /// <summary>
        /// The job execution finished successfully. 
        /// </summary>
        Finished = 3,

        /// <summary>
        /// The job execution failed. 
        /// </summary>
        Failed = 4,

        /// <summary>
        /// The user requested to stop/kill the job. 
        /// </summary>
        CancelRequested = 5,

        /// <summary>
        /// The job execution was stopped due to a user request. 
        /// </summary>
        Cancelled = 6
    }
}