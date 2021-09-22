// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Server;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Pocket;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class NamedPipeConnectionTests : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();

        public NamedPipeConnectionTests(ITestOutputHelper output)
        {
            _disposables.Add(output.SubscribeToPocketLogger());
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        [FactSkipLinux]
        public async Task it_can_reuse_connection_for_multiple_proxy_kernel()
        {
            var pipeName = Guid.NewGuid().ToString();

            // start server
            
            using var remoteCompositeKernel = new CompositeKernel
            {
                new FakeKernel("csharp")
                {
                    Handle = (command, context) =>
                    {
                        if (command is SubmitCode sc)
                        {
                            context.Display(sc.Code);
                        }
                        return Task.CompletedTask;
                    }
                },
                new FakeKernel("powershell")
            };

            remoteCompositeKernel.DefaultKernelName = "csharp";

            StartServer(remoteCompositeKernel, pipeName);

            // setup connection

            var connector = new NamedPipeKernelConnector(pipeName);

            // use same connection to create 2 proxy kernel

            using var localKernel1 =  await connector.ConnectKernelAsync(new KernelName("kernel1"));

            using var localKernel2 = await connector.ConnectKernelAsync(new KernelName("kernel2"));

            var kernelCommand1 = new SubmitCode("echo1");

            var kernelCommand2 = new SubmitCode("echo2");

            var res1 = await localKernel1.SendAsync(kernelCommand1);

            var res2 = await localKernel2.SendAsync(kernelCommand2);

            var kernelEvents1 = res1.KernelEvents.ToSubscribedList();

            var kernelEvents2 = res2.KernelEvents.ToSubscribedList();

            kernelEvents1.Should().ContainSingle<CommandSucceeded>().Which.Command.As<SubmitCode>().Code.Should()
                .Be(kernelCommand1.Code);

            kernelEvents1.Should().ContainSingle<DisplayedValueProduced>().Which.FormattedValues.Should().ContainSingle(f => f.Value == kernelCommand1.Code);

            kernelEvents2.Should().ContainSingle<CommandSucceeded>().Which.Command.As<SubmitCode>().Code.Should()
                .Be(kernelCommand2.Code);

            kernelEvents2.Should().ContainSingle<DisplayedValueProduced>().Which.FormattedValues.Should().ContainSingle(f => f.Value == kernelCommand2.Code);
        }

        [FactSkipLinux]
        public async Task can_address_remote_composite_kernel_using_named_pipe()
        {
            using var localCompositeKernel = new CompositeKernel
            {
                new FSharpKernel()
            }.UseKernelClientConnection(new ConnectNamedPipeCommand());

            localCompositeKernel.DefaultKernelName = "fsharp";

            var pipeName = Guid.NewGuid().ToString();
            var remoteDefaultKernelInvoked = false;

            using var remoteCompositeKernel = new CompositeKernel
            {
                new FakeKernel("csharp")
                {
                    Handle = (command, context) =>
                    {
                        remoteDefaultKernelInvoked = true;
                        return Task.CompletedTask;
                    }
                },
                new FakeKernel("powershell")
            };

            remoteCompositeKernel.DefaultKernelName = "csharp";

            StartServer(remoteCompositeKernel, pipeName);

            using var events = localCompositeKernel.KernelEvents.ToSubscribedList();

            var connectToRemoteKernel = new SubmitCode($"#!connect named-pipe --kernel-name newKernelName --pipe-name {pipeName}");
            var codeSubmissionForRemoteKernel = new SubmitCode(@"
#!newKernelName
var x = 1 + 1;
x");

            await localCompositeKernel.SendAsync(connectToRemoteKernel);
            await localCompositeKernel.SendAsync(codeSubmissionForRemoteKernel);
            
            events.Should().NotContainErrors();

            remoteDefaultKernelInvoked.Should()
                                      .BeTrue();
        }

        void StartServer(Kernel remoteKernel, string pipeName) =>  remoteKernel.UseNamedPipeKernelServer(pipeName, new DirectoryInfo(Environment.CurrentDirectory)); 
    }
}