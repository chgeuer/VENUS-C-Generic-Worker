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
    /// An <see cref="IClaimRequirement"/> which requires that all of the inner <see cref="IClaimRequirement"/>s are satisfied. 
    /// </summary>
    public class AndClaimRequirement : IClaimRequirement
    {
        private IEnumerable<IClaimRequirement> _all;
        /// <summary>
        /// Gets the list of all <see cref="IClaimRequirement"/>s in this instance.
        /// </summary>
        public IEnumerable<IClaimRequirement> All
        {
            get { return _all; }
            private set { _all = Simplify(value); }
        }

        private static IEnumerable<IClaimRequirement> Simplify(IEnumerable<IClaimRequirement> value)
        {
            if (value == null) throw new ArgumentNullException("value");

            var composition = new List<IClaimRequirement>();

            Func<IClaimRequirement, bool> simplifiable = v => v is AndClaimRequirement;
            Func<IClaimRequirement, IEnumerable<IClaimRequirement>> extract = a => ((AndClaimRequirement) a).All;

            foreach (var v in value)
            {
                if (simplifiable(v))
                {
                    composition.AddRange(Simplify(extract(v)));
                }
                else
                {
                    composition.Add(v);
                }
            }

            return composition;
        }

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AndClaimRequirement"/> class.
        /// </summary>
        /// <param name="allClaims">All claims.</param>
        public AndClaimRequirement(IEnumerable<IClaimRequirement> allClaims)
        {
            if (allClaims == null) throw new ArgumentNullException("allClaims");
            All = allClaims;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AndClaimRequirement"/> class.
        /// </summary>
        /// <param name="allClaims">All claims.</param>
        public AndClaimRequirement(params IClaimRequirement[] allClaims)
        {
            if (allClaims == null) throw new ArgumentNullException("allClaims");
            All = allClaims;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AndClaimRequirement"/> class.
        /// </summary>
        /// <param name="anyClaims">Any claims.</param>
        public AndClaimRequirement(params Claim[] anyClaims)
        {
            if (anyClaims == null) throw new ArgumentNullException("anyClaims");

            All = anyClaims.Select(claim => claim.AsRequirement());
        }

        internal AndClaimRequirement(string serialized)
        {
            if (string.IsNullOrEmpty(serialized)) throw new ArgumentNullException("serialized");

            this.Load(XElement.Parse(serialized));
        }

        internal AndClaimRequirement(XElement element)
        {
            if (element == null) throw new ArgumentNullException("element");
            this.Load(element);
        }

        #endregion

        #region IClaimRequirement members

        /// <summary>
        /// Determines whether the  <see cref="IClaimRequirement"/> is satisfied by the provided claims collection.
        /// </summary>
        /// <param name="claims"></param>
        /// <returns></returns>
        public bool IsSatisfiedBy(IEnumerable<Claim> claims)
        {
            if (claims == null) throw new ArgumentNullException("claims");
            return !All.Any(claim => !claim.IsSatisfiedBy(claims));
        }

        /// <summary>
        /// Serializes the <see cref="IClaimRequirement"/> into an <see cref="XElement"/>.
        /// </summary>
        /// <returns></returns>
        public XElement Serialize()
        {
            return new XElement(ClaimRequirementPolicy.N.AndElem, this.All.Select(x => x.Serialize()));
        }

        /// <summary>
        /// Initializes an  <see cref="IClaimRequirement"/> from an <see cref="XElement"/>.
        /// </summary>
        /// <param name="element"></param>
        public void Load(XElement element)
        {
            if (element == null) throw new ArgumentNullException("element");
            All = element.Elements().Select(ClaimRequirementPolicy.LoadFromElement);
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
            const string separator = " and ";

            var sb = new StringBuilder();

            sb.Append("( ");

            var claims = All.ToList();

            for (int index = 0; index < claims.Count; index++)
            {
                var claimRequirement = claims[index];

                sb.Append(claimRequirement.ToString());
                if (index < claims.Count - 1)
                {
                    sb.Append(separator);
                }
            }

            sb.Append(" )");

            return sb.ToString();
        }
    }
}
