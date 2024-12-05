// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.App.Connection;

internal static class PackageAcquisition
{
    internal static string InferCompatiblePackageVersion()
    {
        var informationalVersion = typeof(Formatter)
                                   .Assembly
                                   .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                   .InformationalVersion;

        if (informationalVersion.Contains("+"))
        {
            informationalVersion = informationalVersion.Split("+")[0];
        }

        var splitInformationalVersion = informationalVersion.Split('.');

        var version = splitInformationalVersion.Length is 4
                          ? string.Join(".", splitInformationalVersion[0..4])
                          : informationalVersion;
        return version;
    }
}