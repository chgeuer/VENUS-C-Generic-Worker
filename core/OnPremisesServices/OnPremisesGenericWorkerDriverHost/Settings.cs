//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition.Hosting;
using OnPremisesGenericWorkerDriverHost;
using Microsoft.EMIC.Cloud.GenericWorker;
using Microsoft.EMIC.Cloud.GenericWorker.AzureProvider;
using Microsoft.EMIC.Cloud.Storage.Azure;
using Microsoft.EMIC.Cloud.GenericWorker.SampleNotifications;

namespace GenericWorkerDriverHostContainerSettings
{
    public static class Settings
    {
        private static CompositionContainer m_CompositionContainer;
        private static object m_lock = new object();
        public static CompositionContainer Container
        {
            get
            {
                if (m_CompositionContainer == null)
                {
                    lock (m_lock)
                    {
                        if (m_CompositionContainer == null)
                        {
                            m_CompositionContainer = new CompositionContainer(new AggregateCatalog(
                                                           new TypeCatalog(
                                                               typeof(OnPremisesSettings.OnPremisesSettings),
                                                               typeof(GenericWorkerDriverHostSettings)
                                                               ),
                                                           new AssemblyCatalog(typeof(FilesystemMapper).Assembly),
                                                           new AssemblyCatalog(typeof(AzureGWRuntimeEnvironment).Assembly),
                                                           new AssemblyCatalog(typeof(AzureArgumentSingleReference).Assembly),
                                                           new AssemblyCatalog(typeof(KTH.GenericWorker.CDMI.CDMIBlobReference).Assembly),
                                                           new AssemblyCatalog(typeof(AzureQueueJobStatusNotificationPlugin).Assembly)
                                                           ));
                        }
                    }
                }

                return m_CompositionContainer;
            }
        }
    }
}
