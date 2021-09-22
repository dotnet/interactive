// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
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

            // setup connection

            //var namedPipeConnection = new NamedPipeKernelConnection(pipeName);

            //var localKernel1 =  await namedPipeConnection.ConnectKernelAsync(new KernelName("kernel1"));

            //var localKernel2 = await namedPipeConnection.ConnectKernelAsync(new KernelName("kernel2"));

            //var kernelEvents1 = localKernel1.KernelEvents.ToSubscribedList();

            //var kernelEvents2 = localKernel2.KernelEvents.ToSubscribedList();

            

            throw new NotImplementedException();
        }

        [FactSkipLinux]
        public async Task can_address_remote_composite_kernel_using_named_pipe()
        {
            using var localCompositeKernel = new CompositeKernel
            {
                new FSharpKernel()
            }.UseKernelClientConnection(new ConnectNamedPipe());

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