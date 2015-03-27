//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Microsoft.EMIC.Cloud.Security
{
    using System;
    using System.IO;
    using System.Security.Authentication;
    using System.Security.Cryptography;
    using Microsoft.IdentityModel.Web;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class SymmetricKeyCookieTransform : CookieTransform
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SymmetricKeyCookieTransform"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        public SymmetricKeyCookieTransform(byte[] key) 
        {
            if (key == null) throw new ArgumentNullException("key");
            _masterKey = key;
            _cipher = Aes.Create;
            _hmac = HMACSHA256.Create;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SymmetricKeyCookieTransform"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="cipher">The cipher.</param>
        /// <param name="hmac">The hmac.</param>
        public SymmetricKeyCookieTransform(byte[] key, Func<SymmetricAlgorithm> cipher, Func<HMAC> hmac)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (cipher == null) throw new ArgumentNullException("cipher");
            if (hmac == null) throw new ArgumentNullException("hmac");

            _masterKey = key;
            _cipher = cipher;
            _hmac = hmac;
        }

        private byte[] _masterKey;
        private Func<SymmetricAlgorithm> _cipher;
        private Func<HMAC> _hmac;
        private int iterations { get { return 10; } }
        private int saltLen { get { return 8; } }
        private int ivLen { get { return _cipher().IV.Length; } }

        private static readonly Random random = new Random();
        private byte[] GetNewSalt()
        {
            var salt = new byte[saltLen];
            random.NextBytes(salt);
            return salt;
        }

        /// <summary>
        /// Encodes the specified byte array.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public override byte[] Encode(byte[] value)
        {
            SymmetricAlgorithm cipher = _cipher();

            MemoryStream result = new MemoryStream();

            // generate fresh salt
            byte[] salt = GetNewSalt();
            result.Write(salt, 0, salt.Length);

            // derive key
            Rfc2898DeriveBytes deriveBytes = new Rfc2898DeriveBytes(_masterKey, salt, iterations);
            byte[] derivedKey = deriveBytes.GetBytes(cipher.KeySize / 8);
            cipher.Key = derivedKey;

            // generate fresh IV
            cipher.GenerateIV();
            result.Write(cipher.IV, 0, cipher.IV.Length);

            // 'sign' plaintext
            HMAC hmacAlgo = _hmac();
            hmacAlgo.Key = derivedKey;
            byte[] computedHmacBytes = hmacAlgo.ComputeHash(value);
            byte[] toBeSignedOctets = new byte[computedHmacBytes.Length + value.Length];
            Array.Copy(computedHmacBytes, 0, toBeSignedOctets, 0, computedHmacBytes.Length);
            Array.Copy(value, 0, toBeSignedOctets, computedHmacBytes.Length, value.Length);

            var csDecrypt = new CryptoStream(result, cipher.CreateEncryptor(), CryptoStreamMode.Write);
            csDecrypt.Write(toBeSignedOctets, 0, toBeSignedOctets.Length);
            csDecrypt.FlushFinalBlock();

            return result.ToArray();
        }

        /// <summary>
        /// Decodes the specified byte array.
        /// </summary>
        /// <param name="encoded">The encoded.</param>
        /// <returns></returns>
        public override byte[] Decode(byte[] encoded)
        {
            SymmetricAlgorithm cipher = _cipher();

            // load salt
            byte[] salt = new byte[saltLen];
            Array.Copy(encoded, 0, salt, 0, salt.Length);

            // derive key
            var deriveBytes = new Rfc2898DeriveBytes(_masterKey, salt, iterations);
            byte[] derivedKey = deriveBytes.GetBytes(cipher.KeySize / 8);
            cipher.Key = derivedKey;

            // load IV
            byte[] iv = new byte[ivLen];
            Array.Copy(encoded, saltLen, iv, 0, ivLen);
            cipher.IV = iv;

            var signedStream = new MemoryStream();
            var csDecrypt = new CryptoStream(signedStream, cipher.CreateDecryptor(), CryptoStreamMode.Write);
            csDecrypt.Write(encoded, saltLen + ivLen, encoded.Length - (saltLen + ivLen));
            csDecrypt.FlushFinalBlock();
            byte[] signedBytes = signedStream.ToArray();

            HMAC hmacAlgo = _hmac();
            hmacAlgo.Key = derivedKey;

            byte[] conveyedHmacBytes = new byte[hmacAlgo.HashSize / 8];
            Array.Copy(signedBytes, 0, conveyedHmacBytes, 0, conveyedHmacBytes.Length);
            byte[] plaintextBytes = new byte[signedStream.Length - conveyedHmacBytes.Length];
            Array.Copy(signedBytes, conveyedHmacBytes.Length, plaintextBytes, 0, signedStream.Length - conveyedHmacBytes.Length);
            byte[] computedHmacBytes = hmacAlgo.ComputeHash(plaintextBytes);

            Action<byte[], byte[], Action> ensureEquality = (a, b, failure) =>
            {
                if (a.Length != b.Length) 
                    failure();
                for (int i = 0; i < a.Length; i++) 
                    if (a[i] != b[i]) 
                        failure();
            };

            ensureEquality(conveyedHmacBytes, computedHmacBytes, () =>
            {
                throw new InvalidCredentialException(
                    string.Format("Session token is not correctly signed"));
            });

            return plaintextBytes;
        }
    }
}
