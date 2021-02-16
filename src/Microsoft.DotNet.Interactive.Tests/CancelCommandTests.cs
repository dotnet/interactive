// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class CancelCommandTests : LanguageKernelTestBase
    {
        public CancelCommandTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task cancel_command_cancels_all_deferred_commands_on_composite_kernel()
        {
            var deferredCommandExecuted = false;

            var kernel = CreateKernel();
            
            var deferred = new SubmitCode("placeholder")
            {
                Handler = (command, context) =>
                {
                    deferredCommandExecuted = true;
                    return Task.CompletedTask;
                }
            };

            var cancelCommand = new Cancel();

            kernel.DeferCommand(deferred);

            var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SendAsync(cancelCommand);

            using var _ = new AssertionScope();

            deferredCommandExecuted.Should().BeFalse();

            events
                .Should().ContainSingle<CommandSucceeded>()
                .Which
                .Command
                .Should()
                .Be(cancelCommand);
        }

        [Fact]
        public async Task cancel_command_cancels_all_deferred_commands_on_subkernels()
        {
            var deferredCommandExecuted = false;

            var kernel = CreateKernel();

            var deferred = new SubmitCode("placeholder")
            {
                Handler = (command, context) =>
                {
                    deferredCommandExecuted = true;
                    return Task.CompletedTask;
                }
            };

            var cancelCommand = new Cancel();

            kernel.ChildKernels[0].DeferCommand(deferred);

            await kernel.SendAsync(cancelCommand);

            using var _ = new AssertionScope();

            deferredCommandExecuted.Should().BeFalse();

            KernelEvents
                .Should().ContainSingle<CommandSucceeded>()
                .Which
                .Command
                .Should()
                .Be(cancelCommand);
        }

    }
}