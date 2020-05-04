// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Security;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.Win32;

namespace Microsoft.DotNet.Interactive.Telemetry
{
    internal class DockerContainerDetectorForTelemetry : IDockerContainerDetector
    {
        public IsDockerContainerResult IsDockerContainer()
        {
            switch (RuntimeEnvironment.OperatingSystemPlatform)
            {
                case Platform.Windows:
                    try
                    {
                        using (RegistryKey subkey
                            = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control"))
                        {
                            return subkey?.GetValue("ContainerType") != null
                                ? IsDockerContainerResult.True
                                : IsDockerContainerResult.False;
                        }
                    }
                    catch (SecurityException)
                    {
                        return IsDockerContainerResult.Unknown;
                    }
                case Platform.Linux:
                    return ReadProcToDetectDockerInLinux()
                        ? IsDockerContainerResult.True
                        : IsDockerContainerResult.False;
                case Platform.Unknown:
                    return IsDockerContainerResult.Unknown;
                case Platform.Darwin:
                default:
                    return IsDockerContainerResult.False;
            }
        }

        private static bool ReadProcToDetectDockerInLinux()
        {
            return Telemetry.IsRunningInDockerContainer || File.ReadAllText("/proc/1/cgroup").Contains("/docker/");
        }
    }
}
