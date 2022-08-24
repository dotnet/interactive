// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Reflection;

namespace Microsoft.DotNet.Interactive.Telemetry;

public class BuildInfo
{
    public string AssemblyVersion { get; set; }
    public string BuildDate { get; set; }
    public string AssemblyInformationalVersion { get; set; }
    public string AssemblyName { get; set; }

    public static BuildInfo GetBuildInfo(Assembly assembly)
    {
        var info = new BuildInfo
        {
            AssemblyName = assembly.GetName().Name,
            AssemblyInformationalVersion = assembly
                                           .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                           .InformationalVersion,
            AssemblyVersion = assembly.GetName().Version.ToString(),
            BuildDate = new FileInfo(assembly.Location).CreationTimeUtc.ToString("o")
        };

        return info;
    }
}