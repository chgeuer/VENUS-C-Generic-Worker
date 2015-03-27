//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿
namespace Microsoft.EMIC.Cloud.STS
{
    using System;
    using System.ComponentModel.Composition.Hosting;
    using System.IdentityModel.Tokens;
    using Microsoft.IdentityModel.Claims;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;
    using Microsoft.EMIC.Cloud.GenericWorker.AzureSecurity;

    public class SampleCustomUserNameSecurityTokenHandler : UserNameSecurityTokenHandler
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

        private SampleCustomUserNameSecurityTokenHandler() { }

        public SampleCustomUserNameSecurityTokenHandler(CompositionContainer container)
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
        }

        public override Type TokenType { get { return typeof(UserNameSecurityToken); } }

        public override bool CanValidateToken { get { return true; } }

        public override ClaimsIdentityCollection ValidateToken(SecurityToken token)
        {
            System.Diagnostics.Trace.TraceInformation("SampleCustomUserNameSecurityTokenHandler.ValidateToken()");

            var up = token as UserNameSecurityToken;
            if (up == null)
            {
                throw new ArgumentException("token", string.Format(ExceptionMessages.MustBe, typeof(UserNameSecurityToken).Name));
            }

            var ctx = CreateUserTableContext();
            var user = ctx.GetUser(up.UserName);
            if (user == null || user.Password != up.Password)
            {
                throw new SecurityTokenValidationException(ExceptionMessages.InvalidUsernamePassword);
            }

            var claimsId = new ClaimsIdentity(user.Claims);

            claimsId.Claims.Add(new Claim(ClaimTypes.Name, up.UserName));
            claimsId.Claims.Add(new Claim(ClaimTypes.AuthenticationMethod, AuthenticationMethods.Password));

            return new ClaimsIdentityCollection(new IClaimsIdentity[] { claimsId });
        }
    }
}