// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using FluentAssertions;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions.Extensions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class KernelTests
    {
        [Fact]
        public void Deferred_initialization_command_is_not_executed_prior_to_first_submission()
        {
            var receivedCommands = new List<KernelCommand>();

            using var kernel = new FakeKernel
            {
                Handle = (command, context) =>
                {
                    receivedCommands.Add(command);
                    return Task.CompletedTask;
                }
            };

            kernel.DeferCommand(new SubmitCode("hello"));
            kernel.DeferCommand(new SubmitCode("world!"));

            receivedCommands.Should().BeEmpty();
        }

        [Fact]
        public async Task Deferred_initialization_command_is_executed_on_first_submission()
        {
            var receivedCommands = new List<KernelCommand>();

            using var kernel = new FakeKernel
            {
                Handle = (command, context) =>
                {
                    receivedCommands.Add(command);
                    return Task.CompletedTask;
                }
            };

            kernel.DeferCommand(new SubmitCode("one"));
            kernel.DeferCommand(new SubmitCode("two"));

            await kernel.SendAsync(new SubmitCode("three"));

            receivedCommands
                .Select(c => c is SubmitCode submitCode ? submitCode.Code : c.ToString())
                .Should()
                .BeEquivalentSequenceTo("one", "two", "three");
        }

        [Fact]
        public async Task Split_non_referencing_directives_are_executed_in_textual_order()
        {
            var receivedCommands = new List<string>();

            using var kernel = new FakeKernel
            {
                Handle = (command, context) =>
                {
                    receivedCommands.Add(((SubmitCode)command).Code);
                    return Task.CompletedTask;
                }
            };

            kernel.AddDirective(new Command("#!one")
            {
                Handler = CommandHandler.Create(() =>
                {
                    receivedCommands.Add("#!one");
                })
            });

            kernel.AddDirective(new Command("#!two")
            {
                Handler = CommandHandler.Create(() =>
                {
                    receivedCommands.Add("#!two");
                })
            });

            var code1 = "var a = 1";
            var code2 = "var b = 2";
            var code3 = "var c = 3";
            var directive1 = "#!one";
            var directive2 = "#!two";

            await kernel.SubmitCodeAsync($@"
{code1}
{directive1}
{code2}
{directive2}
{code3}
");

            receivedCommands
                .Select(c => c.Trim())
                .Should()
                .BeEquivalentSequenceTo(
                    code1,
                    directive1,
                    code2,
                    directive2,
                    code3);
        }

        [Fact]
        public async Task Middleware_is_only_executed_once_per_command()
        {
            var middeware1Count = 0;
            var middeware2Count = 0;
            var middeware3Count = 0;

            using var kernel = new FakeKernel();

            kernel.AddMiddleware(async (command, context, next) =>
            {
                middeware1Count++;
                await next(command, context);
            }, "one");
            kernel.AddMiddleware(async (command, context, next) =>
            {
                middeware2Count++;
                await next(command, context);
            }, "two");
            kernel.AddMiddleware(async (command, context, next) =>
            {
                middeware3Count++;
                await next(command, context);
            }, "three");

            await kernel.SendAsync(new SubmitCode("123"));

            middeware1Count.Should().Be(1);
            middeware2Count.Should().Be(1);
            middeware3Count.Should().Be(1);
        }

        [Fact]
        public async Task kernelEvents_sequence_completes_when_kernel_is_disposed ()
        {
            var kernel = new FakeKernel();
            var events = kernel.KernelEvents.Timeout(5.Seconds()).LastOrDefaultAsync();

            kernel.Dispose();

            var lastEvent = await events;
            lastEvent.Should().BeNull();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void When_command_type_registered_then_kernel_registers_envelope_type_for_serialization(bool withHandler)
        {
            Microsoft.DotNet.Interactive.Server.KernelCommandEnvelope.ResetToDefaults();

            using var kernel = new FakeKernel();

            if (withHandler)
            {
                kernel.RegisterCommandHandler<CustomCommandTypes.FirstSubmission.MyCommand>(
                    CustomCommandTypes.MyCommandType,
                    (_, _) => Task.CompletedTask);
            }
            else
            {
                kernel.RegisterCommandType<CustomCommandTypes.FirstSubmission.MyCommand>(
                    CustomCommandTypes.MyCommandType);
            }

            var originalCommand = new CustomCommandTypes.FirstSubmission.MyCommand("xyzzy");
            string envelopeJson = Microsoft.DotNet.Interactive.Server.KernelCommandEnvelope.Serialize(originalCommand);
            var roundTrippedCommandEnvelope = Microsoft.DotNet.Interactive.Server.KernelCommandEnvelope.Deserialize(envelopeJson);

            roundTrippedCommandEnvelope
                .Command
                .Should()
                .BeOfType<CustomCommandTypes.FirstSubmission.MyCommand>()
                .Which
                .Info
                .Should()
                .Be(originalCommand.Info);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void When_command_type_reregistered_with_changed_type_command_then_kernel_registers_updated_envelope_type_for_serialization(bool withHandler)
        {
            // Notebook authors should be able to develop their custom commands experimentally and progressively,
            // so we don't want any "you have to restart your kernel now" situations just because you already
            // called RegisterCommandHandler once for a particular command type.
            Microsoft.DotNet.Interactive.Server.KernelCommandEnvelope.ResetToDefaults();

            using var kernel = new FakeKernel();

            if (withHandler)
            {
                kernel.RegisterCommandHandler<CustomCommandTypes.FirstSubmission.MyCommand>(
                    CustomCommandTypes.MyCommandType,
                    (_, _) => Task.CompletedTask);
                kernel.RegisterCommandHandler<CustomCommandTypes.SecondSubmission.MyCommand>(
                    CustomCommandTypes.MyCommandType,
                    (_, _) => Task.CompletedTask);
            }
            else
            {
                kernel.RegisterCommandType<CustomCommandTypes.FirstSubmission.MyCommand>(
                    CustomCommandTypes.MyCommandType);
                kernel.RegisterCommandType<CustomCommandTypes.SecondSubmission.MyCommand>(
                    CustomCommandTypes.MyCommandType);
            }

            var originalCommand = new CustomCommandTypes.SecondSubmission.MyCommand("xyzzy", 42);
            string envelopeJson = Microsoft.DotNet.Interactive.Server.KernelCommandEnvelope.Serialize(originalCommand);
            var roundTrippedCommandEnvelope = Microsoft.DotNet.Interactive.Server.KernelCommandEnvelope.Deserialize(envelopeJson);

            roundTrippedCommandEnvelope
                .Command
                .Should()
                .BeOfType<CustomCommandTypes.SecondSubmission.MyCommand>()
                .Which
                .Info
                .Should()
                .Be(originalCommand.Info);
            roundTrippedCommandEnvelope
                .Command
                .As<CustomCommandTypes.SecondSubmission.MyCommand>()
                .AdditionalProperty
                .Should()
                .Be(originalCommand.AdditionalProperty);
        }
    }
}