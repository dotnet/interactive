// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

using FluentAssertions;
using FluentAssertions.Extensions;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.DotNet.Interactive.Utility;

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests;
#pragma warning disable xUnit1000

public class CancelCommandTests : LanguageKernelTestBase
{
    public CancelCommandTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task cancel_issues_CommandSucceeded()
    {
        using var kernel = CreateKernel()
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
        // todo: this test is flaky and timeouts in CI
        while (true)
        {
            using var kernel = CreateKernel()
                .LogCommandsToPocketLogger()
                .LogEventsToPocketLogger();

            // make sure the deferred commands are flushed
            await kernel.SubmitCodeAsync(" ");

            var commandToCancel = new SubmitCode("""
                using Microsoft.DotNet.Interactive;
                await Task.Delay(10);
                while(!KernelInvocationContext.Current.CancellationToken.IsCancellationRequested)
                { 
                    await Task.Delay(10); 
                }
                """,
                targetKernelName: "csharp");
            var followingCommand = new SubmitCode("1");
            try
            {
                var _ = kernel.SendAsync(commandToCancel);
                await kernel.SendAsync(new Cancel()).Timeout(10.Seconds());

                var result = await kernel.SendAsync(followingCommand).Timeout(10.Seconds());

                result.Events
                      .Should()
                      .ContainSingle<CommandSucceeded>();
                break;
            }
            catch (TimeoutException)
            {
            }
        }
    }

    [Fact]
    public async Task cancel_succeeds_when_there_is_command_in_progress_to_cancel()
    {
        using var kernel = CreateKernel();

        var cancelCommand = new Cancel();

        var results = await kernel.SendAsync(cancelCommand);

        results.Events
            .Should()
            .ContainSingle<CommandSucceeded>(c => c.Command == cancelCommand);
    }
    
    [Fact]
    public async Task can_cancel_user_loop_using_CancellationToken()
    {
        // todo: this test is flaky and timeouts in CI
        while (true)
        {
            using var kernel = CreateKernel();
                
            var cancelCommand = new Cancel();

            var commandToCancel = new SubmitCode(@"
using Microsoft.DotNet.Interactive;
var cancellationToken = KernelInvocationContext.Current.CancellationToken;
while(!cancellationToken.IsCancellationRequested){ 
    await Task.Delay(10); 
}", targetKernelName: "csharp");
            try
            {
                var resultForCommandToCancel = kernel.SendAsync(commandToCancel);

                await Task.Delay(200);

                await kernel.SendAsync(cancelCommand).Timeout(10.Seconds());

                var result = await resultForCommandToCancel.Timeout(10.Seconds());

                result.Events
                      .Should()
                      .ContainSingle<CommandFailed>()
                      .Which
                      .Command
                      .Should()
                      .Be(commandToCancel);
                break;
            }
            catch (TimeoutException)
            {
            }
        }
    }

    [Fact]
    public async Task can_cancel_user_code_when_commands_are_split()
    {
        // todo: this test is flaky and timeouts in CI
        while (true)
        {
            using var kernel = CreateKernel();

            var cancelCommand = new Cancel();

            var commandToCancel = new SubmitCode(@"
#!csharp 
using Microsoft.DotNet.Interactive;
var cancellationToken = KernelInvocationContext.Current.CancellationToken;
while(!cancellationToken.IsCancellationRequested){ 
    await Task.Delay(10); 
}");
            try
            {
                var resultForCommandToCancel = kernel.SendAsync(commandToCancel);

                await Task.Delay(200);

                await kernel.SendAsync(cancelCommand).Timeout(10.Seconds());

                var result = await resultForCommandToCancel.Timeout(10.Seconds());

                result.Events
                    .Should()
                    .ContainSingle<CommandFailed>()
                    .Which
                    .Command
                    .Should()
                    .Be(commandToCancel);
                break;
            }
            catch (TimeoutException)
            {
            }
        }
    }

    [Fact]
    public void user_code_can_react_to_cancel_command_using_KernelInvocationContext_cancellation_token()
    {
        using var kernel = CreateKernel();
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

        public CancellableCommand(string targetKernelName = null) : base(targetKernelName)
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