//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Tests
{
    using System;
    using System.Text;
    using Microsoft.EMIC.Cloud.Security;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
     [TestClass]
    public class SecurityTests
    {
         [TestMethod]
         public void SymmetricKeyCookieTransformTest()
         {
             byte[] masterKey = Convert.FromBase64String("cHz9DpC42rR+oeWI1y6YqCNcFJFieKjU/O3tPcCHDfE=");
             var random = new Random();
             Action<byte[], byte[], Action> ensureEquality = (a, b, failure) =>
             {
                 if (a.Length != b.Length)
                     failure();
                 for (int i = 0; i < a.Length; i++)
                     if (a[i] != b[i])
                         failure();
             };

             for (int i = 0; i < 10; i++)
             {
                 const int maxLen = 1024*512;
                 byte[] plain = new byte[random.Next(maxLen)];
                 random.NextBytes(plain);

                 var p = new SymmetricKeyCookieTransform(masterKey);
                 byte[] cipher = p.Encode(plain);
                 byte[] decoded = p.Decode(cipher);

                 ensureEquality(plain, decoded, () =>
                 {
                     throw new Exception(
                         string.Format("End-to-end wrap failed"));
                 });
             }
         }
    }
}
