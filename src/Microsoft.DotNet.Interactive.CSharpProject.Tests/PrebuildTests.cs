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

public class PrebuildTests : IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public PrebuildTests(ITestOutputHelper output)
    {
        _disposables.Add(output.SubscribeToPocketLogger());
    }

    public void Dispose() => _disposables.Dispose();

    [Fact]
    public async Task A_prebuild_is_not_initialized_more_than_once()
    {
        var initializer = new TestPrebuildInitializer(
            "console",
            "MyProject");

        var prebuild = PrebuildUtilities.CreateEmptyBuildablePrebuild(initializer: initializer);

        await prebuild.GetOrCreateWorkspaceAsync();
        await prebuild.GetOrCreateWorkspaceAsync();

        initializer.InitializeCount.Should().Be(1);
    }

    [Fact]
    public async Task Prebuild_after_create_actions_are_not_run_more_than_once()
    {
        var afterCreateCallCount = 0;

        var initializer = new PrebuildInitializer(
            "console",
            "test",
            afterCreate: async _ =>
            {
                await Task.Yield();
                afterCreateCallCount++;
            });

        var prebuild = PrebuildUtilities.CreateEmptyBuildablePrebuild(initializer: initializer);

        await prebuild.GetOrCreateWorkspaceAsync();
        await prebuild.GetOrCreateWorkspaceAsync();

        afterCreateCallCount.Should().Be(1);
    }

    [Fact]
    public async Task A_prebuild_copy_is_not_reinitialized_if_the_source_was_already_initialized()
    {
        var initializer = new TestPrebuildInitializer(
            "console",
            "MyProject");

        var original = PrebuildUtilities.CreateEmptyBuildablePrebuild(initializer: initializer);

        await original.GetOrCreateWorkspaceAsync();

        var copy = await original.CreateBuildableCopy();

        await copy.GetOrCreateWorkspaceAsync();

        initializer.InitializeCount.Should().Be(1);
    }
    
    [Fact]
    public async Task When_prebuild_contains_simple_console_app_then_entry_point_dll_is_in_the_build_directory()
    {
        var prebuild = PrebuildUtilities.CreateEmptyBuildablePrebuild(initializer: new PrebuildInitializer("console", "empty"));

        await prebuild.GetOrCreateWorkspaceAsync();

        prebuild.EntryPointAssemblyPath.Exists.Should().BeTrue();

        prebuild.EntryPointAssemblyPath
            .FullName
            .Should()
            .Be(Path.Combine(
                prebuild.Directory.FullName,
                "bin",
                "Debug",
                prebuild.TargetFramework,
                "empty.dll"));
    }

    [Fact]
    public async Task If_a_build_is_in_flight_then_the_second_one_will_wait_on_it_to_complete()
    {
        var buildEvents = new LogEntryList();
        var buildEventsMessages = new List<string>();
        var prebuild = await PrebuildUtilities.CreateBuildableConsolePrebuildCopy();
        var barrier = new Barrier(2);
        LogEvents.Subscribe(
            e =>
            {
                buildEvents.Add(e);
                buildEventsMessages.Add(e.Evaluate().Message);
                if (e.Evaluate().Message.StartsWith("Building prebuild "))
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
            Task.Run(() => prebuild.BuildAsync()),
            Task.Run(() => prebuild.BuildAsync()));

        buildEventsMessages.Should()
                           .Contain(e => e.StartsWith($"Building prebuild '{prebuild.Name}'"))
                           .And
                           .Contain(e => e.StartsWith($"Skipping build for prebuild '{prebuild.Name}'"));
    }

    [Fact]
    public async Task Directory_is_created_on_demand_during_build()
    {
        DirectoryInfo directory = new(Path.Combine(Prebuild.DefaultPrebuildsDirectory.FullName, Guid.NewGuid().ToString("N")));

        var prebuild = new Prebuild("console", enableBuild: true, directory: directory);

        await prebuild.BuildAsync();

        directory.Exists.Should().BeTrue();
    }
}