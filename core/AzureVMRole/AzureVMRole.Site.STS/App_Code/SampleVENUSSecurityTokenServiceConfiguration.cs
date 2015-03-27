//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿
namespace Microsoft.EMIC.Cloud.STS
{
    using System;
    using System.ComponentModel.Composition.Hosting;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.ServiceModel;
    using System.Web;
    using Microsoft.EMIC.Cloud.AzureSettings;
    using Microsoft.EMIC.Cloud.Security;
    using Microsoft.IdentityModel.Claims;
    using Microsoft.IdentityModel.Configuration;
    using Microsoft.IdentityModel.Protocols.WSFederation;
    using Microsoft.IdentityModel.Protocols.WSFederation.Metadata;
    using Microsoft.IdentityModel.Protocols.WSIdentity;
    using Microsoft.IdentityModel.SecurityTokenService;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;
    using Microsoft.EMIC.Cloud.GenericWorker.AzureSecurity;

    public class SampleVENUSSecurityTokenServiceConfiguration : SecurityTokenServiceConfiguration
    {
        internal CompositionContainer CompositionContainer { get; private set; }
        private readonly string UserTableConnectionString;
        private readonly string UserTableTableName;
        private readonly CloudStorageAccount account;

        internal UserTableTableDataContext CreateUserTableContext()
        {
            return new UserTableTableDataContext(account.TableEndpoint.AbsoluteUri, account.Credentials,
                                                 UserTableTableName);
        }

        public SampleVENUSSecurityTokenServiceConfiguration(CompositionContainer container)
            : base()
        {
            if (container == null) throw new ArgumentNullException("container");
            this.CompositionContainer = container;
            this.UserTableConnectionString = this.CompositionContainer.GetExportedValue<string>(CompositionIdentifiers.STSOnAzureConnectionString);
            this.UserTableTableName = this.CompositionContainer.GetExportedValue<string>(CompositionIdentifiers.DevelopmentSecurityTokenServiceUserTableName);
            this.account = CloudStorageAccount.Parse(this.UserTableConnectionString);
            var tc = account.CreateCloudTableClient();
            if (!tc.DoesTableExist(this.UserTableTableName))
            {
                tc.CreateTableIfNotExist(this.UserTableTableName);
            }

            this.SecurityTokenService = typeof(SampleVENUSSecurityTokenService);
            this.SigningCredentials = new X509SigningCredentials(STSCertificate);
        }

        private static volatile SampleVENUSSecurityTokenServiceConfiguration current;
        private static readonly object ThisLock = new object();
        public static SampleVENUSSecurityTokenServiceConfiguration Current
        {
            get
            {
                if (current == null)
                {
                    lock (ThisLock)
                    {
                        if (current == null)
                        {
                            current = new SampleVENUSSecurityTokenServiceConfiguration(
                                new CompositionContainer(
                                    new AggregateCatalog(
                                        new AssemblyCatalog(typeof (CompositionIdentifiers).Assembly),
                                        new AssemblyCatalog(typeof (AzureSettingsProvider).Assembly))));
                        }
                    }
                }
                return current;
            }
        }

        public override SecurityTokenService CreateSecurityTokenService()
        {
            return new SampleVENUSSecurityTokenService(this);
        }

        
        internal X509Certificate2 STSCertificate
        {
            get
            {
                return X509Helper.GetX509Certificate2(StoreLocation.LocalMachine, StoreName.My, STSCertificateThumbprint, X509FindType.FindByThumbprint);
            }
        }

        internal string STSCertificateThumbprint
        {
            get
            {
                return this.CompositionContainer.GetExportedValue<string>(CompositionIdentifiers.STSCertificateThumbprint);
            }
        }

        internal string IssuerAddress
        {
            get
            {
                return this.CompositionContainer.GetExportedValue<string>(CompositionIdentifiers.STSURL);
            }
        }

        private const string AgeClaim = "http://www.government.de/#age";
        public static readonly Dictionary<string, string> CustomClaimDescriptions = new Dictionary<string, string>
                                                                             {
                                                                                 { AgeClaim, "Age"},
                                                                             };


        public void SerializeMetadata(Stream stream)
        {
            var context = HttpContext.Current;

            var entity = new EntityDescriptor(new EntityId(IssuerAddress))
            {
                SigningCredentials = this.SigningCredentials
            };

            var sts = new SecurityTokenServiceDescriptor();
            sts.ClaimTypesOffered.Add(new DisplayClaim(ClaimTypes.Name, "Name", ""));
            sts.ClaimTypesOffered.Add(new DisplayClaim(ClaimTypes.Email, "Email", ""));
            sts.ClaimTypesOffered.Add(new DisplayClaim(ClaimTypes.Role, "Role", ""));
            CustomClaimDescriptions.ToList().ForEach(
                description => sts.ClaimTypesOffered.Add(new DisplayClaim(description.Key, description.Value, "")));

            sts.SecurityTokenServiceEndpoints.Add(new EndpointAddress(IssuerAddress));
            sts.TargetScopes.Add(new EndpointAddress(IssuerAddress));
            sts.ProtocolsSupported.Add(new Uri(WSFederationConstants.Namespace));
            sts.Keys.Add(new KeyDescriptor(this.SigningCredentials.SigningKeyIdentifier)
            {
                Use = KeyType.Signing
            });

            entity.RoleDescriptors.Add(sts);

            new MetadataSerializer().WriteMetadata(stream, entity);
        }
    }
}