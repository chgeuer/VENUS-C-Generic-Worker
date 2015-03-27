//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.ServiceModel.Activation;
using System.ServiceModel;

namespace Microsoft.EMIC.Cloud.GenericWorker
{
    /// <summary>
    /// This class provides the implementation of the scaling service
    /// </summary>
    [Export(typeof(ScalingServiceImpl))]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceBehavior(AddressFilterMode = AddressFilterMode.Any)]
    public class ScalingServiceImpl : IScalingService
    {
        /// <summary>
        /// Gets or sets the available scaling plugins.
        /// </summary>
        /// <value>
        /// The scaling plugins.
        /// </value>
        [ImportMany]
        public IEnumerable<IScalingPlugin> ScalingPlugins { get; set; }

        /// <summary>
        /// Lists the deployment sizes for the different cloud providers.
        /// </summary>
        /// <returns>A collection of DeploymentSize instances describing the available instances for the different cloud providers.</returns>
        public List<DeploymentSize> ListDeployments()
        {
            var deps = new List<DeploymentSize>();
            foreach (var plugin in ScalingPlugins)
            {
                try
                {
                    var provider = plugin.CloudProviderID;
                    var instances = plugin.InstanceCount;
                    deps.Add(new DeploymentSize() { CloudProviderID = provider, InstanceCount = instances });
                }
                catch (Exception exc)
                {
                    throw new FaultException<OGF.BES.Faults.Fault>
                      (new OGF.BES.Faults.Fault()
                      {
                      }, new FaultReason(ExceptionMessages.RuntimeScalingPlugin + exc.ToString()));
                }
            }
            return deps;
        }

        /// <summary>
        /// Updates the given deployment.
        /// </summary>
        /// <param name="newDeploymentSize">New size of the deployment.</param>
        public void UpdateDeployment(DeploymentSize newDeploymentSize)
        {
            if (newDeploymentSize == null)
            {
                throw new FaultException<OGF.BES.Faults.InvalidRequestMessageFaultType>
                       (new OGF.BES.Faults.InvalidRequestMessageFaultType()
                       {

                       }, new FaultReason(ExceptionMessages.DeploymentSizeNull));
            }
            if (newDeploymentSize.InstanceCount <= 0)
            {
                throw new FaultException<OGF.BES.Faults.InvalidRequestMessageFaultType>
                       (new OGF.BES.Faults.InvalidRequestMessageFaultType()
                       {

                       }, new FaultReason(string.Format(ExceptionMessages.InstanceCount, newDeploymentSize.InstanceCount)));
            }
            var plugin = ScalingPlugins.Where(p => p.CloudProviderID == newDeploymentSize.CloudProviderID).FirstOrDefault();
            if (plugin == null)
            {
                throw new FaultException<OGF.BES.Faults.UnsupportedFeatureFaultType>
                       (new OGF.BES.Faults.UnsupportedFeatureFaultType()
                       {

                       }, new FaultReason(string.Format(ExceptionMessages.NoScalingPlugin, newDeploymentSize.CloudProviderID)));
            }
            try 
            { 
                plugin.InstanceCount = newDeploymentSize.InstanceCount;
            }
            catch (Exception exc)
            {
                throw new FaultException<OGF.BES.Faults.Fault>
                      (new OGF.BES.Faults.Fault()
                      {
                      }, new FaultReason(ExceptionMessages.RuntimeScalingPlugin + exc.ToString()));

            }
        }
    }
}
