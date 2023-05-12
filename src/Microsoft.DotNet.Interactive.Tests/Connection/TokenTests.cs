// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests.Connection;

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
    public void Repeated_calls_to_GetOrCreateToken_for_the_same_command_return_the_same_value()
    {
        var command = new SubmitCode("123");

        var token1 = command.GetOrCreateToken();
        var token2 = command.GetOrCreateToken();

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