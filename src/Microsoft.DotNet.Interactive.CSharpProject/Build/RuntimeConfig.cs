// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.CSharpProject.Build;

internal static class RuntimeConfig
{
    public static string GetTargetFramework(FileInfo runtimeConfigFile)
    {
        if (runtimeConfigFile == null)
        {
            throw new ArgumentNullException(nameof(runtimeConfigFile));
        }

        var content = File.ReadAllText(runtimeConfigFile.FullName);

        var fileContentJson = JObject.Parse(content);

        return fileContentJson["runtimeOptions"]["tfm"].Value<string>();
    }
}