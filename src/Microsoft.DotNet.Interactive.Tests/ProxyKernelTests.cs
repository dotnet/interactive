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
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class ProxyKernelTests : IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public ProxyKernelTests(ITestOutputHelper output)
        {
            _disposables.Add(output.SubscribeToPocketLogger());
        }

        public void Dispose()
        {
            _disposables.Dispose();
        } 
        [FactSkipLinux]
        public async Task Handling_kernel_can_be_specified_using_kernel_name_as_a_directive_as_a_proxy_named_pipe()
        {
            var fSharpKernel = new FSharpKernel();
            using var kernel = new CompositeKernel
            {
                fSharpKernel
            }.UseProxyKernel();
            kernel.DefaultKernelName = fSharpKernel.Name;

            var pipeName = Guid.NewGuid().ToString();
            using var cSharpKernel = new CSharpKernel();
            Action doWait = () =>
                Task.Run(() => NamedPipeKernelServer.WaitForConnection(cSharpKernel, pipeName));
            doWait();

            using var events = kernel.KernelEvents.ToSubscribedList();

            var proxyCommand = new SubmitCode($"#!connect test {pipeName}");

            await kernel.SendAsync(proxyCommand);

            var proxyCommand2 = new SubmitCode(@"
var x = 1 + 1;
x", targetKernelName: "test");

            await kernel.SendAsync(proxyCommand2);

            events.Should()
                  .ContainSingle<CommandHandled>(e => e.Command == proxyCommand);
        }

        [FactSkipLinux]
        public async Task Handling_kernel_can_be_specified_using_kernel_name_as_a_directive_as_a_proxy_named_pipe2()
        {
            var fSharpKernel = new FSharpKernel();
            using var kernel = new CompositeKernel
            {
                fSharpKernel
            }.UseProxyKernel();
            kernel.DefaultKernelName = fSharpKernel.Name;

            var pipeName = Guid.NewGuid().ToString();
            using var cSharpKernel = new CSharpKernel();
            Action doWait = () =>
                Task.Run(() => NamedPipeKernelServer.WaitForConnection(cSharpKernel, pipeName));
            doWait();

            using var events = kernel.KernelEvents.ToSubscribedList();

            var proxyCommand = new SubmitCode($"#!connect test {pipeName}");

            await kernel.SendAsync(proxyCommand);

            var proxyCommand2 = new SubmitCode(@"
#!test
var x = 1 + 1;
x");

            await kernel.SendAsync(proxyCommand2);

            var proxyCommand3 = new SubmitCode(@"
#!test
var y = x + x;
y");

            await kernel.SendAsync(proxyCommand3);

            events.Should()
                  .ContainSingle<CommandHandled>(e => e.Command == proxyCommand2);

            events.Should()
                  .ContainSingle<CommandHandled>(e => e.Command == proxyCommand3);
        }
    }
}
