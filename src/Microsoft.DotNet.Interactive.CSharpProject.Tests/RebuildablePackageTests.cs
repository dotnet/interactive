// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using Pocket;
using Microsoft.DotNet.Interactive.CSharpProject.Packaging;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public class RebuildablePackageTests : IDisposable
{
    private readonly CompositeDisposable disposables = new();

    public RebuildablePackageTests(ITestOutputHelper output)
    {
        disposables.Add(output.SubscribeToPocketLogger());
    }

    public void Dispose() => disposables.Dispose();

    [Fact(Skip = "Review this")]
    public async Task If_a_new_file_is_added_the_workspace_includes_the_file()
    {
        var package = (RebuildablePackage)await Create.ConsoleWorkspaceCopy(isRebuildable: true);
        var ws = await package.CreateWorkspaceForRunAsync();

        var newFile = Path.Combine(package.Directory.FullName, "Sample.cs");
        ws.CurrentSolution.Projects.First().Documents.Select(d => d.FilePath).Should().NotContain( newFile);

        File.WriteAllText(newFile, "//this is a new file");

        ws = await package.CreateWorkspaceForRunAsync();

        ws.CurrentSolution.Projects.First().Documents.Select(d => d.FilePath).Should().Contain(newFile);
    }

    [Fact]
    public async Task If_an_already_built_package_contains_new_file_the_new_workspace_contains_the_file()
    {
        var oldPackage = await Create.ConsoleWorkspaceCopy(isRebuildable:true);
        var ws = await oldPackage.CreateWorkspaceForRunAsync();

        var newFile = Path.Combine(oldPackage.Directory.FullName, "Sample.cs");
        ws.CurrentSolution.Projects.First().Documents.Select(d => d.FilePath).Should().NotContain(newFile);

        File.WriteAllText(newFile, "//this is a new file");

        var newPackage = new RebuildablePackage(directory: oldPackage.Directory);
        ws = await newPackage.CreateWorkspaceForRunAsync();

        ws.CurrentSolution.Projects.First().Documents.Select(d => d.FilePath).Should().Contain(newFile);
    }

    [Fact]
    public async Task If_an_already_built_package_contains_a_new_file_and_an_old_file_is_deleted_workspace_reflects_it()
    {
        var oldPackage = await Create.ConsoleWorkspaceCopy(isRebuildable: true);

        var sampleCsFile = Path.Combine(oldPackage.Directory.FullName, "Sample.cs");
        File.WriteAllText(sampleCsFile, "//this is a file which will be deleted later");
        var ws = await oldPackage.CreateWorkspaceForRunAsync();
        ws.CurrentSolution.Projects.First().Documents.Select(d => d.FilePath).Should().Contain(sampleCsFile);

        File.Delete(sampleCsFile);
        var newFileAdded = Path.Combine(oldPackage.Directory.FullName, "foo.cs");
        File.WriteAllText(newFileAdded, "//this is a file we have just created");

        var newPackage = new RebuildablePackage(directory: oldPackage.Directory);
        ws = await newPackage.CreateWorkspaceForRunAsync();

        ws.CurrentSolution.Projects.First().Documents.Select(d => d.FilePath).Should().NotContain(sampleCsFile);
        ws.CurrentSolution.Projects.First().Documents.Select(d => d.FilePath).Should().Contain(newFileAdded);
    }

    [Fact]
    public async Task If_an_existing_file_is_modified_then_the_workspace_is_updated()
    {
        var package = (RebuildablePackage)await Create.ConsoleWorkspaceCopy(isRebuildable: true);
        var oldWorkspace = await package.CreateWorkspaceForRunAsync();

        var existingFile = Path.Combine(package.Directory.FullName, "Program.cs");
        File.WriteAllText(existingFile, "//this is Program.cs");
        await Task.Delay(1000);

        var newWorkspace = await package.CreateWorkspaceForRunAsync();

        newWorkspace.Should().NotBeSameAs(oldWorkspace);
    }

    [Fact]
    public async Task If_a_build_is_in_progress_and_another_request_comes_in_both_are_resolved_using_the_final_one()
    {
        var vt = new TestScheduler();
        var package = (RebuildablePackage)await Create.ConsoleWorkspaceCopy(isRebuildable: true, buildThrottleScheduler: vt);
        var workspace1 = package.CreateWorkspaceForRunAsync();
        vt.AdvanceBy(TimeSpan.FromSeconds(0.2).Ticks);
        var newFile = Path.Combine(package.Directory.FullName, "Sample.cs");
        File.WriteAllText(newFile, "//this is Sample.cs");
        vt.AdvanceBy(TimeSpan.FromSeconds(0.2).Ticks);
        var workspace2 = package.CreateWorkspaceForRunAsync();
        vt.AdvanceBy(TimeSpan.FromSeconds(0.6).Ticks);


        workspace1.Should().BeSameAs(workspace2);

        var workspaces = await Task.WhenAll(workspace1, workspace2);

        workspaces[0].CurrentSolution.Projects.First().Documents.Select(d => Path.GetFileName(d.FilePath)).Should().Contain("Sample.cs");
        workspaces[1].CurrentSolution.Projects.First().Documents.Select(d => Path.GetFileName(d.FilePath)).Should().Contain("Sample.cs");
    }
}