// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Pocket;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests;

public abstract class ProxyKernelConnectionTestsBase : IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    protected ProxyKernelConnectionTestsBase(ITestOutputHelper output)
    {
        _disposables.Add(output.SubscribeToPocketLogger());
    }

    protected void RegisterForDisposal(IDisposable disposable)
    {
        _disposables.Add(disposable);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    [WindowsFact(Skip = "connector reuse needs redesign")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Test only enabled on windows platforms")]
    public async Task it_can_reuse_connection_for_multiple_proxy_kernels()
    {
        var createKernel = CreateConnector();

        // use same connection to create 2 proxy kernel
        using var proxyKernel1 = await createKernel("kernel1");
        proxyKernel1.KernelInfo.SupportedKernelCommands.Add(new(nameof(SubmitCode)));

        using var proxyKernel2 = await createKernel("kernel2");
        proxyKernel2.KernelInfo.SupportedKernelCommands.Add(new(nameof(SubmitCode)));

        var kernelCommand1 = new SubmitCode("\"echo1\"");

        var kernelCommand2 = new SubmitCode("\"echo2\"");

        var result1 = await proxyKernel1.SendAsync(kernelCommand1);

        var result2 = await proxyKernel2.SendAsync(kernelCommand2);

        result1.Events.Should().NotContainErrors()
               .And
               .ContainSingle<CommandSucceeded>()
               .Which
               .Command.As<SubmitCode>()
               .Code
               .Should().Be(kernelCommand1.Code);

        result1.Events.Should()
            .ContainSingle<ReturnValueProduced>()
            .Which
            .FormattedValues
            .Should().ContainSingle(f => f.Value == "echo1");

        result2.Events.Should().NotContainErrors()
            .And
            .ContainSingle<CommandSucceeded>()
            .Which
            .Command.As<SubmitCode>()
            .Code
            .Should().Be(kernelCommand2.Code);

        result2.Events.Should()
            .ContainSingle<ReturnValueProduced>()
            .Which
            .FormattedValues
            .Should().ContainSingle(f => f.Value == "echo2");
    }

    [WindowsFact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Test only enabled on windows platforms")]
    public async Task can_connect_to_remote_using_connect_magic_command()
    {
        using var localCompositeKernel = new CompositeKernel
        {
            new FakeKernel("fsharp")
        };
        localCompositeKernel.DefaultKernelName = "fsharp";

        CreateConnector();

        AddKernelConnector(localCompositeKernel);

        var localKernelName = "newKernelName";

        var connectToRemoteKernel = CreateConnectCommand(localKernelName);

        var connectResults = await localCompositeKernel.SendAsync(connectToRemoteKernel);

        connectResults.Events.Should().NotContainErrors();

        var codeSubmissionForRemoteKernel = new SubmitCode(
            $"""
             #!{localKernelName}
             var x = 1 + 1;
             x.Display("text/plain");
             """);

        var submissionResults = await localCompositeKernel.SendAsync(codeSubmissionForRemoteKernel);

        submissionResults.Events
                         .Should()
                         .NotContainErrors()
                         .And
                         .ContainSingle<DisplayedValueProduced>()
                         .Which
                         .FormattedValues
                         .Single()
                         .Value
                         .Should()
                         .Be("2");
    }

    [WindowsFact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Test only enabled on windows platforms")]
    public async Task fast_path_commands_over_proxy_can_be_handled()
    {
        var createKernel = CreateConnector();

        using var kernel = await createKernel("newKernelName");
        kernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(RequestHoverText)));

        var markedCode = "var x = 12$$34;";

        MarkupTestFile.GetLineAndColumn(markedCode, out var code, out var line, out var column);

        var result = await kernel.SendAsync(new RequestHoverText(code, new LinePosition(line, column)));

        result.Events
              .Should()
              .ContainSingle<HoverTextProduced>();
    }

    protected abstract Func<string, Task<ProxyKernel>> CreateConnector();

    protected abstract SubmitCode CreateConnectCommand(string localKernelName);

    protected abstract void AddKernelConnector(CompositeKernel compositeKernel);
}