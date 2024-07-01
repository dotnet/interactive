// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public static class RunResultExtensions
{
    public static void ShouldFailWithOutput(this RunResult result, params string[] expected)
    {
        using (new AssertionScope("result"))
        {
            result.Succeeded.Should().BeFalse();
            result.Output.ShouldMatch(expected);
            result.Exception.Should().BeNull();
        }
    }

    public static void ShouldSucceedWithOutput(this RunResult result, params string[] expected)
    {
        using (new AssertionScope("result"))
        {
            result.Succeeded.Should().BeTrue();
            result.Output.ShouldMatch(expected);
            result.Exception.Should().BeNull();
        }
    }

    public static void ShouldSucceedWithOutputAsOneOf(this RunResult result, params string[] expected)
    {
        using (new AssertionScope("result"))
        {
            result.Succeeded.Should().BeTrue();
            result.Output.Should().Contain(s => expected.Any(e => e==s)  );
            result.Exception.Should().BeNull();
        }
    }

    public static void ShouldSucceedWithNoOutput(this RunResult result) =>
        result.ShouldSucceedWithOutput(Array.Empty<string>());

    public static void ShouldFailWithExceptionContaining(this RunResult result, string text, params string[] output)
    {
        using (new AssertionScope("result"))
        {
            result.Succeeded.Should().BeFalse();
            result.Output.Should().NotBeNull();
            result.Output.ShouldMatch(output);
            result.Exception.Should().Contain(text);
        }
    }

    public static void ShouldSucceedWithExceptionContaining(this RunResult result, string text, params string[] output)
    {
        using (new AssertionScope("result"))
        {
            result.Succeeded.Should().BeTrue();
            result.Output.ShouldMatch(output);
            result.Exception.Should().Contain(text);
        }
    }
}