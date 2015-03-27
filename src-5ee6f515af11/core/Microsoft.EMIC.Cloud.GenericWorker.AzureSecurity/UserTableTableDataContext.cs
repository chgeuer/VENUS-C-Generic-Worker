//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.Collections.Generic;

namespace Microsoft.EMIC.Cloud.GenericWorker.AzureSecurity
{
    using System.Linq;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;

    /// <summary>
    /// The <see cref="TableServiceContext"/> to interact with the index table for submitted or running jobs. 
    /// </summary>
    public class UserTableTableDataContext : TableServiceContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserTableTableDataContext"/> class.
        /// </summary>
        /// <param name="baseAddress">The base address.</param>
        /// <param name="credentials">The credentials.</param>
        /// <param name="tableName">Name of the table.</param>
        public UserTableTableDataContext(string baseAddress, StorageCredentials credentials, string tableName)
            : base(baseAddress, credentials)
        {
            this.TableName = tableName;
            this.IgnoreResourceNotFoundException = true;
        }

        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        /// <value>
        /// The name of the table.
        /// </value>
        public string TableName { get; private set; }

        /// <summary>
        /// Gets the current users.
        /// </summary>
        public IQueryable<UserTableEntity> Users
        {
            get { return this.CreateQuery<UserTableEntity>(this.TableName); }
        }

        public bool AreCredentialsValid(string username, string password)
        {
            var user = Users.Where(u => (u.RowKey == username && u.Password == password)).FirstOrDefault();
            return (user != null);
        }

        public User GetUser(string username)
        {
            var ute = Users.Where(x => x.RowKey == username).FirstOrDefault();
            return ute == null ? null : new User(ute);
        }

        public void AddUser(User user)
        {
            this.AddObject(this.TableName, user.UserTableEntity);
            this.SaveChangesWithRetries();
        }
    }
}