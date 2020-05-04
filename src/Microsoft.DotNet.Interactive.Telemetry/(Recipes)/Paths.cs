// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.Interactive.Telemetry
{
    internal static class Paths
    {
        private const string DotnetHomeVariableName = "DOTNET_CLI_HOME";
        private const string DotnetProfileDirectoryName = ".dotnet";
        private const string ToolsShimFolderName = "tools";

        static Paths()
        {
            UserProfile = Environment.GetEnvironmentVariable(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? "USERPROFILE"
                    : "HOME");

            DotnetToolsPath = Path.Combine(UserProfile, DotnetProfileDirectoryName, ToolsShimFolderName);

            var nugetPackagesEnvironmentVariable = Environment.GetEnvironmentVariable("NUGET_PACKAGES");

            NugetCache = String.IsNullOrWhiteSpace(nugetPackagesEnvironmentVariable)
                             ? Path.Combine(UserProfile, ".nuget", "packages")
                             : nugetPackagesEnvironmentVariable;
        }

        public static string DotnetUserProfileFolderPath =>
            Path.Combine(DotnetHomePath, DotnetProfileDirectoryName);

        public static string DotnetHomePath
        {
            get
            {
                var home = Environment.GetEnvironmentVariable(DotnetHomeVariableName);
                if (string.IsNullOrEmpty(home))
                {
                    home = UserProfile;
                    if (string.IsNullOrEmpty(home))
                    {
                        throw new DirectoryNotFoundException();
                    }
                }

                return home;
            }
        }

        public static string DotnetToolsPath { get; }

        public static string UserProfile { get; }

        public static string NugetCache { get; }

        public static readonly string InstallDirectory = Path.GetDirectoryName(typeof(Paths).Assembly.Location);
    }
}
