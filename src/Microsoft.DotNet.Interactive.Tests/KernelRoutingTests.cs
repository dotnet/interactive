// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Server;
using Microsoft.DotNet.Interactive.Tests.Parsing;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Pocket;
using Xunit;
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

        public void Dispose()
        {
            _disposables.Dispose();
        }
        
        [Fact]
        public void the_host_provides_uri_for_kernels()
        {
            using var composite = new CompositeKernel();
            using var host = KernelHost.InProcess(composite);

            var child = new FakeKernel("localName");
            composite.Add(child);

            host.TryGetKernelInfo(child, out var kernelInfo);
            kernelInfo.OriginUri.Should().NotBeNull();
            host.Uri.IsBaseOf(kernelInfo.OriginUri).Should().Be(true);
        }

        [Fact]
        public void when_attaching_host_to_composite_kernels_subkernels_are_provided_with_uri()
        {
            using var composite = new CompositeKernel();
            var child = new FakeKernel("localName");
            composite.Add(child);

            using var host = KernelHost.InProcess(composite);

            host.TryGetKernelInfo(child, out var kernelInfo);
            kernelInfo.OriginUri.Should().NotBeNull();
            host.Uri.IsBaseOf(kernelInfo.OriginUri).Should().Be(true);
        }

        [Fact]
        public void detached_kernels_do_not_have_uri()
        {
            using var composite = new CompositeKernel();
            using var host = KernelHost.InProcess(composite);
            var child = new FakeKernel("localName");

            var found = host.TryGetKernelInfo(child, out _);
            found.Should().Be(false);
        }


        [FactSkipLinux]
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
                },
            };

            remoteCompositeKernel.DefaultKernelName = "csharp";
            var pipeName = Guid.NewGuid().ToString();

            StartServer(remoteCompositeKernel, pipeName);

            var connection = new NamedPipeKernelConnector(pipeName);

            var proxyKernel = await connection.ConnectKernelAsync(new KernelInfo("proxyKernel"));
            
            var code = @"#i ""nuget:source1""
#i ""nuget:source2""
#r ""nuget:package1""
#r ""nuget:package2""

Console.WriteLine(1);";

            var command = new SubmitCode(code, proxyKernel.Name);
            await proxyKernel.SendAsync(command);

            handledCommands.Should().ContainSingle<SubmitCode>();
        }


        [FactSkipLinux]
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

            StartServer(remoteCompositeKernel, pipeName);

            var connection = new NamedPipeKernelConnector(pipeName);

            var proxyKernel = await connection.ConnectKernelAsync(new KernelInfo("proxyKernel"));

            var code = @"#i ""nuget:source1""
#i ""nuget:source2""
#r ""nuget:package1""
#r ""nuget:package2""

Console.WriteLine(1);";

            var command = new SubmitCode(code);
            await proxyKernel.SendAsync(command);

            handledCommands.Should().ContainSingle<SubmitCode>();
        }

        void StartServer(CompositeKernel remoteKernel, string pipeName)
        {
           
            var serverStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            var kernelCommandAndEventPipeStreamReceiver = new KernelCommandAndEventPipeStreamReceiver(serverStream);
            var kernelCommandAndEventPipeStreamSender = new KernelCommandAndEventPipeStreamSender(serverStream);
            var host = new KernelHost(remoteKernel,
                kernelCommandAndEventPipeStreamSender,
                new MultiplexingKernelCommandAndEventReceiver(kernelCommandAndEventPipeStreamReceiver));
            remoteKernel.RegisterForDisposal(serverStream);

            Task.Run(() =>
            {
                serverStream.WaitForConnection();
                var _ = host.ConnectAsync();
            });
        }
    }
}
