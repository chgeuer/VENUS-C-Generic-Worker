//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.Text;

namespace Microsoft.EMIC.Cloud.Security.AuthorizationPolicy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.IdentityModel.Claims;

    /// <summary>
    /// An <see cref="IClaimRequirement"/> which requires that the wrapped claim is satisfied. 
    /// </summary>
    public class WrappedClaim : IClaimRequirement
    {
        /// <summary>
        /// Gets or sets the claim.
        /// </summary>
        /// <value>
        /// The claim.
        /// </value>
        public Claim Claim { get; set; }

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="WrappedClaim"/> class.
        /// </summary>
        /// <param name="claim">The claim.</param>
        public WrappedClaim(Claim claim)
        {
            if (claim == null) throw new ArgumentNullException("claim");
            Claim = claim;
        }

        internal WrappedClaim(string serialized)
        {
            if (serialized == null) throw new ArgumentNullException("serialized");
            this.Load(XElement.Parse(serialized));
        }

        internal WrappedClaim(XElement element)
        {
            if (element == null) throw new ArgumentNullException("element");
            this.Load(element);
        }

        #endregion

        #region IClaimRequirement members

        /// <summary>
        /// Determines whether the  <see cref="IClaimRequirement"/> is satisfied by the provided claims collection.
        /// </summary>
        /// <param name="claims">List of claims</param>
        /// <returns></returns>
        public bool IsSatisfiedBy(IEnumerable<Claim> claims)
        {
            if (claims == null) throw new ArgumentNullException("claims");
            var matchingClaim = claims.Where(
                c => string.Equals(c.ClaimType, this.Claim.ClaimType) && string.Equals(c.Value, this.Claim.Value)).
                FirstOrDefault();

            return matchingClaim != null;
        }

        /// <summary>
        /// Serializes the <see cref="IClaimRequirement"/> into an <see cref="XElement"/>.
        /// </summary>
        /// <returns></returns>
        public XElement Serialize()
        {
            Func<Claim, XAttribute> claimtype = c => new XAttribute(ClaimRequirementPolicy.N.ClaimTypeAttr, c.ClaimType);
            Func<Claim, XText> claimvalue = c => new XText(c.Value);
            Func<Claim, XElement> claimAsElem = c => new XElement(ClaimRequirementPolicy.N.ClaimElem, claimtype(c), claimvalue(c));

            return claimAsElem(this.Claim);
        }

        /// <summary>
        /// Initializes an  <see cref="IClaimRequirement"/> from an <see cref="XElement"/>.
        /// </summary>
        /// <param name="element"></param>
        public void Load(XElement element)
        {
            if (element == null) throw new ArgumentNullException("element");
            this.Claim = new Claim(element.Attribute(ClaimRequirementPolicy.N.ClaimTypeAttr).Value, ((XText)element.FirstNode).Value);
        }

        #endregion

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            string ct = Claim.ClaimType;
            int idx = ct.LastIndexOf("/") + 1;
            if (idx < ct.Length)
            {
                ct = ct.Substring(idx);
            }
 
            return string.Format("{0}=\"{1}\"", ct, Claim.Value);
        }
    }
}
