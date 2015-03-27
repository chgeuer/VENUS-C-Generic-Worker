//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;

namespace Microsoft.EMIC.Cloud.Security.AuthorizationPolicy
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.IdentityModel.Claims;

    /// <summary>
    /// Stores <see cref="IClaimRequirement"/> policies for different resources.
    /// </summary>
    public class ClaimRequirementPolicy : Dictionary<string, IClaimRequirement>
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ClaimRequirementPolicy"/> class.
        /// </summary>
        public ClaimRequirementPolicy() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClaimRequirementPolicy"/> class.
        /// </summary>
        /// <param name="serialized">The serialized.</param>
        public ClaimRequirementPolicy(string serialized)
        {
            if (string.IsNullOrEmpty(serialized)) throw new ArgumentNullException("serialized");
            this.Load(XElement.Parse(serialized));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClaimRequirementPolicy"/> class.
        /// </summary>
        /// <param name="element">The element.</param>
        public ClaimRequirementPolicy(XElement element)
        {
            if (element == null) throw new ArgumentNullException("element");
            this.Load(element);
        }

        #endregion

        internal bool IsSatisfiedBy(string operation, IEnumerable<Claim> claims)
        {
            if (!this.ContainsKey(operation))
            {
                return false;
            }

            return this[operation].IsSatisfiedBy(claims);
        }

        /// <summary>
        /// Serializes this instance.
        /// </summary>
        /// <returns></returns>
        public XElement Serialize()
        {
            Func<string, IClaimRequirement, XElement> resourcePolicy = (resource, policy) => new XElement(N.ClaimPolicyResourcePolicyElem, 
                new XAttribute(N.ResourceAttr, resource), policy.Serialize());

            return new XElement(N.ClaimPolicyElemlem, this.Select(pe => resourcePolicy(pe.Key, pe.Value)));
        }

        /// <summary>
        /// Loads the specified element.
        /// </summary>
        /// <param name="element">The element.</param>
        private void Load(XElement element)
        {
            if (element == null) throw new ArgumentNullException("element");

            var desirialized = element.Elements(N.ClaimPolicyResourcePolicyElem).ToDictionary(
                e => e.Attribute(N.ResourceAttr).Value,
                e => LoadFromElement(e.Elements().First()));

            desirialized.ToList().ForEach(e => this.Add(e.Key, e.Value));

        }

        internal static IClaimRequirement LoadFromElement(XElement element)
        {
            if (element == null) throw new ArgumentNullException("element");

            if (N.AndElem.Equals(element.Name))
                return new AndClaimRequirement(element);
            
            if (N.OrElem.Equals(element.Name))
                return new OrClaimRequirement(element);
            
            return new WrappedClaim(element);
        }

        internal static class N
        {
            internal static readonly XName ClaimPolicyElemlem = XName.Get("ClaimRequirementsPolicy");
            internal static readonly XName ClaimPolicyResourcePolicyElem = XName.Get("Policy");
            internal static readonly XName ResourceAttr = XName.Get("Operation");
            internal static readonly XName ClaimElem = XName.Get("Claim");
            internal static readonly XName ClaimTypeAttr = XName.Get("Type");
            internal static readonly XName OrElem = XName.Get("Any");
            internal static readonly XName AndElem = XName.Get("All");
        }
    }
}
