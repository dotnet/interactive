// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.CSharpProject.Build;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public sealed class PrebuildFixture
{
    private bool _alreadyInitialized;

    private PrebuildFixture()
    {
    }

    public static PrebuildFixture Instance { get; } = new PrebuildFixture();

    public async Task InitializeAsync()
    {
        if (_alreadyInitialized)
        {
            return;
        }

        var consolePrebuild = await Prebuild.GetOrCreateConsolePrebuildAsync(true);
        await consolePrebuild.EnsureReadyAsync();

        consolePrebuild = await Prebuild.GetOrCreateConsolePrebuildAsync(false);
        await consolePrebuild.EnsureReadyAsync();
        Prebuild = consolePrebuild;

        _alreadyInitialized = true;
    }

    public Prebuild Prebuild { get; private set; }
}