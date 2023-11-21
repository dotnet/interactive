// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO.Pipes;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Tests;

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.NamedPipeConnector.Tests;

public class NamedPipeConnectionTests : ProxyKernelConnectionTestsBase
{
    private readonly string _pipeName = Guid.NewGuid().ToString();

    public NamedPipeConnectionTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void connect_command_is_available_when_a_user_adds_a_kernel_connection_type()
    {
        using var compositeKernel = new CompositeKernel();

        compositeKernel.AddKernelConnector(new ConnectNamedPipeCommand());

        compositeKernel.Directives
            .Should()
            .Contain(c => c.Name == "#!connect");
    }

    protected override Func<string, Task<ProxyKernel>> CreateConnector()
    {
        CreateRemoteKernelTopology(_pipeName);

        var connector = new NamedPipeKernelConnector(_pipeName);

        RegisterForDisposal(connector);

        return connector.CreateKernelAsync;
    }

    protected override SubmitCode CreateConnectCommand(string localKernelName)
    {
        return new SubmitCode($"#!connect named-pipe --kernel-name {localKernelName} --pipe-name {_pipeName}");
    }

    protected override void AddKernelConnector(CompositeKernel compositeKernel)
    {
        compositeKernel.AddKernelConnector(new ConnectNamedPipeCommand());
    }
   
    private void CreateRemoteKernelTopology(string pipeName)
    {
        var remoteCompositeKernel = new CompositeKernel
        {
            new CSharpKernel(),
            new FSharpKernel()
        };

        remoteCompositeKernel.DefaultKernelName = "csharp";

        RegisterForDisposal(remoteCompositeKernel);

        var serverStream = new NamedPipeServerStream(
            pipeName,
            PipeDirection.InOut,
            1,
            PipeTransmissionMode.Message,
            PipeOptions.Asynchronous);

        var sender = KernelCommandAndEventSender.FromNamedPipe(
            serverStream,
            new Uri("kernel://remote"));

        var receiver = KernelCommandAndEventReceiver.FromNamedPipe(serverStream);

        var host = remoteCompositeKernel.UseHost(sender, receiver, new Uri("kernel://local"));

        var _ = Task.Run(() =>
        {
            // required as waiting connection on named pipe server will block
            serverStream.WaitForConnection();
            var _ = host.ConnectAsync();
        });

        RegisterForDisposal(host);
        RegisterForDisposal(receiver);
        RegisterForDisposal(serverStream);
    }
}