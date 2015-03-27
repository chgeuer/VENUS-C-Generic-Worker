//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OGF.BES.Managememt
{
    /// <summary>
    /// WCF created cleint class to interact with OGF BES based Management Service
    /// </summary>
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class BESManagementPortTypeClient : System.ServiceModel.ClientBase<IBESManagementPortType>, IBESManagementPortType
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="BESManagementPortTypeClient"/> class.
        /// </summary>
        public BESManagementPortTypeClient()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BESManagementPortTypeClient"/> class.
        /// </summary>
        /// <param name="endpointConfigurationName">Name of the endpoint configuration.</param>
        public BESManagementPortTypeClient(string endpointConfigurationName) :
            base(endpointConfigurationName)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BESManagementPortTypeClient"/> class.
        /// </summary>
        /// <param name="endpointConfigurationName">Name of the endpoint configuration.</param>
        /// <param name="remoteAddress">The remote address.</param>
        public BESManagementPortTypeClient(string endpointConfigurationName, string remoteAddress) :
            base(endpointConfigurationName, remoteAddress)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BESManagementPortTypeClient"/> class.
        /// </summary>
        /// <param name="endpointConfigurationName">Name of the endpoint configuration.</param>
        /// <param name="remoteAddress">The remote address.</param>
        public BESManagementPortTypeClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) :
            base(endpointConfigurationName, remoteAddress)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BESManagementPortTypeClient"/> class.
        /// </summary>
        /// <param name="binding">The binding.</param>
        /// <param name="remoteAddress">The remote address.</param>
        public BESManagementPortTypeClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) :
            base(binding, remoteAddress)
        {
        }

        /// <summary>
        /// Stops accepting new activities specified by the StopAcceptingNewActivitiesRequest input and returns a StopAcceptingNewActivitiesResponse output
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        StopAcceptingNewActivitiesResponse IBESManagementPortType.StopAcceptingNewActivities(StopAcceptingNewActivitiesRequest request)
        {
            return base.Channel.StopAcceptingNewActivities(request);
        }


        /// <summary>
        /// Stops accepting new activities specified by the StopAcceptingNewActivitiesRequest input and returns a StopAcceptingNewActivitiesResponse output
        /// </summary>
        /// <param name="StopAcceptingNewActivities1">The stop accepting new activities input.</param>
        /// <returns></returns>
        public StopAcceptingNewActivitiesResponseType StopAcceptingNewActivities(StopAcceptingNewActivitiesType StopAcceptingNewActivities1)
        {
            StopAcceptingNewActivitiesRequest inValue = new StopAcceptingNewActivitiesRequest();
            inValue.StopAcceptingNewActivities = StopAcceptingNewActivities1;
            StopAcceptingNewActivitiesResponse retVal = ((IBESManagementPortType)(this)).StopAcceptingNewActivities(inValue);
            return retVal.StopAcceptingNewActivitiesResponse1;
        }

        /// <summary>
        /// Starts accepting new activities specified by the StartAcceptingNewActivitiesRequest input and returns a StartAcceptingNewActivitiesResponse output
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        StartAcceptingNewActivitiesResponse IBESManagementPortType.StartAcceptingNewActivities(StartAcceptingNewActivitiesRequest request)
        {
            return base.Channel.StartAcceptingNewActivities(request);
        }

        /// <summary>
        /// Starts accepting new activities specified by the StartAcceptingNewActivitiesRequest input and returns a StartAcceptingNewActivitiesResponse output
        /// </summary>
        /// <param name="StartAcceptingNewActivities1">The start accepting new activities input.</param>
        /// <returns></returns>
        public StartAcceptingNewActivitiesResponseType StartAcceptingNewActivities(StartAcceptingNewActivitiesType StartAcceptingNewActivities1)
        {
            StartAcceptingNewActivitiesRequest inValue = new StartAcceptingNewActivitiesRequest();
            inValue.StartAcceptingNewActivities = StartAcceptingNewActivities1;
            StartAcceptingNewActivitiesResponse retVal = ((IBESManagementPortType)(this)).StartAcceptingNewActivities(inValue);
            return retVal.StartAcceptingNewActivitiesResponse1;
        }
    }
}
