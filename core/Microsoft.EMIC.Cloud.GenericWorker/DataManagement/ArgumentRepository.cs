//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Microsoft.EMIC.Cloud.DataManagement
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Linq;
    using System.Xml;

    /// <summary>
    /// A factory for <see cref="IArgument"/> objects. 
    /// </summary>
    [Export]
    public class ArgumentRepository
    {
        /// <summary>
        /// Gets or sets the arguments.
        /// </summary>
        /// <value>
        /// The arguments.
        /// </value>
        [ImportMany(typeof(IProviderSpecificReference))]
        public IEnumerable<Lazy<IProviderSpecificReference, IProviderSpecificReferenceMetadata>> Arguments { get; set; }

        /// <summary>
        /// Loads the specified XmlSerialized into an IArgument implementation.
        /// </summary>
        /// <param name="e">The serialized argument</param>
        /// <returns>an IArgument implementation</returns>
        public IArgument Load(XmlElement e)
        {
            if (e.LocalName == LiteralArgument.LocalName)
            {
                var lit = new LiteralArgument();
                lit.LoadContents(e, this);
                return lit;
            }
            else if (e.LocalName == SingleReference.LocalName)
            {
                var sr = new SingleReference();
                sr.LoadContents(e, this);
                IProviderSpecificReference dataRef = this.LoadRef(e.FirstChild.FirstChild as XmlElement);
                sr.Ref.ProviderSpecificReference = dataRef;

                return sr;
            }
            else if (e.LocalName == ReferenceArray.LocalName)
            {
                var ra = new ReferenceArray();
                ra.LoadContents(e, this);
                int refNr = 0;
                foreach (var r in ra.References)
                {
                    var dataRef = this.LoadRef(e.ChildNodes.Item(refNr).FirstChild as XmlElement);
                    r.ProviderSpecificReference = dataRef;
                    refNr++;
                }

                return ra;
            }
            else if (e.LocalName == SwitchArgument.LocalName){
                var sa = new SwitchArgument();
                sa.LoadContents(e, this);
                return sa;
            }
            else if (e.LocalName == "AzureArgSingleReference")
            {
                throw new NotSupportedException(ExceptionMessages.AzureArgSingleReferenceChanged);
            }

            throw new NotSupportedException(string.Format(
                ExceptionMessages.NoImplementationToLoad, e.NamespaceURI, e.LocalName));
        }

        internal Reference LoadRefEl(XmlElement e)
        {
            var result = new Reference();
            result.LoadContents(e);
            result.ProviderSpecificReference=LoadRef((XmlElement)e.FirstChild);

            return result;
        }

        internal IProviderSpecificReference LoadRef(XmlElement e)
        {
            Type createdType = this.Arguments
                   .Where(arg => string.Equals(arg.Metadata.LocalName, e.LocalName) &&
                       string.Equals(arg.Metadata.NamespaceURI, e.NamespaceURI))
                   .Select(arg => arg.Metadata.Type).FirstOrDefault();
            if (createdType == null)
            {
                throw new NotSupportedException(string.Format(
                    ExceptionMessages.CanNotLocateType, e.NamespaceURI, e.LocalName));
            }

            var result = Activator.CreateInstance(createdType) as IProviderSpecificReference;
            if (result == null)
            {
                throw new NotSupportedException(string.Format(
                    ExceptionMessages.CanNotCreateInstance, createdType.FullName));
            }
            result.LoadContents(e); 

            return result;
        }
    }
}