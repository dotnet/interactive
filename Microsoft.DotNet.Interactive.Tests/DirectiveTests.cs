// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class DirectiveTests
    {
        [Fact(Timeout = 45000)]
        public void Directives_may_be_prefixed_with_hash()
        {
            using var kernel = new CompositeKernel();

            kernel
                .Invoking(k => k.AddDirective(new Command("#hello")))
                .Should()
                .NotThrow();
        }

        [Theory(Timeout = 45000)]
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

        [Theory(Timeout = 45000)]
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

        [Fact(Timeout = 45000)]
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
    }
}