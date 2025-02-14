// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.App.Commands;
using Microsoft.DotNet.Interactive.App.Events;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Jupyter;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Tests;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.DotNet.Interactive.Utility;
using Xunit;
using static Microsoft.DotNet.Interactive.App.CodeExpansion;

namespace Microsoft.DotNet.Interactive.App.Tests;

public class CodeExpansionTests
{
    [Fact]
    public async Task Connection_shortcuts_include_well_known_data_connections()
    {
        using var kernel = CreateKernel()
            .UseCodeExpansions(
                new(codeExpansions: KernelBuilder.GetDataKernelCodeExpansions()));

        var result = await kernel.SendAsync(new RequestCodeExpansionInfos());

        result.Events.Should().NotContainErrors();

        result.Events.Should().ContainSingle<CodeExpansionInfosProduced>()
              .Which
              .CodeExpansionInfos
              .Should()
              .Contain(KernelBuilder.GetDataKernelCodeExpansions().Select( e => e.Info));
    }

    [Fact]
    public async Task When_connect_directive_is_run_then_recent_connection_list_item_is_added()
    {
        RecentConnectionList recentConnectionList = new();

        using var kernel = CreateKernel()
            .UseCodeExpansions(new() { GetRecentConnections = () => recentConnectionList });

        kernel.AddConnectDirective(
            new ConnectFakeKernelDirective("fake", name => Task.FromResult<Kernel>(new FakeKernel(name))));

        await kernel.SendAsync(new SubmitCode("#!connect fake --kernel-name myFakeKernel --fakeness-level 9000", "csharp"));

        recentConnectionList
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeEquivalentTo(
                new CodeExpansion(
                    [new("#!connect fake --kernel-name myFakeKernel --fakeness-level 9000", "csharp")],
                    new CodeExpansionInfo("myFakeKernel", CodeExpansionKind.RecentConnection)));
    }

    [Fact]
    public async Task When_connect_directive_is_run_then_code_expansion_is_added_to_config()
    {
        RecentConnectionList recentConnectionList = new();

        var config = new CodeExpansionConfiguration
        {
            GetRecentConnections = () => recentConnectionList
        };

        using var kernel = CreateKernel()
            .UseCodeExpansions(config);

        kernel.AddConnectDirective(
            new ConnectFakeKernelDirective("fake", name => Task.FromResult<Kernel>(new FakeKernel(name))));

        await kernel.SendAsync(new SubmitCode("#!connect fake --kernel-name myFakeKernel --fakeness-level 9000", "csharp"));

        var codeExpansion = await config.GetCodeExpansionAsync("myFakeKernel");

        codeExpansion.Should().NotBeNull();

        codeExpansion.Content.Should().ContainSingle()
                     .Which.Code.Should().Be("#!connect fake --kernel-name myFakeKernel --fakeness-level 9000");
    }

    [Fact]
    public async Task When_connect_directive_comes_from_nuget_package_then_pound_r_is_included_in_the_connection_shortcut()
    {
        RecentConnectionList recentConnectionList = new();

        var config = new CodeExpansionConfiguration
        {
            GetRecentConnections = () => recentConnectionList
        };

        var kernel = new CompositeKernel
        {
            new CSharpKernel()
                .UseNugetDirective()
        }.UseCodeExpansions(config)
         .UseNuGetExtensions();

        var extensionPackage = await KernelExtensionTestHelper.GetKernelConnectionExtensionPackageAsync();

        var packageLoadingCodeSubmission = $"""
                                            #i "nuget:{extensionPackage.PackageLocation}"
                                            #r "nuget:{extensionPackage.Name},{extensionPackage.Version}"
                                            """;
        var result = await kernel.SubmitCodeAsync(packageLoadingCodeSubmission);

        result.Events.Should().NotContainErrors();

        result = await kernel.SendAsync(
                     new SubmitCode("""
                                    #!connect mykernel --kernel-name mine
                                    """));

        result.Events.Should().NotContainErrors();

        recentConnectionList.Should()
                            .ContainSingle()
                            .Which
                            .Should()
                            .BeEquivalentTo(
                                new CodeExpansion(
                                [
                                    new(packageLoadingCodeSubmission, "csharp"),
                                    new("#!connect mykernel --kernel-name mine", "csharp")
                                ], new("mine", CodeExpansionKind.RecentConnection)));
    }

    [Fact]
    public async Task Triggering_a_code_expansion_causes_a_SendEditableCode_to_be_sent_with_the_connect_code()
    {
        using var kernel = CreateKernel().UseCodeExpansions(new(KernelBuilder.GetDataKernelCodeExpansions()));

        List<SendEditableCode> receivedSendEditableCode = new();

        kernel.RegisterCommandHandler<SendEditableCode>((command, _) =>
        {
            receivedSendEditableCode.Add(command);
            return Task.CompletedTask;
        });

        var result = await kernel.SendAsync(
                         new SubmitCode("""
                                        #!expand "Kusto Query Language"
                                        """));

        result.Events.Should().NotContainErrors();

        var expectedCodeSubmissions = KernelBuilder.GetDataKernelCodeExpansions().Single(e => e.Info.Name == "Kusto Query Language").Content;

        // The order of the submissions is reversed so that they come out in the correct order in the notebook
        receivedSendEditableCode[0].Code.Should().Be(expectedCodeSubmissions[0].Code);
        receivedSendEditableCode[1].Code.Should().Be(expectedCodeSubmissions[1].Code);
    }

    [Fact]
    public async Task Jupyter_kernelspec_expansions_are_correctly_generated_from_kernelspecs()
    {
        var config = new CodeExpansionConfiguration(
            kernelSpecModule: new FakeKernelSpecModule(new()
            {
                ["python3"] = new KernelSpec
                {
                    DisplayName = "Python 3 (ipykernel)",
                    Language = "python"
                }
            }));

        var actualCodeExpansion = await config.GetCodeExpansionAsync("python3");

        var expectedCodeExpansion = new CodeExpansion(
            content: [new("#!connect jupyter --kernel-name python3 --kernel-spec python3", "csharp")],
            new CodeExpansionInfo("python3", CodeExpansionKind.KernelSpecConnection)
            {
                Description = "Python 3 (ipykernel)"
            });

        actualCodeExpansion.Should().BeEquivalentTo(expectedCodeExpansion);
    }

    private class FakeKernelSpecModule : IJupyterKernelSpecModule
    {
        private readonly IReadOnlyDictionary<string, KernelSpec> _kernelSpecs;

        public FakeKernelSpecModule(Dictionary<string, KernelSpec> kernelSpecs)
        {
            _kernelSpecs = kernelSpecs;
        }

        public Task<CommandLineResult> InstallKernelAsync(DirectoryInfo sourceDirectory)
        {
            throw new NotImplementedException();
        }

        public DirectoryInfo GetDefaultKernelSpecDirectory()
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyDictionary<string, KernelSpec>> ListKernelsAsync()
        {
            return Task.FromResult(_kernelSpecs);
        }

        public IJupyterEnvironment GetEnvironment()
        {
            throw new NotImplementedException();
        }
    }

    private static CompositeKernel CreateKernel()
    {
        var pwshKernel = new PowerShellKernel();
        return new CompositeKernel
        {
            new CSharpKernel(),
            pwshKernel
        }.UseSecretManager(new SecretManager(pwshKernel));
    }
}