// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
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
        public async Task Directive_handlers_are_in_invoked_the_order_in_which_they_occur_in_the_code_submission()
        {
            using var kernel = new CSharpKernel();
            var events = kernel.KernelEvents.ToSubscribedList();

            kernel.AddDirective(new Command("#increment")
            {
                Handler = CommandHandler.Create(async (KernelInvocationContext context) =>
                {
                    await context.HandlingKernel.SubmitCodeAsync("i++;");
                } )
            });

            await kernel.SubmitCodeAsync(@"
var i = 0;
#increment
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

        [Fact(Skip = "issue #105")]
        public async Task Directives_can_display_help()
        {
            // using var consoleOut = await ConsoleOutput.Capture();

            using var kernel = new CompositeKernel();
            using var events = kernel.KernelEvents.ToSubscribedList();

            var command = new Command("#!hello")
            {
                new Option<bool>("--loudness")
            };
            command.Handler = CommandHandler.Create((IConsole console) =>
            {
            });

            kernel.AddDirective(command);

            await kernel.SubmitCodeAsync("#!hello -h");

            events.Should()
                  .ContainSingle<DisplayedValueProduced>()
                  .Which
                  .FormattedValues
                  .Should()
                  .ContainSingle(e => e.MimeType == "text/html")
                  .Which
                  .Value
                  .As<string>()
                  .Should()
                  .ContainAll("Usage", "#!hello [options]", "--loudness");
        }
    }
}