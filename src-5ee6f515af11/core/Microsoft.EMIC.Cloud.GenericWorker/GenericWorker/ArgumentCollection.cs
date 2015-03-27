//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.EMIC.Cloud.DataManagement;
using Microsoft.EMIC.Cloud.ApplicationRepository;

namespace Microsoft.EMIC.Cloud.GenericWorker
{
    /// <summary>
    /// Contains a collection of all arguments
    /// </summary>
    [DataContract]
    public class ArgumentCollection : List<IArgument>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ArgumentCollection"/> class.
        /// </summary>
        public ArgumentCollection() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArgumentCollection"/> class.
        /// </summary>
        /// <param name="collection">The collection.</param>
        public ArgumentCollection(IEnumerable<IArgument> collection) : base(collection) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArgumentCollection"/> class.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        public ArgumentCollection(int capacity) : base(capacity) { }

        /// <summary>
        /// Gets the element at the specified index.
        /// </summary>
        /// <returns>
        /// The element at the specified index or null.
        /// </returns>
        ///   
        public IArgument this[string name]
        {
            get
            {
                return this.Where(a => string.Equals(a.Name, name)).FirstOrDefault();
            }
        }

        /// <summary>
        /// Determines whether the collections contains the specified commandline argument.
        /// </summary>
        /// <param name="cla">The commandline argument</param>
        /// <returns>
        ///   <c>true</c> if the collections contains the commandline argument; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(CommandLineArgument cla)
        {
            return this[cla.Name] != null;
        }
    }
}