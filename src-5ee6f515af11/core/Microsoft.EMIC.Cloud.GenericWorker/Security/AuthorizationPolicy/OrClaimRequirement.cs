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
    /// An <see cref="IClaimRequirement"/> which requires that at least one of the inner <see cref="IClaimRequirement"/>s is satisfied. 
    /// </summary>
    public class OrClaimRequirement : IClaimRequirement
    {
        private IEnumerable<IClaimRequirement> _any;
        /// <summary>
        /// Gets or sets the claim requirement list 
        /// </summary>
        public IEnumerable<IClaimRequirement> Any
        {
            get { return _any; }
            private set { _any = Simplify(value); }
        }

        private static IEnumerable<IClaimRequirement> Simplify(IEnumerable<IClaimRequirement> value)
        {
            if (value == null) throw new ArgumentNullException("value");

            var composition = new List<IClaimRequirement>();

            Func<IClaimRequirement, bool> simplifiable = a => a is OrClaimRequirement;
            Func<IClaimRequirement, IEnumerable<IClaimRequirement>> extract = a => ((OrClaimRequirement)a).Any;

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
        /// Initializes a new instance of the <see cref="OrClaimRequirement"/> class.
        /// </summary>
        /// <param name="anyClaims">Any claims.</param>
        public OrClaimRequirement(IEnumerable<IClaimRequirement> anyClaims)
        {
            if (anyClaims == null) throw new ArgumentNullException("anyClaims");
            Any = anyClaims;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrClaimRequirement"/> class.
        /// </summary>
        /// <param name="anyClaims">Any claims.</param>
        public OrClaimRequirement(params IClaimRequirement[] anyClaims)
        {
            if (anyClaims == null) throw new ArgumentNullException("anyClaims");
            Any = anyClaims;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrClaimRequirement"/> class.
        /// </summary>
        /// <param name="anyClaims">Any claims.</param>
        public OrClaimRequirement(params Claim[] anyClaims)
        {
            if (anyClaims == null) throw new ArgumentNullException("anyClaims");

            Any = anyClaims.Select(claim => claim.AsRequirement());
        }

        internal OrClaimRequirement(string serialized)
        {
            if (string.IsNullOrEmpty(serialized)) throw new ArgumentNullException("serialized");
            this.Load(XElement.Parse(serialized));
        }

        internal OrClaimRequirement(XElement element)
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
            return Any.Any(claim => claim.IsSatisfiedBy(claims));
        }

        /// <summary>
        /// Serializes the <see cref="IClaimRequirement"/> into an <see cref="XElement"/>.
        /// </summary>
        /// <returns></returns>
        public XElement Serialize()
        {
            return new XElement(ClaimRequirementPolicy.N.OrElem, this.Any.Select(x => x.Serialize()));
        }

        /// <summary>
        /// Initializes an  <see cref="IClaimRequirement"/> from an <see cref="XElement"/>.
        /// </summary>
        /// <param name="element"></param>
        public void Load(XElement element)
        {
            if (element == null) throw new ArgumentNullException("element");
            Any = element.Elements().Select(ClaimRequirementPolicy.LoadFromElement);
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
            const string separator = " or ";

            var sb = new StringBuilder();

            sb.Append("( ");

            var claims = Any.ToList();

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