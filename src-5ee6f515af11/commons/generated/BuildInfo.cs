//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Permissions;

[assembly: AssemblyVersion(
    Microsoft.EMIC.AutomatedBuild.BuildInfo.ProductVersionStringMajor + "." + 
    Microsoft.EMIC.AutomatedBuild.BuildInfo.ProductVersionStringMinor + "." + 
    Microsoft.EMIC.AutomatedBuild.BuildInfo.AssemblyBuildNumberString + "." + 
    "0")]
[assembly: AssemblyFileVersion(
    Microsoft.EMIC.AutomatedBuild.BuildInfo.ProductVersionStringMajor + "." + 
    Microsoft.EMIC.AutomatedBuild.BuildInfo.ProductVersionStringMinor + "." +
    Microsoft.EMIC.AutomatedBuild.BuildInfo.AssemblyBuildNumberString + "." + 
    "0")]
[assembly: AssemblyConfiguration(Microsoft.EMIC.AutomatedBuild.BuildInfo.AssemblyConfiguration)]
[assembly: AssemblyCompany(Microsoft.EMIC.AutomatedBuild.BuildInfo.AssemblyCompany)]
//[assembly: AssemblyCopyright(Microsoft.EMIC.AutomatedBuild.BuildInfo.AssemblyCopyright)]
//[assembly: AssemblyTrademark(Microsoft.EMIC.AutomatedBuild.BuildInfo.AssemblyTrademark)]

namespace Microsoft.EMIC.AutomatedBuild 
{
    internal static class BuildInfo 
    {
        public const string AssemblyCompany = "Advanced Technology Labs Europe";
        public const string AssemblyTrademark = "Advanced Technology Labs Europe";

        public const string AssemblyProductName = "Microsoft contribution to VENUS-C - Generic Worker (Changeset 5ee6f515af11, Revision 1588)";

		public const int VersionControlSystem_HgRevision_Number = 1588;
		public const string VersionControlSystem_HgRevision_String = "1588";
		public const string VersionControlSystem_HgChangeset_String = "5ee6f515af11";
		public const string BuildSystem_BuildUser_String = @"EUROPE\a-meick";

        public const string AssemblyConfiguration = "*** Local build - do not distribute! ***";
        public const string AssemblyCopyright = "Copyright © Advanced Technology Labs Europe 2013";

		public const int ProductVersionNumberMajor = 2;
		public const string ProductVersionStringMajor = "2";
		public const int ProductVersionNumberMinor = 1;
		public const string ProductVersionStringMinor = "1";
		public const int AssemblyBuildNumber = 0;
    public const string AssemblyBuildNumberString = "0";

        public static string Version 
        {
            get
            {
                return "Version " + Microsoft.EMIC.AutomatedBuild.BuildInfo.ProductVersionStringMajor + "." +
                       Microsoft.EMIC.AutomatedBuild.BuildInfo.ProductVersionStringMinor + "." +
                       Microsoft.EMIC.AutomatedBuild.BuildInfo.AssemblyBuildNumberString + "." +
                       "0" + " Hg " + VersionControlSystem_HgChangeset_String;
            }
        }
    }
}
