// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging;

internal static class DepsFileParser
{
    public static string GetEntryPointAssemblyName(FileInfo depsFile)
    {
        if (depsFile == null)
        {
            throw new ArgumentNullException(nameof(depsFile));
        }

        var content = depsFile.Read();

        var projectName = depsFile.Name.Replace(".deps.json", "");

        var fileContentJson = JObject.Parse(content);

        var runtimeTarget = fileContentJson.SelectToken("$.runtimeTarget.name");

        var target = fileContentJson.SelectToken($"$.targets['{runtimeTarget}']")
            .OfType<JProperty>()
            .Single(t => t.Name.StartsWith($"{projectName}/"));

        var runtimeAssemblyName = target.SelectToken("$..runtime")
            .OfType<JProperty>()
            .Single()
            .Name;

        return runtimeAssemblyName;
    }
}