// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using FluentAssertions.Extensions;
using System.Threading.Tasks;
using Pocket;
using Xunit;
using Xunit.Abstractions;
using Microsoft.DotNet.Interactive.CSharpProject.Build;
using System.Threading;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public class PackageTests : IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public PackageTests(ITestOutputHelper output)
    {
        _disposables.Add(output.SubscribeToPocketLogger());
    }

    public void Dispose() => _disposables.Dispose();

    [Fact]
    public async Task A_package_is_not_initialized_more_than_once()
    {
        var initializer = new TestPackageInitializer(
            "console",
            "MyProject");

        var package = PackageUtilities.CreateEmptyBuildablePackage(initializer: initializer);

        await package.GetOrCreateWorkspaceAsync();
        await package.GetOrCreateWorkspaceAsync();

        initializer.InitializeCount.Should().Be(1);
    }

    [Fact]
    public async Task Package_after_create_actions_are_not_run_more_than_once()
    {
        var afterCreateCallCount = 0;

        var initializer = new PackageInitializer(
            "console",
            "test",
            afterCreate: async _ =>
            {
                await Task.Yield();
                afterCreateCallCount++;
            });

        var package = PackageUtilities.CreateEmptyBuildablePackage(initializer: initializer);

        await package.GetOrCreateWorkspaceAsync();
        await package.GetOrCreateWorkspaceAsync();

        afterCreateCallCount.Should().Be(1);
    }

    [Fact]
    public async Task A_package_copy_is_not_reinitialized_if_the_source_was_already_initialized()
    {
        var initializer = new TestPackageInitializer(
            "console",
            "MyProject");

        var original = PackageUtilities.CreateEmptyBuildablePackage(initializer: initializer);

        await original.GetOrCreateWorkspaceAsync();

        var copy = await original.CreateBuildableCopy();

        await copy.GetOrCreateWorkspaceAsync();

        initializer.InitializeCount.Should().Be(1);
    }
    
    [Fact]
    public async Task When_package_contains_simple_console_app_then_entry_point_dll_is_in_the_build_directory()
    {
        var package = PackageUtilities.CreateEmptyBuildablePackage(initializer: new PackageInitializer("console", "empty"));

        await package.GetOrCreateWorkspaceAsync();

        package.EntryPointAssemblyPath.Exists.Should().BeTrue();

        package.EntryPointAssemblyPath
            .FullName
            .Should()
            .Be(Path.Combine(
                package.Directory.FullName,
                "bin",
                "Debug",
                package.TargetFramework,
                "empty.dll"));
    }

    [Fact]
    public async Task If_a_build_is_in_flight_then_the_second_one_will_wait_on_it_to_complete()
    {
        var buildEvents = new LogEntryList();
        var buildEventsMessages = new List<string>();
        var package = await PackageUtilities.CreateBuildableConsolePackageCopy();
        var barrier = new Barrier(2);
        LogEvents.Subscribe(
            e =>
            {
                buildEvents.Add(e);
                buildEventsMessages.Add(e.Evaluate().Message);
                if (e.Evaluate().Message.StartsWith("Building package "))
                {
                    barrier.SignalAndWait(30.Seconds());
                }
            }, searchInAssemblies:
            new[]
            {
                typeof(LogEvents).Assembly,
                typeof(ICodeRunner).Assembly
            });

        await Task.WhenAll(
            Task.Run(() => package.DoFullBuildAsync()),
            Task.Run(() => package.DoFullBuildAsync()));

        buildEventsMessages.Should()
                           .Contain(e => e.StartsWith("Building package " + package.Name))
                           .And
                           .Contain(e => e.StartsWith("Skipping build for package " + package.Name));
    }
}