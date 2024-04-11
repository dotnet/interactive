// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.CSharpProject.Build;

public static class PrebuildFinder
{
    public static Task<Prebuild> FindAsync(
        this IPrebuildFinder finder,
        string prebuildName) =>
        finder.FindAsync(new PrebuildDescriptor(prebuildName));

    public static IPrebuildFinder Create(Func<Task<Prebuild>> getPrebuildAsync)
    {
        return new AnonymousPrebuildFinder(getPrebuildAsync);
    }

    private class AnonymousPrebuildFinder : IPrebuildFinder
    {
        private readonly AsyncLazy<Prebuild> _lazyPrebuild;

        public AnonymousPrebuildFinder(Func<Task<Prebuild>> getPrebuildAsync)
        {
            _lazyPrebuild = new(getPrebuildAsync);
        }

        public async Task<Prebuild> FindAsync(PrebuildDescriptor descriptor)
        {
            return await _lazyPrebuild.ValueAsync();
        }
    }
}