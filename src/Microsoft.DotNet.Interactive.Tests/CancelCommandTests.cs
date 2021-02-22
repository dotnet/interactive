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

        [Theory]
        [InlineData(Language.CSharp, Skip = "requires scheduler working")]
        [InlineData(Language.FSharp, Skip = "requires scheduler working")]
        [InlineData(Language.PowerShell, Skip = "requires scheduler working")]
        public async Task cancel_command_cancels_all_deferred_commands_on_subkernels(Language language)
        {

            var kernel = CreateKernel(language);

            var deferred = new CancellableCommand();

            var cancelCommand = new Cancel();

            foreach (var subkernel in kernel.ChildKernels)
            {
                subkernel.DeferCommand(deferred);
            }

            await kernel.SendAsync(cancelCommand);

            using var _ = new AssertionScope();

            deferred.HasRun.Should().BeFalse();

            KernelEvents
                .Should().ContainSingle<CommandSucceeded>()
                .Which
                .Command
                .Should()
                .Be(cancelCommand);
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
                    Task.Run(async () =>
                    {
                        await kernel.SendAsync(submitCodeCommand);
                    }),
                    Task.Run(async () =>
                    {
                        await Task.Delay(100);
                        await kernel.SendAsync(cancelCommand);
                    }))
                .Wait(TimeSpan.FromSeconds(20));


            using var _ = new AssertionScope();

            submitCodeCommand.HasRun.Should().BeTrue();
            submitCodeCommand.HasBeenCancelled.Should().BeTrue();

            KernelEvents
                .Should()
                .ContainSingle<CommandFailed>(c => c.Command == submitCodeCommand)
                .Which
                .Exception
                .Should()
                .BeOfType<OperationCanceledException>();
        }

        [Theory]
        [InlineData(Language.CSharp, Skip = "requires scheduler working")]
        [InlineData(Language.FSharp, Skip = "requires scheduler working")]
        [InlineData(Language.PowerShell, Skip = "to address later")]
        public async Task commands_issued_after_cancel_command_are_executed(Language language)
        {
   
            var kernel = CreateKernel(language);

            var cancelCommand = new Cancel();

            var commandToCancel = new CancellableCommand( );

            var commandToRun = new SubmitCode("1");

            var _ = kernel.SendAsync(commandToCancel);
            await Task.Delay(4000);
            await kernel.SendAsync(cancelCommand);
            await kernel.SendAsync(commandToRun);

           // using var _ = new AssertionScope();
            
            commandToCancel.HasRun.Should().BeTrue();
            commandToCancel.HasBeenCancelled.Should().BeTrue();

            KernelEvents
                .Should()
                .ContainSingle<CommandFailed>(c => c.Command == commandToCancel)
                .Which
                .Exception
                .Should()
                .BeOfType<OperationCanceledException>();
        }


        [Theory]
        [InlineData(Language.CSharp, @"while(true){
    if(Microsoft.DotNet.Interactive.KernelInvocationContext.Current.CancellationToken.IsCancellationRequested)
    {
        Console.WriteLine(""done c#"");
        break;
    }
} 
", "done c#", Skip = "requires scheduler working")]
        [InlineData(Language.FSharp, @"
System.Threading.Thread.Sleep(3000)
Console.WriteLine(""done c#"")", "done f#", Skip = "for the moment")]
        public async Task user_code_can_react_to_cancel_command_using_cancellation_token(Language language, string code, string expectedValue)
        {
            var kernel = CreateKernel(language);
            var cancelCommand = new Cancel();
            var submitCodeCommand = new SubmitCode(code);
       
            var _ = kernel.SendAsync(submitCodeCommand);
            await Task.Delay(4000);
            await kernel.SendAsync(cancelCommand);
            
            KernelEvents
                .Should()
                .ContainSingle<StandardOutputValueProduced>()
                .Which
                .Value
                .Should()
                .Be(expectedValue);

        }
    }


}