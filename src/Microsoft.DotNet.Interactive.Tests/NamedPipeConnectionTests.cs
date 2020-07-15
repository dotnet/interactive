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
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public NamedPipeConnectionTests(ITestOutputHelper output)
        {
            _disposables.Add(output.SubscribeToPocketLogger());
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        [FactSkipLinux]
        public async Task can_address_remote_composite_kernel_using_named_pipe()
        {
            using var localCompositeKernel = new CompositeKernel
            {
                new FSharpKernel()
            }.UseConnection(new ConnectNamedPipe());

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

            remoteDefaultKernelInvoked.Should()
                                      .BeTrue();
        }

        void StartServer(Kernel remoteKernel, string pipeName) => Task.Run(() => { remoteKernel.EnableApiOverNamedPipe(pipeName, new DirectoryInfo(Environment.CurrentDirectory)); });
    }
}