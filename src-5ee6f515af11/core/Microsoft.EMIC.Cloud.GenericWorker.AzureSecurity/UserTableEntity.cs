//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using Microsoft.EMIC.Cloud.Security;

namespace Microsoft.EMIC.Cloud.GenericWorker.AzureSecurity
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Collections.ObjectModel;
    using System.Xml.Linq;
    using System.Collections.Specialized;
    using Microsoft.IdentityModel.Claims;

    using Microsoft.WindowsAzure.StorageClient;
    
    /// <summary>
    /// A <see cref="TableServiceEntity"/> containing user account information.
    /// </summary>
    public class UserTableEntity : TableServiceEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserTableEntity"/> class.
        /// </summary>
        public UserTableEntity() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserTableEntity"/> class.
        /// </summary>
        /// <param name="username">The user name.</param>
        public UserTableEntity(string username)
            : base(rowKey: username, partitionKey: string.Empty) { }

        public string Password { get; set; }

        public string SerializedClaims { get; set; }

        public bool IsComputeAdministrator { get; set; }
        public bool IsApplicationRepositoryAdministrator { get; set; }
        public bool IsResearcher { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("User \"{0}\"", this.RowKey);
        }
    }

    public class User
    {
        public UserTableEntity UserTableEntity { get; private set; }
        public string Username { get { return UserTableEntity.RowKey; } }
        public string Password { get { return UserTableEntity.Password; } set { UserTableEntity.Password = value; }  }
        public readonly ObservableCollection<Claim> Claims = new ObservableCollection<Claim>();

        public User(string username, string password, IEnumerable<Claim> claims)
        {
            UserTableEntity = new UserTableEntity(username) { Password = password };
            Claims.CollectionChanged += ClaimCollectionChanged;
            claims.ToList().ForEach(Claims.Add);
        }

        public User(UserTableEntity ute)
        {
            UserTableEntity = ute;
            
            // Initialize ...
            var claims = LoadClaims(UserTableEntity.SerializedClaims);
            claims.ForEach(Claims.Add);

            // ... and ensure we're notified when changes occur
            Claims.CollectionChanged += ClaimCollectionChanged;
        }

        void ClaimCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UserTableEntity.SerializedClaims = SerializeClaims(Claims);
        }

        internal class N
        {
            internal static readonly string claims = "claims";
            internal static readonly string claim = "c";
            internal static readonly string claimtype = "t";
        }

        private string SerializeClaims(IEnumerable<Claim> claims)
        {
            Func<Claim, XAttribute> claimtype = c => new XAttribute(N.claimtype, c.ClaimType);;
            Func<Claim, XText> claimvalue = c => new XText(c.Value);
            Func<Claim, XElement> claimAsElem = c => new XElement(N.claim, claimtype(c), claimvalue(c));
            var claimsElem = new XElement(N.claims, claims.Select(claimAsElem));
            return new XDocument(claimsElem).ToString();
        }

        private  List<Claim> LoadClaims(string serializedClaims)
        {
            if (string.IsNullOrEmpty(serializedClaims))
            {
                return new List<Claim>();
            }

            var doc = XDocument.Parse(serializedClaims);

            return doc.Element(N.claims).Elements(N.claim).Select(
                ce => new Claim(ce.Attribute(N.claimtype).Value, ((XText)ce.FirstNode).Value)).ToList();
        }

        public bool IsApplicationRepositoryAdministrator 
        { 
            get { return this.UserTableEntity.IsApplicationRepositoryAdministrator; }
            set { this.UserTableEntity.IsApplicationRepositoryAdministrator = value; }
        }
        public bool IsComputeAdministrator
        {
            get { return this.UserTableEntity.IsComputeAdministrator; }
            set { this.UserTableEntity.IsComputeAdministrator = value; }
        }
        public bool IsResearcher
        {
            get { return this.UserTableEntity.IsResearcher; }
            set { this.UserTableEntity.IsResearcher = value; }
        }

        public bool HasClaim(Claim claim)
        {
            IEnumerable<Claim> matches = this.Claims.Where(c => c.ClaimType.Equals(claim.ClaimType) && c.Value.Equals(claim.Value)).ToList();

            return matches.FirstOrDefault() != null;
        }
    }
}