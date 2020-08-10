﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Jupyter;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class DirectiveTests
    {
        [Fact]
        public void Directives_may_be_prefixed_with_hash()
        {
            using var kernel = new CompositeKernel();

            kernel
                .Invoking(k => k.AddDirective(new Command("#hello")))
                .Should()
                .NotThrow();
        }

        [Theory]
        [InlineData("{")]
        [InlineData(";")]
        [InlineData("a")]
        [InlineData("1")]
        public void Directives_may_not_begin_with_(string value)
        {
            using var kernel = new CompositeKernel();

            kernel
                .Invoking(k => k.AddDirective(new Command($"{value}hello")))
                .Should()
                .Throw<ArgumentException>()
                .Which
                .Message
                .Should()
                .Be($"Invalid directive name \"{value}hello\". Directives must begin with \"#\".");
        }

        [Theory]
        [InlineData("{")]
        [InlineData(";")]
        [InlineData("a")]
        [InlineData("1")]
        public void Directives_may_not_have_aliases_that_begin_with_(string value)
        {
            using var kernel = new CompositeKernel();

            var command = new Command("#!this-is-fine");
            command.AddAlias($"{value}hello");

            kernel
                .Invoking(k =>
                {
                    kernel.AddDirective(command);
                })
                .Should()
                .Throw<ArgumentException>()
                .Which
                .Message
                .Should()
                .Be($"Invalid directive name \"{value}hello\". Directives must begin with \"#\".");
        }

        [Fact]
        public async Task Directive_handlers_are_invoked_in_the_order_in_which_they_occur_in_the_code_submission()
        {
            using var kernel = new CSharpKernel();
            var events = kernel.KernelEvents.ToSubscribedList();

            kernel.AddDirective(new Command("#!increment")
            {
                Handler = CommandHandler.Create(async (KernelInvocationContext context) =>
                {
                    await context.HandlingKernel.SubmitCodeAsync("i++;");
                })
            });

            await kernel.SubmitCodeAsync(@"
var i = 0;
#!increment
i");

            events
                .Should()
                .ContainSingle<ReturnValueProduced>()
                .Which
                .Value
                .Should()
                .Be(1);
        }

        [Fact]
        public async Task Directive_parse_errors_are_displayed()
        {
            var command = new Command("#!oops")
            {
                new Argument<string>()
            };

            using var kernel = new CSharpKernel();

            kernel.AddDirective(command);

            var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync("#!oops");

            events.Should()
                  .ContainSingle<CommandFailed>()
                  .Which
                  .Message
                  .Should()
                  .Be("Required argument missing for command: #!oops");
        }

        [Fact]
        public async Task Directive_parse_errors_prevent_code_submission_from_being_run()
        {
            var command = new Command("#!x")
            {
                new Argument<string>()
            };

            using var kernel = new CSharpKernel();

            kernel.AddDirective(command);

            var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync("#!x\n123");

            events.Should().NotContain(e => e is ReturnValueProduced);
        }

        [Fact]
        public void Directives_with_duplicate_aliases_are_not_allowed()
        {
            using var kernel = new CompositeKernel();

            kernel.AddDirective(new Command("#dupe"));

            kernel.Invoking(k => 
                                k.AddDirective(new Command("#dupe")))
                  .Should()
                  .Throw<ArgumentException>()
                  .Which
                  .Message
                  .Should()
                  .Be("Alias \'#dupe\' is already in use.");
        }

        [Fact]
        public async Task OnComplete_can_be_used_to_act_on_completion_of_commands()
        {
            using var kernel = new FakeKernel();

            using var events = kernel.KernelEvents.ToSubscribedList();

            kernel.AddDirective(new Command("#!wrap")
            {
                Handler = CommandHandler.Create(async (KernelInvocationContext c) =>
                {
                    await c.DisplayAsync("hello!");

                    c.OnComplete(async context =>
                    {
                        await context.DisplayAsync("goodbye!");
                    });
                })
            });

            await kernel.SubmitCodeAsync("#!wrap");

            events
                .OfType<DisplayedValueProduced>()
                .Select(e => e.Value)
                .Should()
                .BeEquivalentSequenceTo("hello!", "goodbye!");
        }

        [Fact]
        public async Task When_an_unrecognized_directive_is_encountered_it_is_forwarded_to_the_kernel_as_submitCode()
        {
            SubmitCode submitCommand = null;
            using var kernel = new CompositeKernel
            {
                new FakeKernel("csharp")
                {
                    Handle = (command, context) =>
                    {
                        submitCommand = command as SubmitCode;
                        return Task.CompletedTask;
                    }
                }
            };

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync("#!undefinedDirective");
            using var _ = new AssertionScope();
            
            submitCommand.Should().NotBeNull();
            submitCommand.Code.Should().Be("#!undefinedDirective");
        }

        [Theory]
        [InlineData("csharp")]
        [InlineData(".NET")]
        public async Task Directives_can_display_help(string kernelName)
        {
            var cSharpKernel = new CSharpKernel().UseDefaultMagicCommands();
            using var compositeKernel = new CompositeKernel
            {
                cSharpKernel
            };

            var command = new Command("#!hello")
            {
                new Option<bool>("--loudness")
            };

            var kernel = compositeKernel.FindKernel(kernelName);

            kernel.AddDirective(command);

            using var events = compositeKernel.KernelEvents.ToSubscribedList();

            await compositeKernel.SubmitCodeAsync("#!hello -h");

            events.Should()
                  .ContainSingle<StandardOutputValueProduced>()
                  .Which
                  .FormattedValues
                  .Should()
                  .ContainSingle(v => v.MimeType == PlainTextFormatter.MimeType)
                  .Which
                  .Value
                  .Should()
                  .ContainAll("Usage", "#!hello", "[options]", "--loudness");

            events.Should()
                  .NotContainErrors();
        }

        [Fact]
        public async Task New_directives_can_be_added_after_older_ones_have_been_evaluated()
        {
            using var kernel = new CompositeKernel { new CSharpKernel() };

            using var events = kernel.KernelEvents.ToSubscribedList();

            var oneWasCalled = false;
            var twoWasCalled = false;

            kernel.AddDirective(new Command("#!one")
            {
                Handler = CommandHandler.Create(() => oneWasCalled = true)
            });

            await kernel.SubmitCodeAsync("#!one\n123");

            events.Should().NotContainErrors();

            kernel.AddDirective(new Command("#!two")
            {
                Handler = CommandHandler.Create(() => twoWasCalled = true)
            });

            await kernel.SubmitCodeAsync("#!two\n123");

            events.Should().NotContainErrors();
            oneWasCalled.Should().BeTrue();
            twoWasCalled.Should().BeTrue();
        }
    }
}