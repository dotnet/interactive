// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Assent;
using FluentAssertions;
using FluentAssertions.Primitives;
using System.Linq;
using System;

namespace Microsoft.DotNet.Interactive.Formatting.Tests.Utility;

public static class AssertionExtensions
{
    public static AndWhichConstraint<StringAssertions, string> BeEquivalentHtmlTo(
        this StringAssertions assertions,
        string expected)
    {
        var subject = assertions.Subject;

        var actual = subject.IndentHtml();

        expected = expected.IndentHtml();

        var diff = new DefaultStringComparer(true).Compare(
            actual,
            expected).Error;

        (diff ?? "")
            .Replace("Received:", "\nActual:\n")
            .Replace("Approved:", "\nExpected:\n")
            .Should()
            .BeNullOrEmpty(because: "HTML doesn't match. Unexpected output was: \n\n" + actual);

        return new AndWhichConstraint<StringAssertions, string>(
            subject.Should(),
            subject);
    }

    public static AndWhichConstraint<StringAssertions, string> ContainEquivalentHtmlFragments(
        this StringAssertions assertions,
        params string[] expectedItems)
    {
        var subject = assertions.Subject;

        var actual = subject.IndentHtml();

        for (var i = 0; i < expectedItems.Length; i++)
        {
            var expected = expectedItems[i];

            var expectedIndented = expected.IndentHtml();

            var actualTrimmed = actual.TrimStartOfEachLine();

            var expectedTrimmed = expectedIndented.TrimStartOfEachLine();

            actualTrimmed.Should().Contain(expectedTrimmed);
        }

        return new AndWhichConstraint<StringAssertions, string>(
            subject.Should(),
            subject);
    }

    private static string TrimStartOfEachLine(this string input)
    {
        var lines = input.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join("\n", lines.Select(l => l.TrimStart()));
    }

    public static AndWhichConstraint<StringAssertions, string> BeExceptingWhitespace(
        this StringAssertions assertions,
        string expected)
    {
        Normalize(assertions.Subject)
            .Should()
            .Be(Normalize(expected));

        return new AndWhichConstraint<StringAssertions, string>(
            assertions.Subject.Should(),
            assertions.Subject);

        static string Normalize(string value) =>
            value
                .Trim()
                .Crunch()
                .Replace("\r\n", "\n");
    }
}