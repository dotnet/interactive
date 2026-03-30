// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.CSharpProject.Build;
using Xunit;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public class PrebuildFixture : IAsyncLifetime
{
    internal const string NuGetConfigContent =
        """
        <?xml version="1.0" encoding="utf-8"?>
        <configuration>
          <packageSources>
            <clear />
            <add key="dotnet-public" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json" />
          </packageSources>
        </configuration>
        """;

    public async Task InitializeAsync()
    {
        var consolePrebuild = await Prebuild.GetOrCreateConsolePrebuildAsync(enableBuild: true, nugetConfigContent: NuGetConfigContent);
        await consolePrebuild.EnsureReadyAsync();

        consolePrebuild = await Prebuild.GetOrCreateConsolePrebuildAsync(enableBuild: false, nugetConfigContent: NuGetConfigContent);
        await consolePrebuild.EnsureReadyAsync();
        Prebuild = consolePrebuild;
    }

    public Prebuild Prebuild { get; private set; }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}