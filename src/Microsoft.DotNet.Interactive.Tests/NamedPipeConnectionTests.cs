// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO.Pipes;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.FSharp;

using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests;

public class NamedPipeConnectionTests : KernelConnectionTestsBase<string>
{
    public NamedPipeConnectionTests(ITestOutputHelper output) : base(output)
    {
    }

    protected override Task<IKernelConnector> CreateConnectorAsync(string pipeName)
    {
        return Task.FromResult<IKernelConnector>(new NamedPipeKernelConnector(pipeName));
    }

    protected override string CreateConnectionConfiguration()
    {
        return Guid.NewGuid().ToString();
    }

    protected override SubmitCode CreateConnectionCommand(string pipeName)
    {
        return new SubmitCode($"#!connect named-pipe --kernel-name newKernelName --pipe-name {pipeName}");
    }

    protected override void ConfigureConnectCommand(CompositeKernel compositeKernel)
    {
        compositeKernel.UseKernelClientConnection(new ConnectNamedPipeCommand());
    }

    protected override Task<KernelHost> CreateRemoteKernelTopologyAsync(string pipeName)
    {
        var remoteCompositeKernel = new CompositeKernel
            {
                new CSharpKernel(),
                new FSharpKernel()
            };

        remoteCompositeKernel.DefaultKernelName = "csharp";
        RegisterForDisposal(remoteCompositeKernel);
        return ConnectHostAsync(remoteCompositeKernel, pipeName);
    }


    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility",
        Justification = "Test only enabled on windows platform")]
    protected override Task<KernelHost> ConnectHostAsync(CompositeKernel remoteKernel, string pipeName)
    {
        var serverStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
        var kernelCommandAndEventPipeStreamReceiver = new KernelCommandAndEventPipeStreamReceiver(serverStream);
        var kernelCommandAndEventPipeStreamSender = new KernelCommandAndEventPipeStreamSender(serverStream);
        var host = new KernelHost(remoteKernel,
            kernelCommandAndEventPipeStreamSender,
            new MultiplexingKernelCommandAndEventReceiver(kernelCommandAndEventPipeStreamReceiver));



        Task.Run(() =>
        {
            // required as waiting connection on named pipe server will block
            serverStream.WaitForConnection();
            var _ = host.ConnectAsync();
        });

        return Task.FromResult(host);
    }
}