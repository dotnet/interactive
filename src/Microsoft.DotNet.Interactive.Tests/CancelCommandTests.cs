// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
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
        public async Task commands_issued_after_cancel_command_are_executed()
        {
            var kernel = CreateKernel();

            var cancelCommand = new Cancel();

            var commandToCancel = new CancellableCommand();

            var commandToRun = new SubmitCode("1");

            var _ = kernel.SendAsync(commandToCancel);
            await kernel.SendAsync(cancelCommand);
            await kernel.SendAsync(commandToRun);

            KernelEvents
                .Should()
                .ContainSingle<CommandSucceeded>(c => c.Command == commandToCancel);
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

                await Task.Delay(TimeSpan.FromDays(1), context.CancellationToken);
            }

            public Task Invoked => _invoked.Task;

            public Task Cancelled => _cancelled.Task;
        }
    }
}