// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
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

        public void Dispose() => _disposables.Dispose();

        [Fact]
        public async Task When_target_kernel_name_is_specified_then_ProxyKernel_does_not_split_magics()
        {
            var handledCommands = new List<KernelCommand>();
            using var localCompositeKernel = new CompositeKernel();
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

            ConnectHost.ConnectInProcessHost(
                localCompositeKernel,
                remoteCompositeKernel);

            await localCompositeKernel
                  .Host
                  .ConnectProxyKernelOnDefaultConnectorAsync(
                      "csharp-proxy",
                      new(remoteCompositeKernel.Host.Uri, "csharp"));

            var code = @"#i ""nuget:source1""
#i ""nuget:source2""
#r ""nuget:package1""
#r ""nuget:package2""

Console.WriteLine(1);";

            var command = new SubmitCode(code, "csharp-proxy");
            var result = await localCompositeKernel.SendAsync(command);

            var events = result.KernelEvents.ToSubscribedList();

            events.Should().NotContainErrors();

            handledCommands.Should()
                           .ContainSingle<SubmitCode>()
                           .Which
                           .Code
                           .Should()
                           .Be(code);
        }

        [Fact]
        public async Task When_target_kernel_name_is_not_specified_then_proxyKernel_does_not_split_magics()
        {
            var handledCommands = new List<KernelCommand>();
            using var localCompositeKernel = new CompositeKernel();
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

            ConnectHost.ConnectInProcessHost(
                localCompositeKernel,
                remoteCompositeKernel);

            await localCompositeKernel
                  .Host
                  .ConnectProxyKernelOnDefaultConnectorAsync(
                      "csharp-proxy",
                      new(remoteCompositeKernel.Host.Uri, "csharp"));

            var code = @"#i ""nuget:source1""
#i ""nuget:source2""
#r ""nuget:package1""
#r ""nuget:package2""

Console.WriteLine(1);";

            var command = new SubmitCode(code);
            var result = await localCompositeKernel.SendAsync(command);
            var events = result.KernelEvents.ToSubscribedList();

            handledCommands.Should()
                           .ContainSingle<SubmitCode>()
                           .Which
                           .Code
                           .Should()
                           .Be(code);
        }

        [Fact]
        public async Task A_default_kernel_name_can_be_specified_to_handle_a_command_type()
        {
            KernelCommand receivedByKernelOne = null;
            KernelCommand receivedByKernelTwo = null;

            var kernelOne = new FakeKernel("one");

            kernelOne.AddMiddleware((command, context, next) =>
            {
                receivedByKernelOne = command;
                return Task.CompletedTask;
            });

            var kernelTwo = new FakeKernel("two");

            kernelTwo.AddMiddleware((command, context, next) =>
            {
                receivedByKernelTwo = command;
                return Task.CompletedTask;
            });

            using var compositeKernel = new CompositeKernel
            {
                kernelOne,
                kernelTwo
            };

            compositeKernel.SetDefaultTargetKernelNameForCommand(typeof(RequestInput), "one");

            var command = new RequestInput("Input please!");

            await compositeKernel.SendAsync(command);

            receivedByKernelOne.Should().Be(command);
        }
    }
}
