// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Security;
using Microsoft.Win32;

namespace Microsoft.DotNet.Interactive.Telemetry
{
    internal class DockerContainerDetectorForTelemetry : IDockerContainerDetector
    {
        public IsDockerContainerResult IsDockerContainer()
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    using (RegistryKey subkey
                        = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control"))
                    {
                        return subkey?.GetValue("ContainerType") is not null
                            ? IsDockerContainerResult.True
                            : IsDockerContainerResult.False;
                    }
                }
                catch (SecurityException)
                {
                    return IsDockerContainerResult.Unknown;
                }
            }

            if (OperatingSystem.IsLinux())
            {
                return ReadProcToDetectDockerInLinux()
                    ? IsDockerContainerResult.True
                    : IsDockerContainerResult.False;
            }

            if (OperatingSystem.IsMacOS())
            {
                return IsDockerContainerResult.False;
            }

            return IsDockerContainerResult.Unknown;
        }

        private static bool ReadProcToDetectDockerInLinux()
        {
            return Telemetry.IsRunningInDockerContainer || File.ReadAllText("/proc/1/cgroup").Contains("/docker/");
        }
    }
}
