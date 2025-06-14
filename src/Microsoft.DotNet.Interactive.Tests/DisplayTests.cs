// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests;

public class DisplayTests
{
    [Fact]
    public void When_no_invocation_context_is_present_then_Display_writes_to_the_console()
    {
        var output = new StringBuilder();

        using var subscription = ConsoleOutput.Subscribe(console => console.Out.Subscribe(s => output.Append(s)));
        var message = "Display me.";

        message.Display();

        output.ToString().Should().Be("Display me." + Environment.NewLine);
    }

    [Fact]
    public void When_no_invocation_context_is_present_then_DisplayAs_writes_to_the_console()
    {
        var output = new StringBuilder();

        using var _ = ConsoleOutput.Subscribe(console => console.Out.Subscribe(s => output.Append(s)));
        var message = "Display me.";

        message.DisplayAs("text/plain");

        output.ToString().Should().Be("Display me." + Environment.NewLine);
    }

    [Fact]
    public void When_no_invocation_context_is_present_then_DisplayedValue_Update_writes_to_the_console()
    {
        var output = new StringBuilder();

        using var subscription = ConsoleOutput.Subscribe(console => console.Out.Subscribe(s => output.Append(s)));
        var message = "Display me.";

        var displayedValue = message.Display();
        output.Clear();

        using var _ = new AssertionScope();

        displayedValue.Update("Now display me.");

        output.ToString().Should().Be("Now display me." + Environment.NewLine);
    }
}