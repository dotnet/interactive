﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests.Connection
{
    public class TokenTests
    {
        [Fact]
        public void A_token_is_generated_on_demand()
        {
            var command = new SubmitCode("123");

            command.GetOrCreateToken()
                   .Should()
                   .NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void Repeated_calls_to_GetToken_for_the_same_command_return_the_same_value()
        {
            var command = new SubmitCode("123");

            var token1 = command.GetOrCreateToken();
            var token2 = command.GetOrCreateToken();

            token2.Should().Be(token1);
        }

        [Fact]
        public void When_created_in_the_same_context_then_child_commands_having_the_same_parent_also_have_the_same_token()
        {
            var parentCommand = new SubmitCode("123");

            string token1 = null;
            string token2 = null;

            using (KernelInvocationContext.Establish(parentCommand))
            {
                token1 = new SubmitCode("456").GetOrCreateToken();
                token2 = new SubmitCode("456").GetOrCreateToken();
            }

            token1.Should().Be(token2);
        }

        [Fact]
        public void When_resent_then_child_commands_having_the_same_parent_have_repeatable_tokens()
        {
            var parentCommand = new SubmitCode("123");
            parentCommand.SetToken("the-token");

            string token1 = null;
            string token2 = null;

            using (KernelInvocationContext.Establish(parentCommand))
            {
                token1 = new SubmitCode("456").GetOrCreateToken();
            }

            using (KernelInvocationContext.Establish(parentCommand))
            {
                token2 = new SubmitCode("456").GetOrCreateToken();
            }

            token1.Should().Be(token2);
        }

        [Fact]
        public void Command_tokens_are_reproducible_given_the_same_seed()
        {
            var command1 = new SubmitCode("123");
            command1.SetToken("the-token");
            string token1 = command1.GetOrCreateToken();

            var command2 = new SubmitCode("123");
            command2.SetToken("the-token");
            string token2 = command2.GetOrCreateToken();

            token2.Should().Be(token1);
        }

        [Fact]
        public void Command_tokens_cannot_be_changed()
        {
            var command = new SubmitCode("123");

            command.SetToken("once");

            command.Invoking(c => c.SetToken("again"))
                   .Should()
                   .Throw<InvalidOperationException>()
                   .Which
                   .Message
                   .Should()
                   .Be("Command token cannot be changed.");
        }
    }
}