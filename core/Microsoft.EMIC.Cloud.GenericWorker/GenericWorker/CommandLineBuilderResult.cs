//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Microsoft.EMIC.Cloud.GenericWorker
{
    /// <summary>
    /// Data transfer object for the <see cref="CommandLineBuilder"/>. 
    /// </summary>
    public class CommandLineBuilderResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineBuilderResult"/> class.
        /// </summary>
        public CommandLineBuilderResult() { }

        /// <summary>
        /// Gets or sets a value whether the commandline building succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the name of executable.
        /// </summary>
        public string Executable { get; set; }

        /// <summary>
        /// Gets or sets the compounded commandline arguments string.
        /// </summary>
        public string CommandLineArgs { get; set; }

        /// <summary>
        /// Gets or sets the working directory.
        /// </summary>
        /// <value>
        /// The working directory.
        /// </value>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Gets or sets the error description.
        /// </summary>
        /// <value>
        /// The error description.
        /// </value>
        public string ErrorDescription { get; set; }

        /// <summary>
        /// Gets or sets the transferedbytes
        /// </summary>
        /// <value>
        /// The error description.
        /// </value>
        public long TransferredBytes { get; set; }

        /// <summary>
        /// Gets or sets the number of files transfered
        /// </summary>
        /// <value>
        /// The error description.
        /// </value>
        public int numberOfFilesTransfered { get; set; }

        /// <summary>
        /// Returns the error description (if any) or the values of this instance.
        /// </summary>
        public override string ToString()
        {
            if (!Success)
            {
                return ErrorDescription;
            }

            return string.Format("Working directory \"{0}\":> Executable \"{1}\" Args \"{2}\"",
                WorkingDirectory, Executable, CommandLineArgs);
        }
    }
}