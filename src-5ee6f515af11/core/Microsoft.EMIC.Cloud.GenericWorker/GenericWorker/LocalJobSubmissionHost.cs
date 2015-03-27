//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Microsoft.EMIC.Cloud.GenericWorker
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition.Hosting;
    using System.ServiceModel;
    using System.ServiceModel.Description;
    using Microsoft.EMIC.Cloud.Utilities;
    using OGF.BES;
    using System.Diagnostics;
    
    /// <summary>
    /// Local job sumission host class
    /// </summary>
    public class LocalJobSubmissionHost
    {
        private readonly ServiceHost _serviceHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalJobSubmissionHost"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="localJobSummissionEndPoint">LocalSubmissionEndPoint's Name</param>
        public LocalJobSubmissionHost(CompositionContainer container, string localJobSummissionEndPoint)
        {
            //var localJobSummissionEndPoint = container.GetExportedValue<string>(CompositionIdentifiers.LocalJobSummissionEndPoint);
            var uri = new Uri(localJobSummissionEndPoint);

            var serviceBinding = new WS2007HttpBinding(SecurityMode.None, reliableSessionEnabled: false);

            _serviceHost = new ServiceHost(typeof(LocalJobSubmissionService), uri);
            _serviceHost.AddServiceEndpoint(typeof(BESFactoryPortType), serviceBinding, "");
            _serviceHost.Description.Behaviors.Add(
               new MyServiceBehavior<LocalJobSubmissionService>(container));

            #region ServiceMetadataBehavior

            var smb = _serviceHost.Description.Behaviors.Find<ServiceMetadataBehavior>();
            if (smb == null)
            {
                smb = new ServiceMetadataBehavior();
                _serviceHost.Description.Behaviors.Add(smb);
            }
            smb.HttpGetEnabled = true;
            _serviceHost.AddServiceEndpoint(typeof(IMetadataExchange), MetadataExchangeBindings.CreateMexHttpBinding(), "mex");

            #endregion

            #region ServiceDebugBehavior

            var sdb = _serviceHost.Description.Behaviors.Find<ServiceDebugBehavior>();
            if (sdb == null)
            {
                sdb = new ServiceDebugBehavior();
                _serviceHost.Description.Behaviors.Add(sdb);
            }
            sdb.IncludeExceptionDetailInFaults = true;

            #endregion

            #region UseRequestHeadersForMetadataAddressBehavior

            var urhfmab = new UseRequestHeadersForMetadataAddressBehavior();
            urhfmab.DefaultPortsByScheme.Add(new System.Collections.Generic.KeyValuePair<string, int>("http", 80));
            _serviceHost.Description.Behaviors.Add(urhfmab);

            #endregion
            
            Debug.Assert(container.GetExportedValue<LocalJobSubmissionService>() != null);
        }

        /// <summary>
        /// Opens this instance.
        /// </summary>
        public void Open()
        {
            _serviceHost.Open();
        }
        /// <summary>
        /// Closes this instance.
        /// </summary>
        public void Close()
        {
            _serviceHost.Close();
        }
    }
}