//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿
using Microsoft.IdentityModel.Claims;

namespace Microsoft.EMIC.Cloud.Tools.UserManagement
{
    using System.Collections.Generic;
    using System.ComponentModel.Composition.Hosting;
    using Microsoft.EMIC.Cloud;
    using Microsoft.EMIC.Cloud.GenericWorker.AzureSecurity;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;

    class UserManagementProgram
    {
        static void Main(string[] args)
        {
            var container = new CompositionContainer(new AggregateCatalog(
                new AssemblyCatalog(typeof(Microsoft.EMIC.Cloud.Demo.CloudSettings).Assembly)
                ));

            var connectionstring = container.GetExportedValue<string>(CompositionIdentifiers.STSOnAzureConnectionString);
            var account = CloudStorageAccount.Parse(connectionstring);
            var tableClient = account.CreateCloudTableClient();
            CloudTableClient.CreateTablesFromModel(typeof(UserTableEntity), account.TableEndpoint.AbsoluteUri, account.Credentials);

            var tablename = container.GetExportedValue<string>(CompositionIdentifiers.DevelopmentSecurityTokenServiceUserTableName);
            tableClient.CreateTableIfNotExist(tablename);
            var ctx = new UserTableTableDataContext(account.TableEndpoint.AbsoluteUri, account.Credentials, tablename);

            ctx.AddUser(new User("Researcher", "secret", new List<Claim>
                                                             {
                                                                 new Claim(ClaimTypes.GivenName, "Joe"),
                                                                 new Claim(ClaimTypes.Surname, "Bloggs"),
                                                                 new Claim(ClaimTypes.Email, "joe@university.edu")
                                                             })
                            {
                                IsResearcher = true
                            });
            ctx.AddUser(new User("Alice", "secret", new List<Claim>
                                                             {
                                                                 new Claim(ClaimTypes.GivenName, "Alice"),
                                                                 new Claim(ClaimTypes.Surname, "Bloggs"),
                                                                 new Claim(ClaimTypes.Email, "alice@university.edu")
                                                             })
                            {
                                IsResearcher = true
                            });
            ctx.AddUser(new User("Bob", "secret", new List<Claim>
                                                             {
                                                                 new Claim(ClaimTypes.GivenName, "Bob"),
                                                                 new Claim(ClaimTypes.Surname, "Bloggs"),
                                                                 new Claim(ClaimTypes.Email, "bob@university.edu")
                                                             })
                            {
                                IsResearcher = true
                            });
            ctx.AddUser(new User("Administrator", "secret", new List<Claim>
                                                             {
                                                                 new Claim(ClaimTypes.GivenName, "Jim"),
                                                                 new Claim(ClaimTypes.Surname, "ITGuy"),
                                                                 new Claim(ClaimTypes.Email, "root@university.edu")
                                                             })
                            {
                                IsResearcher = false, 
                                IsApplicationRepositoryAdministrator = true, 
                                IsComputeAdministrator = true
                            });
            ctx.AddUser(new User("anonymous", "", new List<Claim>
                                                             {
                                                                 new Claim(ClaimTypes.GivenName, "anonymous"),
                                                                 new Claim(ClaimTypes.Surname, "anonymous"),
                                                                 new Claim(ClaimTypes.Email, "anonymous@university.edu")
                                                             })
                            {
                                IsResearcher = true,
                                IsApplicationRepositoryAdministrator = true,
                                IsComputeAdministrator = true
                            });
        }
    }
}
