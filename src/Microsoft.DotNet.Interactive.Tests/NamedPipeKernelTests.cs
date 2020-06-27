// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Server;
using Microsoft.DotNet.Interactive.Tests.Utility;
using FluentAssertions;
using Pocket;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class NamedPipeKernelTests : IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public NamedPipeKernelTests(ITestOutputHelper output)
        {
            _disposables.Add(output.SubscribeToPocketLogger());
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        [FactSkipLinux]
        public async Task can_address_remote_composite_kernel()
        {
            using var localCompositeKernel = new CompositeKernel
            {
                new FSharpKernel()
            }.UseConnectionOverNamedPipe();

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

            var connectToRemoteKernel = new SubmitCode($"#!connect named-pipe newKernelName {pipeName}");
            var codeSubmissionForRemoteKernel = new SubmitCode(@"
#!newKernelName
var x = 1 + 1;
x");

            await localCompositeKernel.SendAsync(connectToRemoteKernel);
            await localCompositeKernel.SendAsync(codeSubmissionForRemoteKernel);

            remoteDefaultKernelInvoked.Should()
                .BeTrue();
        }

        [FactSkipLinux]
        public async Task Handling_kernel_can_be_specified_using_kernel_name_as_a_directive_as_a_proxy_named_pipe()
        {
            var fSharpKernel = new FSharpKernel();
            using var kernel = new CompositeKernel
            {
                fSharpKernel
            }.UseConnectionOverNamedPipe();

            kernel.DefaultKernelName = fSharpKernel.Name;

            var pipeName = Guid.NewGuid().ToString();
            using var remoteKernel = new CSharpKernel();
            StartServer(remoteKernel, pipeName);

            using var events = kernel.KernelEvents.ToSubscribedList();

            var proxyCommand = new SubmitCode($"#!connect named-pipe test {pipeName}");

            await kernel.SendAsync(proxyCommand);

            var proxyCommand2 = new SubmitCode(@"
var x = 1 + 1;
x", targetKernelName: "test");

            await kernel.SendAsync(proxyCommand2);

            events.Should()
                  .ContainSingle<CommandSucceeded>(e => e.Command == proxyCommand);
        }

        void StartServer(Kernel remoteKernel, string pipeName) => Task.Run(() => { remoteKernel.EnableApiOverNamedPipe(pipeName); });

        [FactSkipLinux]
        public async Task Handling_kernel_can_be_specified_using_kernel_name_as_a_directive_as_a_proxy_named_pipe2()
        {
            var fSharpKernel = new FSharpKernel();
            using var localKernel = new CompositeKernel
            {
                fSharpKernel
            }.UseConnectionOverNamedPipe();

            localKernel.DefaultKernelName = fSharpKernel.Name;

            var pipeName = Guid.NewGuid().ToString();
            using var remoteKernel = new CSharpKernel();
            StartServer(remoteKernel, pipeName);

            using var events = localKernel.KernelEvents.ToSubscribedList();

            var proxyCommand = new SubmitCode($"#!connect named-pipe test {pipeName}");

            await localKernel.SendAsync(proxyCommand);

            var proxyCommand2 = new SubmitCode(@"
#!test
var x = 1 + 1;
x");

            await localKernel.SendAsync(proxyCommand2);

            var proxyCommand3 = new SubmitCode(@"
#!test
var y = x + x;
y");

            await localKernel.SendAsync(proxyCommand3);

            events.Should()
                  .ContainSingle<CommandSucceeded>(e => e.Command == proxyCommand2);

            events.Should()
                  .ContainSingle<CommandSucceeded>(e => e.Command == proxyCommand3);
        }
    }
}
