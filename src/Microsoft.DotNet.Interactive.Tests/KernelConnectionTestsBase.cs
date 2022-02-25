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

public abstract class KernelConnectionTestsBase : IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    protected KernelConnectionTestsBase(ITestOutputHelper output)
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

    [WindowsFact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Test only enabled on windows platforms")]
    public async Task it_can_reuse_connection_for_multiple_proxy_kernels()
    {
        var connector = await CreateConnectorAsync();

        // todo: hack to ensure disposal, this should be handled as tear down when client are disposed
        if (connector is IDisposable disposableConnector)
        {
            _disposables.Add(disposableConnector);
        }

        // use same connection to create 2 proxy kernel

        using var proxyKernel1 = await connector.ConnectKernelAsync(new KernelInfo("kernel1"));

        using var proxyKernel2 = await connector.ConnectKernelAsync(new KernelInfo("kernel2"));

        var kernelCommand1 = new SubmitCode("\"echo1\"");

        var kernelCommand2 = new SubmitCode("\"echo2\"");

        var res1 = await proxyKernel1.SendAsync(kernelCommand1);

        var res2 = await proxyKernel2.SendAsync(kernelCommand2);

        var kernelEvents1 = res1.KernelEvents.ToSubscribedList();

        var kernelEvents2 = res2.KernelEvents.ToSubscribedList();

        kernelEvents1.Should().ContainSingle<CommandSucceeded>().Which.Command.As<SubmitCode>().Code.Should()
            .Be(kernelCommand1.Code);

        kernelEvents1.Should().ContainSingle<ReturnValueProduced>().Which.
            FormattedValues.Should().
            ContainSingle(f => f.Value == "echo1");

        kernelEvents2.Should().ContainSingle<CommandSucceeded>().Which.Command.As<SubmitCode>().Code.Should()
            .Be(kernelCommand2.Code);

        kernelEvents2.Should().ContainSingle<ReturnValueProduced>().Which.FormattedValues.Should().ContainSingle(f => f.Value == "echo2");
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

        await CreateConnectorAsync();

        AddKernelConnector(localCompositeKernel);

        var connectToRemoteKernel = CreateConnectCommand();

        var connectResults = await localCompositeKernel.SendAsync(connectToRemoteKernel);

        var codeSubmissionForRemoteKernel = new SubmitCode(@"
#!newKernelName
var x = 1 + 1;
x.Display(""text/plain"");");

        var submissionResults = await localCompositeKernel.SendAsync(codeSubmissionForRemoteKernel);

        connectResults.KernelEvents
                      .ToSubscribedList()
                      .Should()
                      .NotContainErrors();

        submissionResults.KernelEvents
                         .ToSubscribedList()
                         .Should()
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
        var connector = await CreateConnectorAsync();

        using var kernel = await connector.ConnectKernelAsync(new KernelInfo("newKernelName"));

        var markedCode = "var x = 12$$34;";

        MarkupTestFile.GetLineAndColumn(markedCode, out var code, out var line, out var column);

        var result = await kernel.SendAsync(new RequestHoverText(code, new LinePosition(line, column)));

        var events = result.KernelEvents.ToSubscribedList();

        events
            .Should()
            .EventuallyContainSingle<HoverTextProduced>();
    }

    protected abstract Task<IKernelConnector> CreateConnectorAsync();

    protected abstract SubmitCode CreateConnectCommand();

    protected abstract void AddKernelConnector(CompositeKernel compositeKernel);
}