// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Extensions;
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

        [Theory]
        [InlineData(Language.CSharp, Skip = "requires scheduler working")]
        [InlineData(Language.FSharp, Skip = "requires scheduler working")]
        [InlineData(Language.PowerShell, Skip = "to address later")]
        public void cancel_command_cancels_the_running_command(Language language)
        {
            var kernel = CreateKernel(language);

            var cancelCommand = new Cancel();

            var submitCodeCommand = new CancellableCommand();

            Task.WhenAll(
                    Task.Run(async () => { await kernel.SendAsync(submitCodeCommand); }),
                    Task.Run(async () =>
                    {
                        await Task.Delay(100);
                        await kernel.SendAsync(cancelCommand);
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

        [Fact]
        public async Task cancel_issues_CommandSucceeded()
        {
            var kernel = CreateKernel()
                .LogCommandsToPocketLogger()
                .LogEventsToPocketLogger();

            var cancelCommand = new Cancel();

            var commandToCancel = new SubmitCode(@"
using Microsoft.DotNet.Interactive;

while(!KernelInvocationContext.Current.CancellationToken.IsCancellationRequested){ await Task.Delay(10); }", targetKernelName: "csharp");

            var _ = kernel.SendAsync(commandToCancel);
            await kernel.SendAsync(cancelCommand);

            KernelEvents
                .Should()
                .ContainSingle<CommandSucceeded>(c => c.Command == cancelCommand);
        }

        [Fact]
        public async Task new_commands_issued_after_cancel_are_executed()
        {
            var kernel = CreateKernel()
                .LogCommandsToPocketLogger()
                .LogEventsToPocketLogger();

            var commandToCancel = new SubmitCode(@"
using Microsoft.DotNet.Interactive;

while(!KernelInvocationContext.Current.CancellationToken.IsCancellationRequested){ await Task.Delay(10); }", targetKernelName: "csharp");

            var _ = kernel.SendAsync(commandToCancel);
            await kernel.SendAsync(new Cancel());

            var followingCommand = new SubmitCode("1");
            await kernel.SendAsync(followingCommand);

            KernelEvents
                .Should()
                .ContainSingle<CommandSucceeded>(c => c.Command == followingCommand);
        }

        [Fact]
        public async Task cancel_succeeds_when_there_is_command_in_progress_to_cancel()
        {
            var kernel = CreateKernel();

            var cancelCommand = new Cancel();

            await kernel.SendAsync(cancelCommand);

            KernelEvents
                .Should()
                .ContainSingle<CommandSucceeded>(c => c.Command == cancelCommand);
        }

        [Fact]
        public async Task can_cancel_user_infinite_loops()
        {
            var kernel = CreateKernel();

            var cancelCommand = new Cancel();

            var commandToRun = new SubmitCode("while(true){ await Task.Delay(10); }", targetKernelName:"csharp");
           
            var commandToInterrupt = kernel.SendAsync(commandToRun);

            await kernel.SendAsync(cancelCommand);

            await commandToInterrupt;

            KernelEvents
                .Should()
                .ContainSingle<CommandFailed>(c => c.Command == commandToRun);
        }

        [Fact]
        public async Task can_cancel_user_loop_using_CancellationToken()
        {
            var kernel = CreateKernel();

            var cancelCommand = new Cancel();

            var commandToCancel = new SubmitCode(@"
using Microsoft.DotNet.Interactive;

while(!KernelInvocationContext.Current.CancellationToken.IsCancellationRequested){ await Task.Delay(10); }", targetKernelName: "csharp");
            
            var resultForCommandToCancel = kernel.SendAsync(commandToCancel);

            await kernel.SendAsync(cancelCommand);

            await resultForCommandToCancel;

            KernelEvents
                .Should()
                .ContainSingle<CommandFailed>(c => c.Command == commandToCancel);
        }

        [Fact]
        public void user_code_can_react_to_cancel_command_using_KernelInvocationContext_cancellation_token()
        {
            var kernel = CreateKernel();
            var commandInProgress = new CancellableCommand();
            var cancelSent = commandInProgress.Invoked.ContinueWith(async task =>
            {
                // once the cancellable command is running, send a Cancel 
                await kernel.SendAsync(new Cancel());
            });

            var _ = kernel.SendAsync(commandInProgress);

            Task.WhenAll(cancelSent, commandInProgress.Cancelled)
                .Wait(TimeSpan.FromSeconds(5));
        }

        public class CancellableCommand : KernelCommand
        {
            private readonly TaskCompletionSource _invoked = new();
            private readonly TaskCompletionSource _cancelled = new();

            public CancellableCommand(
                string targetKernelName = null,
                KernelCommand parent = null) : base(targetKernelName, parent)
            {
                // prevent NoSuitableKernelException by setting the handler
                Handler = (command, context) => Task.CompletedTask;
            }

            public override async Task InvokeAsync(KernelInvocationContext context)
            {
                _invoked.SetResult();

                context.CancellationToken.Register(() =>
                {
                    _cancelled.SetResult();
                });

                await _cancelled.Task;
            }

            public Task Invoked => _invoked.Task;

            public Task Cancelled => _cancelled.Task;
        }
    }
}