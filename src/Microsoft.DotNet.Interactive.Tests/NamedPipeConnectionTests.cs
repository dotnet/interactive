// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.FSharp;

using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests;

public class NamedPipeConnectionTests : ProxyKernelConnectionTestsBase
{
    private readonly string _pipeName = Guid.NewGuid().ToString();

    public NamedPipeConnectionTests(ITestOutputHelper output) : base(output)
    {
    }

    protected override Func<string, Task<ProxyKernel>> CreateConnector()
    {
        CreateRemoteKernelTopology(_pipeName);

        var connector = new NamedPipeKernelConnector(_pipeName);

        RegisterForDisposal(connector);

        return name => connector.CreateKernelAsync(name);
    }

    protected override SubmitCode CreateConnectCommand(string localKernelName)
    {
        return new SubmitCode($"#!connect named-pipe --kernel-name {localKernelName} --pipe-name {_pipeName}");
    }

    protected override void AddKernelConnector(CompositeKernel compositeKernel)
    {
        compositeKernel.AddKernelConnector(new ConnectNamedPipeCommand());
    }

    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Test only enabled on windows platforms")]
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