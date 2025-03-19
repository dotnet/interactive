// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Dummy;
using FluentAssertions;

namespace Microsoft.DotNet.Interactive.Formatting.Tests;

[TestClass]
public class PlainTextSummaryFormatterTests : FormatterTestBase
{
    [TestMethod]
    public void Default_formatter_Type_displays_keyword_when_available_for_arrays()
    {
        typeof(string[]).ToDisplayString(PlainTextSummaryFormatter.MimeType)
                        .Should().Be("System.String[]");
    }

    [TestMethod]
    public void Default_formatter_for_Type_displays_generic_parameter_name_for_single_parameter_generic_type()
    {
        typeof(List<string>).ToDisplayString(PlainTextSummaryFormatter.MimeType)
                            .Should().Be("System.Collections.Generic.List<System.String>");
        new List<string>().GetType().ToDisplayString(PlainTextSummaryFormatter.MimeType)
                          .Should().Be("System.Collections.Generic.List<System.String>");
    }

    [TestMethod]
    public void Default_formatter_for_Type_displays_generic_parameter_name_for_multiple_parameter_generic_type()
    {
        typeof(Dictionary<string, IEnumerable<int>>).ToDisplayString(PlainTextSummaryFormatter.MimeType)
                                                    .Should().Be(
                                                        "System.Collections.Generic.Dictionary<System.String,System.Collections.Generic.IEnumerable<System.Int32>>");
    }

    [TestMethod]
    public void Default_formatter_for_Type_displays_generic_parameter_names_for_open_generic_types()
    {
        typeof(IList<>).ToDisplayString(PlainTextSummaryFormatter.MimeType)
                       .Should().Be("System.Collections.Generic.IList<T>");
        typeof(IDictionary<,>).ToDisplayString()
                              .Should().Be("System.Collections.Generic.IDictionary<TKey,TValue>");
    }

    [TestMethod]
    public void Null_reference_types_are_indicated()
    {
        string value = null;

        value.ToDisplayString(PlainTextSummaryFormatter.MimeType).Should().Be(Formatter.NullString);
    }

    [TestMethod]
    public void Null_nullables_are_indicated()
    {
        int? nullable = null;

        var output = nullable.ToDisplayString(PlainTextSummaryFormatter.MimeType);

        output.Should().Be(Formatter.NullString);
    }

    [TestMethod]
    public void It_falls_back_to_ToString()
    {
        var instance = new ClassWithManyPropertiesAndCustomToString();

        var formatted = instance.ToDisplayString(PlainTextSummaryFormatter.MimeType);

        formatted.Should().Be($"{typeof(ClassWithManyPropertiesAndCustomToString)} custom ToString value");
    }

    [TestMethod]
    public void It_truncates_longer_outputs()
    {
        var instance = new string('a', 1000);

        var formatted = instance.ToDisplayString(PlainTextSummaryFormatter.MimeType);

        formatted.Length.Should().BeLessThan(500);

        formatted.Should().EndWith("...");
    }

    [TestMethod]
    public void It_expands_sequences_of_scalars()
    {
        var instance = new[] { 1, 2, 3 };

        var formatted = instance.ToDisplayString(PlainTextSummaryFormatter.MimeType);

        formatted.Should().Be("[ 1, 2, 3 ]");
    }

    [TestMethod]
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

    [TestMethod]
    public void It_replaces_newlines()
    {
        var stringWithNewlines = "one\ntwo\r\nthree";

        var formatted = stringWithNewlines.ToDisplayString(PlainTextSummaryFormatter.MimeType);

        formatted.Should().Be("one\\ntwo\\r\\nthree");
    }

    [TestMethod]
    public void If_ToString_throws_then_the_exception_is_displayed()
    {
        var instance = new ClassWithToStringThatThrows();

        var formatted = instance.ToDisplayString(PlainTextSummaryFormatter.MimeType);

        formatted.Should().StartWith("System.Exception: oops!");
    }
}