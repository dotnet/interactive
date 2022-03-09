// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Pocket;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class KernelRoutingTests : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();

        public KernelRoutingTests(ITestOutputHelper output)
        {
            _disposables.Add(output.SubscribeToPocketLogger());
        }

        public void Dispose() => _disposables.Dispose();

        [WindowsFact]
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Test only enabled on windows platforms")]
        public async Task proxyKernel_does_not_perform_split_if_all_parts_go_to_same_targetKernel_as_the_original_command()
        {
            var handledCommands = new List<KernelCommand>();
            using var remoteCompositeKernel = new CompositeKernel
            {
                new FakeKernel("csharp")
                {
                    Handle = (command, _) =>
                    {
                        handledCommands.Add(command);
                        return Task.CompletedTask;
                    }
                },
                new FakeKernel("fsharp")
                {
                    Handle = (_, _) => Task.CompletedTask
                }
            };

            remoteCompositeKernel.DefaultKernelName = "csharp";
            var pipeName = Guid.NewGuid().ToString();

            StartServer(remoteCompositeKernel, pipeName);

            var connection = new NamedPipeKernelConnector(pipeName);

            var proxyKernel = await connection.ConnectKernelAsync("proxyKernel");

            var code = @"#i ""nuget:source1""
#i ""nuget:source2""
#r ""nuget:package1""
#r ""nuget:package2""

Console.WriteLine(1);";

            var command = new SubmitCode(code, proxyKernel.Name);
            await proxyKernel.SendAsync(command);

            handledCommands.Should().ContainSingle<SubmitCode>();
        }


        [WindowsFact]
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Test only enabled on windows platforms")]
        public async Task proxyKernel_does_not_perform_split_if_all_parts_go_to_same_targetKernel_original_command_has_not_target_kernel()
        {
            var handledCommands = new List<KernelCommand>();
            using var remoteCompositeKernel = new CompositeKernel
            {
                new FakeKernel("csharp")
                {
                    Handle = (command, _) =>
                    {
                        handledCommands.Add(command);
                        return Task.CompletedTask;
                    }
                },
                new FakeKernel("fsharp")
                {
                    Handle = (_, _) => Task.CompletedTask
                },
            };

            remoteCompositeKernel.DefaultKernelName = "csharp";
            var pipeName = Guid.NewGuid().ToString();

            using var _ = StartServer(remoteCompositeKernel, pipeName);

            var connection = new NamedPipeKernelConnector(pipeName);

            var proxyKernel = await connection.ConnectKernelAsync("proxyKernel");

            var code = @"#i ""nuget:source1""
#i ""nuget:source2""
#r ""nuget:package1""
#r ""nuget:package2""

Console.WriteLine(1);";

            var command = new SubmitCode(code);
            await proxyKernel.SendAsync(command);

            handledCommands.Should().ContainSingle<SubmitCode>();
        }

        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Test only enabled on windows platforms")]
        KernelHost StartServer(CompositeKernel remoteKernel, string pipeName)
        {
            var serverStream = new NamedPipeServerStream(
                pipeName, 
                PipeDirection.InOut, 
                1, 
                PipeTransmissionMode.Message, 
                PipeOptions.Asynchronous);
            
            var kernelCommandAndEventPipeStreamReceiver = new KernelCommandAndEventPipeStreamReceiver(serverStream);
            
            var kernelCommandAndEventPipeStreamSender = new KernelCommandAndEventPipeStreamSender(serverStream);
            
            var host = remoteKernel.UseHost(
                kernelCommandAndEventPipeStreamSender,
                new MultiplexingKernelCommandAndEventReceiver(kernelCommandAndEventPipeStreamReceiver));
            
            remoteKernel.RegisterForDisposal(serverStream);

            Task.Run(() =>
            {
                serverStream.WaitForConnection();
                var _ = host.ConnectAsync();
            });

            return host;
        }
    }
}
