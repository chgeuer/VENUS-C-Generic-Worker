//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.EMIC.Cloud.GenericWorker.AzureSecurity;
using Microsoft.IdentityModel.Claims;

namespace STSInitializer
{
    class STSInitalizerProgram
    {

        static void Main(string[] args)
        {
            var isInitialized = Properties.Settings.Default.IsInitialized;
            if (!isInitialized)
            {
                SaveSettings();
            }

            while (ShowSettings())
            {
            }

            var connectionstring = Properties.Settings.Default.STSOnAzureConnectionString;
            var account = CloudStorageAccount.Parse(connectionstring);
            var tableClient = account.CreateCloudTableClient();
            CloudTableClient.CreateTablesFromModel(typeof(UserTableEntity), account.TableEndpoint.AbsoluteUri, account.Credentials);

            var tablename = Properties.Settings.Default.DevelopmentSecurityTokenServiceUserTableName; 
            if(tableClient.DoesTableExist(tablename))
            {
                Console.WriteLine(String.Format("There exists already a table named {0} !" , tablename));
                Console.WriteLine(String.Format("All the data in the table {0} will be deleted!", tablename));
                Console.WriteLine(" Do you want to continue?<y/n>");
                while (true)
                {
                    string result = Console.ReadLine().ToLower();
                    if (result == "n" || result == "no")
                    {
                        return;
                    }
                    else if (result == "y" || result == "yes")
                    {
                        break;
                    }
                    else
                    {
                        Console.WriteLine(String.Format("Unspecified command. Do you want to continue?<y/n>", tablename));
                    }
                }

            }
            
            tableClient.CreateTableIfNotExist(tablename);

            var ctx = new UserTableTableDataContext(account.TableEndpoint.AbsoluteUri, account.Credentials, tablename);

            ctx.Users.ToList().ForEach(user => { ctx.DeleteObject(user); ctx.SaveChangesWithRetries(); });

            Console.WriteLine("Which security scenario do you want to setup?");
            Console.WriteLine("A - Army of One");
            Console.WriteLine("B - Research Institute");
            Console.WriteLine("C - Portal (hidden identity)");
            Console.WriteLine("D - Portal (exposed identity)");
            Console.WriteLine("---------------------------------");
            Console.WriteLine("Any other key - Add just some sample users");
            var selection = Console.ReadKey().KeyChar;

            switch (selection)
            {
                case 'a':
                case 'A':
                case 'b':
                case 'B':
                case 'c':
                case 'C':
                    CreateSingleUser(ctx);
                    break;
                case 'd':
                case 'D':
                    Console.WriteLine("In this case the STS user database needs to have an entry for each user.");
                    Console.WriteLine();
                    CreateSampleUsers(ctx);
                    break;
                default:
                    CreateSampleUsers(ctx);
                    break;
            }


            Console.WriteLine(tablename + " is initialized with default values to be used by STS");
            Console.WriteLine("Press <Return> to close ");
            Console.ReadLine();
        }

        private static string AskUser(string property, string defaultValue)
        {
            Console.Write(string.Format("{0} [default: {1}] : ", property, defaultValue));
            var entry = Console.ReadLine();
            return (string.IsNullOrWhiteSpace(entry)) ? defaultValue : entry;
        }

        private static void CreateSingleUser(UserTableTableDataContext ctx)
        {
            Console.WriteLine("\nIn this scenario there is only one user. this user has all administrative privileges.");
            var username = AskUser("Username", "Administrator");
            var passwd = AskUser("Password", "secret");

            ctx.AddUser(new User(username, passwd, new List<Claim>
                                                                {
                                                                    new Claim(ClaimTypes.GivenName, "Jim"),
                                                                    new Claim(ClaimTypes.Surname, "ITGuy"),
                                                                    new Claim(ClaimTypes.Email, "root@university.edu")
                                                                })
            {
                IsResearcher = true,
                IsApplicationRepositoryAdministrator = true,
                IsComputeAdministrator = true
            });
        }

        private static void CreateSampleUsers(UserTableTableDataContext ctx)
        {
            Console.WriteLine("\nSome sample users are added to the STS user database. Feel free to add to or modify them.");
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
        }


        private static bool ShowSettings()
        {
            Console.WriteLine("The application settings saved are:");
            Console.WriteLine(String.Format("Azure stroage Connection String : {0}", Properties.Settings.Default.STSOnAzureConnectionString));
            Console.WriteLine(String.Format("STS Table Name: {0}", Properties.Settings.Default.DevelopmentSecurityTokenServiceUserTableName));

            Console.WriteLine("Press 'y' in order to continue with the shown settings, press 'e' in order to change the application settings<y/e>");

            while (true)
            {
                string result = Console.ReadLine().ToLower();
                if (result == "e")
                {
                    SaveSettings();
                    return true;
                }
                else if (result == "y" || result == "yes")
                {
                    return false;
                }
                else
                {
                    Console.WriteLine("Unspecified command. Press 'y' in order to continue with the shown settings, press 'e' in order to change the application settings<y/e>");
                }
            }
        }

        private static void SaveSettings()
        {
            Console.WriteLine("Application settings are not initialize, please enter the required information");
            Console.WriteLine("Azure Storage Connection String");
            Properties.Settings.Default.STSOnAzureConnectionString = Console.ReadLine();
            Console.WriteLine(String.Format("Sts Table (Default value is {0})", Properties.Settings.Default.DevelopmentSecurityTokenServiceUserTableName));
            Properties.Settings.Default.DevelopmentSecurityTokenServiceUserTableName = Console.ReadLine();
            Console.WriteLine();
            Properties.Settings.Default.IsInitialized = true;
            Properties.Settings.Default.Save();
        }
    }
}
