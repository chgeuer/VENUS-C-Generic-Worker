//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.EMIC.Cloud.DataManagement;
using System.Threading.Tasks;

namespace Microsoft.EMIC.Cloud.DataManagement
{

    /// <summary>
    /// Represents a collection of Reference instances( references to an input/output file).
    /// </summary>
    public class ReferenceCollection : List<Reference> 
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceCollection"/> class.
        /// </summary>
        public ReferenceCollection() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceCollection"/> class.
        /// </summary>
        /// <param name="collection">The collection.</param>
        public ReferenceCollection(IEnumerable<Reference> collection) : base(collection) { }

        /// <summary>
        /// Gets all the references of the reference array as a list of single references.
        /// </summary>
        /// <returns></returns>
        public List<SingleReference> GetSingleReferenceList(string name="")
        {
            var srl = new List<SingleReference>();
            int i = 0;
            foreach (var reference in this)
            {
                srl.Add(new SingleReference
                {
                    Name = name + i,
                    Ref = reference
                });
                i++;
            }
            return srl;
        }
    }

    /// <summary>
    /// Represents a collection of Reference arguments on the commandline without a priori known element count.
    /// </summary>
    [ArgumentExport(
    LocalName = LocalName,
    NamespaceURI = IArgumentExtensions.GenericWorkerNamespace,
    Type = typeof(ReferenceArray))]
    public class ReferenceArray : IReferenceArgument
    {
        internal const string LocalName = "ReferenceArray";
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the references.
        /// </summary>
        public ReferenceCollection References { get; set; }

        ArgumentRepository argumentRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceArray"/> class.
        /// </summary>
        public ReferenceArray() 
        {
            References = new ReferenceCollection();
        }

        /// <summary>
        /// Adds the reference.
        /// </summary>
        /// <param name="refr">The refr.</param>
        private void AddReference(Reference refr)
        {
            if (refr == null)
                throw new ArgumentException(ExceptionMessages.NeedReferenceObject);
            References.Add(refr);
        }

        /// <summary>
        /// Loads the state the of the object out of an xml source.
        /// </summary>
        /// <param name="xmlElement">The XML source.</param>
        /// <param name="argumentRepository">The argument repository.</param>
        public void LoadContents(XmlElement xmlElement, ArgumentRepository argumentRepository)
        {
            this.argumentRepository = argumentRepository;

            Name = xmlElement.Attributes["Name"].Value;
            References = new ReferenceCollection();
            int numReferences = xmlElement.GetElementsByTagName("Reference").Count;
            if (numReferences <= 0)
                throw new ArgumentException(ExceptionMessages.AtLeastOneReference);
            for (int i = 0; i < numReferences; i++)
            {
                XmlElement childElement = (XmlElement)xmlElement.GetElementsByTagName("Reference").Item(i);
                Reference Ref = new Reference();
                Ref.LoadContents(childElement);
                References.Add(Ref);
            }
        }

        /// <summary>
        /// Gets all the references of the reference array as a list of single references.
        /// </summary>
        /// <returns></returns>
        public List<SingleReference> GetSingleReferenceList()
        {
            return this.References.GetSingleReferenceList(this.Name);
        }

        /// <summary>
        /// Serializes the object to the specified document.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <returns></returns>
        public XmlElement Serialize(XmlDocument doc)
        {
            var e = doc.CreateElement(LocalName,
                IArgumentExtensions.GenericWorkerNamespace);

            Action<string, string> add = (name, value) =>
            {
                var a = doc.CreateAttribute(name);
                a.Value = value;
                e.Attributes.Append(a);
            };

            add("Name", Name);

            foreach (var reference in References)
            {
                var RefEl = reference.Serialize(doc);
                e.AppendChild(RefEl);
            }
            return e;
        }

        /// <summary>
        /// Downloads all references specified by the reference array to the specified working directory.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="tokenSource">The token source, which allows to cancell all downloads</param>
        public void Download(string workingDirectory, System.Threading.CancellationTokenSource tokenSource)
        {
            var downloadReferences = this.GetSingleReferenceList();
            Parallel.ForEach(downloadReferences, reference => reference.Download(workingDirectory,tokenSource));
        }

        /// <summary>
        /// Uploads all references specified by the reference array out of the specified working directory.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        public void Upload(string workingDirectory)
        {
            var uploadReferences = this.GetSingleReferenceList();
            Parallel.ForEach(uploadReferences, reference => reference.Upload(workingDirectory));
        }

        /// <summary>
        /// This is not implemented.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <returns></returns>
        public string GetFileLocation(string workingDirectory)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks whether all references specified by the reference array exist.
        /// </summary>
        /// <returns></returns>
        public bool ExistsDataItem()
        {
            return this.GetSingleReferenceList().All(sr => sr.ExistsDataItem());
        }
    }

}
