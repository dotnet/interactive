// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Pocket;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class QuitCommandTests : LanguageKernelTestBase
    {
        public QuitCommandTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task quit_command_fails_when_not_configured()
        {
            var kernel = CreateKernel();

            var quitCommand = new Quit();

            var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SendAsync(quitCommand);

            using var _ = new AssertionScope();

            events
                .Should().ContainSingle<CommandFailed>()
                .Which
                .Command
                .Should()
                .Be(quitCommand);

            events
                .Should().ContainSingle<CommandFailed>()
                .Which
                .Exception
                .Should()
                .BeOfType<InvalidOperationException>();
        }

        [Fact]
        public async Task quit_command_cancels_all_deferred_commands_on_composite_kernel()
        {
            var deferredCommandExecuted = false;

            var quitCommandExecuted = false;

            var kernel = CreateKernel();

            var deferred = new SubmitCode("placeholder")
            {
                Handler = (command, context) =>
                {
                    deferredCommandExecuted = true;
                    return Task.CompletedTask;
                }
            };


            Quit.OnQuit(() => { quitCommandExecuted = true; });

            var quitCommand = new Quit();

            kernel.DeferCommand(deferred);

            var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SendAsync(quitCommand);

            using var _ = new AssertionScope();

            deferredCommandExecuted.Should().BeFalse();
            quitCommandExecuted.Should().BeTrue();

            events
                .Should().ContainSingle<CommandSucceeded>()
                .Which
                .Command
                .Should()
                .Be(quitCommand);
        }

        [Fact]
        public async Task quit_command_cancels_all_deferred_commands_on_subkernels()
        {
            var deferredCommandExecuted = false;

            var quitCommandExecuted = false;

            var kernel = CreateKernel();

            var deferred = new SubmitCode("placeholder")
            {
                Handler = (command, context) =>
                {
                    deferredCommandExecuted = true;
                    return Task.CompletedTask;
                }
            };

            Quit.OnQuit(() => { quitCommandExecuted = true; });

            var quitCommand = new Quit();

            kernel.ChildKernels[0].DeferCommand(deferred);

            var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SendAsync(quitCommand);

            using var _ = new AssertionScope();

            deferredCommandExecuted.Should().BeFalse();
            quitCommandExecuted.Should().BeTrue();

            events
                .Should().ContainSingle<CommandSucceeded>()
                .Which
                .Command
                .Should()
                .Be(quitCommand);
        }
    }
}