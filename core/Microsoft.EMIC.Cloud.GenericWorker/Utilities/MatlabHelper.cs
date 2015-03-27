//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace Microsoft.EMIC.Cloud.Utilities
{
    /// <summary>
    /// This class provides methods to help using the GenericWorker SDK inside Matlab.
    /// Matlab does not support all .NET functionalities and the provided methods can be used as a workaround for the normal .NET methods and constructors that cannot be used inside Matlab.
    /// </summary>
    public class MatlabHelper
    {
        private MatlabHelper()
        {
            // no need to create an instance of this class
        }

        /// <summary>
        /// This method can be used to create an EndpointAddress object.
        /// There is a constructor taking three arguments and the third argument is a params collection of type AdressHeader.
        /// In the .NET world this third argument does not need to be provided so only the first two arguments are passed.
        /// This is not possible in Matlab and an excpetion "No constructor taking two arguments.." or similiar is thrown.
        /// In addition it is not possible to create the required collection of Addressheader. By providing this method an
        /// object of EndpointAdress with only the first two constructor arguments can be created in Matlab.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="identity"></param>
        /// <returns></returns>
        public static EndpointAddress CreateEndpointAdress(Uri uri, EndpointIdentity identity)
        {
            return new EndpointAddress(uri, identity);
        }
    }
}
