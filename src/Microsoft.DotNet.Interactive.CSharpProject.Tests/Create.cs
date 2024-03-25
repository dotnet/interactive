// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.CSharpProject.Packaging;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public static class Create
{
    public static async Task<Package> BuildableConsolePackageCopy(
        [CallerMemberName] string testName = null,
        IScheduler buildThrottleScheduler = null) =>
        await PackageUtilities.Copy(
            await Package.GetOrCreateConsolePackageAsync(true),
            testName,
            buildThrottleScheduler);

    public static Package EmptyBuildablePackage(
        [CallerMemberName] string testName = null,
        IPackageInitializer initializer = null)
    {
        return new Package(
            name: testName,
            directory: PackageUtilities.CreateDirectory(testName),
            initializer: initializer,
            enableBuild: true);
    }
}