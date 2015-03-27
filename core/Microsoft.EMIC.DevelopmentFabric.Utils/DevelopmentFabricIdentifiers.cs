//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace Microsoft.EMIC.DevelopmentFabric.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.WindowsAzure.ServiceRuntime;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    internal class DevelopmentFabricIdentifiers
    {
        internal DevelopmentFabricIdentifiers(string value)
        {
            if (!MicrosoftCorpnetAuthenticationFixer.InDevFabric)
                throw new NotSupportedException("Must be running in development fabric");

            var match = Regex.Match(value, pattern);
            DeploymentId = int.Parse(match.Groups["deploymentid"].Value);
            Name = match.Groups["name"].Value;
            InstanceId = int.Parse(match.Groups["instanceid"].Value);
        }
        private const string pattern = @"^deployment\((?<deploymentid>\d+)\)\.(?<name>.+)\.(?<instanceid>\d+)(_Web)?$";
        public static bool IsDID(string value)
        {
            return Regex.IsMatch(value, pattern);
        }

        public int DeploymentId { get; set; }
        public string Name { get; set; }
        public int InstanceId { get; set; }

        public override string ToString()
        {
            return string.Format("deployment({0}).{1}.{2}_Web", DeploymentId, Name, InstanceId);
        }
        private bool BelongsToSameDeployment(string otherDeploymentId)
        {
            return BelongsToSameDeployment(new DevelopmentFabricIdentifiers(otherDeploymentId));
        }
        private bool BelongsToSameDeployment(DevelopmentFabricIdentifiers other)
        {
            return this.DeploymentId == other.DeploymentId && this.Name == other.Name;
        }
        public static DevelopmentFabricIdentifiers Current
        {
            get { return new DevelopmentFabricIdentifiers(RoleEnvironment.CurrentRoleInstance.Id + "_Web"); }
        }
        public static List<DevelopmentFabricIdentifiers> AzureRoles
        {
            get { return RoleEnvironment.CurrentRoleInstance.Role.Instances.Select(ri => ri.Id).Select(name => new DevelopmentFabricIdentifiers(name)).ToList(); }
        }
        public static int NumberOfRoles
        {
            get { return DevelopmentFabricIdentifiers.AzureRoles.Where(DevelopmentFabricIdentifiers.Current.BelongsToSameDeployment).Count(); }
        }
        public static List<string> PeerRoles
        {
            get
            {
                // Microsoft.WindowsAzure.ServiceRuntime.CurrentRoleInstanceImp // Microsoft.WindowsAzure.ServiceRuntime.ExternalRoleInstanceImpl

                return RoleEnvironment.CurrentRoleInstance.Role.Instances.Where(x => x.GetType().Name.Equals("ExternalRoleInstanceImpl")).Select(ri => ri.Id).ToList();
            }
        }
    }
}
