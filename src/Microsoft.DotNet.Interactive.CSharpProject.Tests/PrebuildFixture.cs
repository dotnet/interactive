// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.CSharpProject.Build;
using Xunit;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public class PrebuildFixture : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        var consolePrebuild = await Prebuild.GetOrCreateConsolePrebuildAsync(true);
        await consolePrebuild.EnsureReadyAsync();

        consolePrebuild = await Prebuild.GetOrCreateConsolePrebuildAsync(false);
        await consolePrebuild.EnsureReadyAsync();
        Prebuild = consolePrebuild;
    }

    public Prebuild Prebuild { get; private set; }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}