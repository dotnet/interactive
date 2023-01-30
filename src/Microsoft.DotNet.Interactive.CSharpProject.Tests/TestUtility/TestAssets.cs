// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public static class TestAssets
{
    public static DirectoryInfo SampleConsole => 
        new DirectoryInfo(Path.Combine(GetTestProjectsFolder(), "SampleConsole"));

    public static DirectoryInfo KernelExtension => 
        new DirectoryInfo(Path.Combine(GetTestProjectsFolder(), "KernelExtension"));

    private static string GetTestProjectsFolder()
    {
        var current = Directory.GetCurrentDirectory();
        return Path.Combine(current, "TestProjects");
    }
}