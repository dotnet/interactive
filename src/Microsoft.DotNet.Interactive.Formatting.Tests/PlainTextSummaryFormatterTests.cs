// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Dummy;
using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.Interactive.Formatting.Tests;

public class PlainTextSummaryFormatterTests : FormatterTestBase
{
    [Fact]
    public void Null_reference_types_are_indicated()
    {
        string value = null;

        value.ToDisplayString(PlainTextSummaryFormatter.MimeType).Should().Be("<null>");
    }

    [Fact]
    public void Null_nullables_are_indicated()
    {
        int? nullable = null;

        var output = nullable.ToDisplayString(PlainTextSummaryFormatter.MimeType);

        output.Should().Be(((object)null).ToDisplayString());
    }

    [Fact]
    public void It_falls_back_to_ToString()
    {
        var instance = new ClassWithManyPropertiesAndCustomToString();

        var formatted = instance.ToDisplayString(PlainTextSummaryFormatter.MimeType);

        formatted.Should().Be($"{typeof(ClassWithManyPropertiesAndCustomToString)} custom ToString value");
    }

    [Fact]
    public void It_truncates_longer_outputs()
    {
        var instance = new string('a', 1000);

        var formatted = instance.ToDisplayString(PlainTextSummaryFormatter.MimeType);

        formatted.Length.Should().BeLessThan(500);

        formatted.Should().EndWith("...");
    }

    [Fact]
    public void It_expands_sequences_of_scalars()
    {
        var instance = new[] { 1, 2, 3 };

        var formatted = instance.ToDisplayString(PlainTextSummaryFormatter.MimeType);

        formatted.Should().Be("[ 1, 2, 3 ]");
    }

    [Fact]
    public void It_does_not_expand_properties_of_non_scalar_values_within_sequences()
    {
        var instance = new FileInfo[]
        {
            new("1.txt"),
            new("2.txt"),
            new("3.txt")
        };

        var formatted = instance.ToDisplayString(PlainTextSummaryFormatter.MimeType);

        formatted.Should().Be("[ 1.txt, 2.txt, 3.txt ]");
    }

    [Fact]
    public void It_replaces_newlines()
    {
        var stringWithNewlines = "one\ntwo\r\nthree";

        var formatted = stringWithNewlines.ToDisplayString(PlainTextSummaryFormatter.MimeType);

        formatted.Should().Be("one\\ntwo\\r\\nthree");
    }

    [Fact]
    public void If_ToString_throws_then_the_exception_is_displayed()
    {
        var instance = new ClassWithToStringThatThrows();

        var formatted = instance.ToDisplayString(PlainTextSummaryFormatter.MimeType);

        formatted.Should().StartWith("System.Exception: oops!");
    }
}