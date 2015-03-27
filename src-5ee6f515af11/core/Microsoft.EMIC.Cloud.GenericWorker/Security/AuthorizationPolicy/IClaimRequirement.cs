//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Microsoft.EMIC.Cloud.Security.AuthorizationPolicy
{
    using System.Collections.Generic;
    using System.Xml.Linq;
    using Microsoft.IdentityModel.Claims;

    /// <summary>
    /// A requirement that a certain security claim or condition is satisfied. 
    /// 
    /// Basic building block for a <see cref="ClaimRequirementPolicy"/>. 
    /// </summary>
    public interface IClaimRequirement
    {
        /// <summary>
        /// Determines whether the  <see cref="IClaimRequirement"/> is satisfied by the provided claims collection. 
        /// </summary>
        /// <param name="claims"></param>
        /// <returns></returns>
        bool IsSatisfiedBy(IEnumerable<Claim> claims);

        /// <summary>
        /// Serializes the <see cref="IClaimRequirement"/> into an <see cref="XElement"/>.
        /// </summary>
        /// <returns></returns>
        XElement Serialize();

        /// <summary>
        /// Initializes an  <see cref="IClaimRequirement"/> from an <see cref="XElement"/>.
        /// </summary>
        /// <param name="element"></param>
        void Load(XElement element);
    }
}
