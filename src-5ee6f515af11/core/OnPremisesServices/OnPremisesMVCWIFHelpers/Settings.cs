//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition.Hosting;
using Microsoft.EMIC.Cloud;

namespace MVCWIFHelpersSettings
{
    public class Settings
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
                            m_CompositionContainer = new CompositionContainer(
                                    new AggregateCatalog(
                                        new AssemblyCatalog(typeof (CompositionIdentifiers).Assembly),
                                        new TypeCatalog(typeof(OnPremisesSettings.OnPremisesSettings))));
                        }
                    }
                }

                return m_CompositionContainer;
            }
        }
    }
}
