// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.CSharpProject.Build;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

[TestClass]
public static class PrebuildFixture
{
    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext _)
    {
        var consolePrebuild = await Prebuild.GetOrCreateConsolePrebuildAsync(true);
        await consolePrebuild.EnsureReadyAsync();

        consolePrebuild = await Prebuild.GetOrCreateConsolePrebuildAsync(false);
        await consolePrebuild.EnsureReadyAsync();
        Prebuild = consolePrebuild;
    }

    public static Prebuild Prebuild { get; private set; }
}