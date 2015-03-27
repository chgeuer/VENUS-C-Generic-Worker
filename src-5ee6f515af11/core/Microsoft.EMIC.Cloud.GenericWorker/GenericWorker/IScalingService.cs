//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;
using OGF.BES.Faults;

namespace Microsoft.EMIC.Cloud.GenericWorker
{
    /// <summary>
    /// Interface that is implemented by the scaling service
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName = "IScalingService")]
    public interface IScalingService
    {
        /// <summary>
        /// Lists the deployment sizes for the different cloud providers.
        /// </summary>
        /// <returns>A collection of DeploymentSize instances describing the available instances for the different cloud providers.</returns>
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(Fault))]
        [System.ServiceModel.FaultContractAttribute(typeof(Fault), Action = "http://tempuri.org/IScalingService/ListDeployments/" +
            "Fault/Fault", Name = "Fault")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        [System.ServiceModel.OperationContractAttribute(Action = "http://tempuri.org/IScalingService/ListDeployments", ReplyAction = "http://tempuri.org/IScalingService/ListDeploymentsResponse")]
        List<Microsoft.EMIC.Cloud.GenericWorker.DeploymentSize> ListDeployments();

        /// <summary>
        /// Updates the given deployment.
        /// </summary>
        /// <param name="newDeploymentSize">New size of the deployment.</param>
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(InvalidRequestMessageFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(UnsupportedFeatureFaultType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(Fault))]
        [System.ServiceModel.FaultContractAttribute(typeof(InvalidRequestMessageFaultType), Action = "http://tempuri.org/IScalingService/UpdateDeployment/" +
            "Fault/InvalidRequestMessageFaultType", Name = "InvalidRequestMessageFaultType")]
        [System.ServiceModel.FaultContractAttribute(typeof(UnsupportedFeatureFaultType), Action = "http://tempuri.org/IScalingService/UpdateDeployment/" +
            "Fault/UnsupportedFeatureFaultType", Name = "UnsupportedFeatureFaultType")]
        [System.ServiceModel.FaultContractAttribute(typeof(Fault), Action = "http://tempuri.org/IScalingService/UpdateDeployment/" +
            "Fault/Fault", Name = "Fault")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults = true)]
        [System.ServiceModel.OperationContractAttribute(Action = "http://tempuri.org/IScalingService/UpdateDeployment", ReplyAction = "http://tempuri.org/IScalingService/UpdateDeploymentResponse")]
        void UpdateDeployment(Microsoft.EMIC.Cloud.GenericWorker.DeploymentSize newDeploymentSize);
    }
}