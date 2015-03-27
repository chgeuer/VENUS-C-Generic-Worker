//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.Xml.Linq;

namespace Tests
{
    using System;
    using Microsoft.EMIC.Cloud.Security.AuthorizationPolicy;
    using Microsoft.IdentityModel.Claims;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [TestClass]
    public class AuthorizationPolicyTests
    {
        Claim a = new Claim(ClaimTypes.Role, "Administrator");
        Claim b = new Claim(ClaimTypes.Role, "Researcher");
        Claim c = new Claim(ClaimTypes.Name, "Jim");
        Claim d = new Claim(ClaimTypes.Name, "Alice");
        Claim e = new Claim(ClaimTypes.Name, "Bob");

        [TestMethod]
        public void AuthorizationPolicySerializationTest()
        {
            ClaimRequirementPolicy originalpolicy = new ClaimRequirementPolicy
                                                {
                                                    { "a && ( b || c )", a.And(b.Or(c)) },
                                                    { "a &&  b && c", a.And(b.And(c)) }
                                                };


            var wrapped = originalpolicy.Serialize().ToString();
            Console.WriteLine(wrapped);

            var policy = new ClaimRequirementPolicy(XElement.Parse(wrapped));

            Assert.IsTrue(policy.IsSatisfiedBy("a && ( b || c )", a, b, c));
            Assert.IsTrue(policy.IsSatisfiedBy("a && ( b || c )", a, b));
            Assert.IsTrue(policy.IsSatisfiedBy("a && ( b || c )", a, c));
            Assert.IsFalse(policy.IsSatisfiedBy("a && ( b || c )", b, c));
            Assert.IsFalse(policy.IsSatisfiedBy("a && ( b || c )", a));

            Assert.IsFalse(policy.IsSatisfiedBy("a &&  b && c", a));
            Assert.IsFalse(policy.IsSatisfiedBy("a &&  b && c", a, b));
            Assert.IsTrue(policy.IsSatisfiedBy("a &&  b && c", a, b, c));

            var x = a.And(b).And(new AndClaimRequirement(a.AsRequirement(), b.AsRequirement(), c.AsRequirement(), e.AsRequirement())).Serialize().ToString();
            Console.WriteLine(x);
            var y = a.Or(b).Or(new OrClaimRequirement(a.AsRequirement(), b.AsRequirement(), c.AsRequirement(), e.AsRequirement())).Serialize().ToString();
            Console.WriteLine(y);



            Console.WriteLine(originalpolicy["a && ( b || c )"].ToString());
            Console.WriteLine(originalpolicy["a &&  b && c"].ToString());
        }
    }
}
