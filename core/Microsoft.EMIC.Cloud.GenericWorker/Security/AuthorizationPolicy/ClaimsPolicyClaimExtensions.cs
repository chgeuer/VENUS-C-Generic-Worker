//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Microsoft.EMIC.Cloud.Security.AuthorizationPolicy
{
    using Microsoft.IdentityModel.Claims;

    /// <summary>
    /// Extension methods to provide simpler C# syntax on <see cref="IClaimRequirement"/>s. 
    /// </summary>
    public static class ClaimsPolicyClaimExtensions
    {
        /// <summary>
        /// Creates a claim requirement instance from a claim.
        /// </summary>
        /// <param name="claim">The claim.</param>
        /// <returns></returns>
        public static IClaimRequirement AsRequirement(this Claim claim)
        {
            return new WrappedClaim(claim);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AndClaimRequirement"/> class from the specified claims
        /// </summary>
        /// <param name="a">Claim A.</param>
        /// <param name="b">Claim B.</param>
        /// <returns></returns>
        public static IClaimRequirement And(this Claim a, Claim b)
        {
            return new AndClaimRequirement(a.AsRequirement(), b.AsRequirement());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AndClaimRequirement"/> class from the specified claim and claim requirement
        /// </summary>
        /// <param name="a">Claim A.</param>
        /// <param name="b">Claim requirement B.</param>
        /// <returns></returns>
        public static IClaimRequirement And(this Claim a, IClaimRequirement b)
        {
            return new AndClaimRequirement(a.AsRequirement(), b);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AndClaimRequirement"/> class from the specified claim and claim requirement
        /// </summary>
        /// <param name="a">Claim requirement A.</param>
        /// <param name="b">Claim B.</param>
        /// <returns></returns>
        public static IClaimRequirement And(this IClaimRequirement a, Claim b)
        {
            return new AndClaimRequirement(a, b.AsRequirement());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AndClaimRequirement"/> class from the specified claim requirements
        /// </summary>
        /// <param name="a">Claim Requirement A.</param>
        /// <param name="b">Claim Requirement B.</param>
        /// <returns></returns>
        public static IClaimRequirement And(this IClaimRequirement a, IClaimRequirement b)
        {
            return new AndClaimRequirement(a, b);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrClaimRequirement"/> class from the specified claims
        /// </summary>
        /// <param name="a">Claim A.</param>
        /// <param name="b">Claim B.</param>
        /// <returns></returns>
        public static IClaimRequirement Or(this Claim a, Claim b)
        {
            return new OrClaimRequirement(a.AsRequirement(), b.AsRequirement());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrClaimRequirement"/> class from the specified claim and claim requirement
        /// </summary>
        /// <param name="a">Claim A.</param>
        /// <param name="b">Claim requirement B.</param>
        /// <returns></returns>
        public static IClaimRequirement Or(this Claim a, IClaimRequirement b)
        {
            return new OrClaimRequirement(a.AsRequirement(), b);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrClaimRequirement"/> class from the specified claim and claim requirement
        /// </summary>
        /// <param name="a">Claim requirement A.</param>
        /// <param name="b">Claim B.</param>
        /// <returns></returns>
        public static IClaimRequirement Or(this IClaimRequirement a, Claim b)
        {
            return new OrClaimRequirement(a, b.AsRequirement());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrClaimRequirement"/> class from the specified claim requirements
        /// </summary>
        /// <param name="a">Claim Requirement A.</param>
        /// <param name="b">Claim Requirement B.</param>
        /// <returns></returns>
        public static IClaimRequirement Or(this IClaimRequirement a, IClaimRequirement b)
        {
            return new OrClaimRequirement(a, b);
        }

        /// <summary>
        /// Determines whether the specified claim requirement is satisfied by the specified claims.
        /// </summary>
        /// <param name="claimRequirement">The claim requirement.</param>
        /// <param name="claims">The claims.</param>
        /// <returns>
        ///   <c>true</c> if the specified claim requirement is satisfied by the specified claims; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsSatisfiedBy(this IClaimRequirement claimRequirement, params Claim[] claims)
        {
            return claimRequirement.IsSatisfiedBy(claims);
        }

        /// <summary>
        /// Determines whether the specified policy is satisfied by the specified claims and operation.
        /// </summary>
        /// <param name="policy">The policy.</param>
        /// <param name="operation">The operation.</param>
        /// <param name="claims">The claims.</param>
        /// <returns>
        ///   <c>true</c> if the specified policy is satisfied by the specified claims and operation; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsSatisfiedBy(this ClaimRequirementPolicy policy, string operation, params Claim[] claims)
        {
            return policy.IsSatisfiedBy(operation, claims);
        }
    }
}
