// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
    [Collection("Do not parallelize")]
    public class QuitCommandTests : LanguageKernelTestBase
    {
        public QuitCommandTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task quit_command_fails_when_not_configured()
        {
            var kernel = CreateKernel();
            Quit.OnQuit(null);
            var quitCommand = new Quit();
            
            await kernel.SendAsync(quitCommand);

            using var _ = new AssertionScope();

            KernelEvents
                .Should().ContainSingle<CommandFailed>()
                .Which
                .Command
                .Should()
                .Be(quitCommand);

            KernelEvents
                .Should().ContainSingle<CommandFailed>()
                .Which
                .Exception
                .Should()
                .BeOfType<InvalidOperationException>();
        }

        [Fact(Skip = "requires scheduler working")]
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

            await kernel.SendAsync(quitCommand);

            using var _ = new AssertionScope();

            deferredCommandExecuted.Should().BeFalse();
            quitCommandExecuted.Should().BeTrue();

            KernelEvents
                .Should().ContainSingle<CommandSucceeded>()
                .Which
                .Command
                .Should()
                .Be(quitCommand);
        }

        [Theory(Skip = "requires scheduler working")]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        [InlineData(Language.PowerShell)]
        public async Task quit_command_cancels_all_deferred_commands_on_subkernels(Language language)
        {
            var deferredCommandExecuted = false;

            var quitCommandExecuted = false;

            var kernel = CreateKernel(language);

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

            foreach (var subkernel in kernel.ChildKernels)
            {
                subkernel.DeferCommand(deferred);
            }

            await kernel.SendAsync(quitCommand);

            using var _ = new AssertionScope();

            deferredCommandExecuted.Should().BeFalse();
            quitCommandExecuted.Should().BeTrue();

            KernelEvents
                .Should().ContainSingle<CommandSucceeded>()
                .Which
                .Command
                .Should()
                .Be(quitCommand);
        }

        [Theory(Skip = "requires scheduler working")]
        [InlineData(Language.CSharp, "System.Threading.Thread.Sleep(3000);")]
        [InlineData(Language.FSharp, "System.Threading.Thread.Sleep(3000)")]
        [InlineData(Language.PowerShell, "Start-Sleep -Milliseconds 3000", Skip = "to address later")]
        public void Quit_command_is_handled(Language language, string code)
        {
            var kernel = CreateKernel(language);

            var quitCommandExecuted = false;

            Quit.OnQuit(() => { quitCommandExecuted = true; });

            var quitCommand = new Quit();

            var submitCodeCommand = new SubmitCode(code);

            Task.WhenAll(
                    Task.Run(async () =>
                    {
                        await Task.Delay(20);
                        await kernel.SendAsync(submitCodeCommand);
                    }),
                    Task.Run(async () =>
                    {
                        await Task.Delay(100);
                        await kernel.SendAsync(quitCommand);
                    }))
                .Wait(TimeSpan.FromSeconds(20));

            using var _ = new AssertionScope();

            quitCommandExecuted.Should().BeTrue();

            KernelEvents
                .Should()
                .ContainSingle<CommandSucceeded>()
                .Which
                .Command
                .Should()
                .Be(quitCommand);
        }


        [Theory(Skip = "requires scheduler working")]
        [InlineData(Language.CSharp, "System.Threading.Thread.Sleep(3000);")]
        [InlineData(Language.FSharp, "System.Threading.Thread.Sleep(3000)")]
        [InlineData(Language.PowerShell, "Start-Sleep -Milliseconds 3000", Skip = "to address later")]
        public void Quit_command_causes_the_running_command_to_fail(Language language, string code)
        {
            var kernel = CreateKernel(language);

            Quit.OnQuit(() => { });

            var quitCommand = new Quit();

            var submitCodeCommand = new SubmitCode(code);

            Task.WhenAll(
                    Task.Run(async () =>
                    {
                        await Task.Delay(20);
                        await kernel.SendAsync(submitCodeCommand);
                    }),
                    Task.Run(async () =>
                    {
                        await Task.Delay(100);
                        await kernel.SendAsync(quitCommand);
                    }))
                .Wait(TimeSpan.FromSeconds(20));

            using var _ = new AssertionScope();

            KernelEvents
                .Should()
                .ContainSingle<CommandFailed>(c => c.Command == submitCodeCommand)
                .Which
                .Exception
                .Should()
                .BeOfType<OperationCanceledException>();
        }
    }
}