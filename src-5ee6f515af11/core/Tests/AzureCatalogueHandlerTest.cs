//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.ComponentModel.Composition.Hosting;
using Microsoft.EMIC.Cloud.Demo;
using Microsoft.EMIC.Cloud.DataManagement;
using Microsoft.EMIC.Cloud.Storage.Azure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class AzureCatalogueHandlerTest
    {
        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        /// <summary>
        ///A test for Exists
        ///</summary>
        [TestMethod()]
        public void ExistsTest()
        {
            string logicalName = "Test";
            bool actual;


            CompositionContainer c = new CompositionContainer(new DirectoryCatalog("."));
            var argRepo = c.GetExportedValue<ArgumentRepository>();

            AzureCatalogueHandler azureHandler = new AzureCatalogueHandler(CloudSettings.UserDataStoreConnectionString, argRepo);
            //I could also put "manually" a new logicalName in the table            
            azureHandler.Add(logicalName, new AzureArgumentSingleReference
            {
                Name = "TestName",
                DataAddress = "someUri",
                ConnectionString = CloudSettings.UserDataStoreConnectionString,
                IsDownload = DataSetType.Input
            });
            actual = azureHandler.Exists(logicalName);
            Assert.AreEqual(true, actual);

            //azureHandler.Remove(logicalName); //An unhandled exception of type 'System.StackOverflowException' occurred in Microsoft.EMIC.Cloud.Storage.Azure.dll
            logicalName = "this logical name does not exist";
            actual = azureHandler.Exists(logicalName);
            Assert.AreEqual(false, actual);
        }
    }
}
