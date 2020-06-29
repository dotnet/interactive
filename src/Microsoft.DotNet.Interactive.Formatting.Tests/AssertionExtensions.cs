// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Assent;
using FluentAssertions;
using FluentAssertions.Primitives;

namespace Microsoft.DotNet.Interactive.Formatting.Tests
{
    public static class AssertionExtensions
    {
        public static AndWhichConstraint<StringAssertions, string> BeEquivalentHtmlTo(
            this StringAssertions assertions,
            string expected)
        {
            var subject = assertions.Subject;

            var actual = subject.IndentHtml();

            var diff = new DefaultStringComparer(true).Compare(
                actual,
                expected.IndentHtml()).Error;

            (diff ?? "")
                .Replace("Received:", "\nActual:\n")
                .Replace("Approved:", "\nExpected:\n")
                .Should()
                .BeNullOrEmpty(because: "HTML doesn't match. Unexpected output was: \n\n" + actual);

            return new AndWhichConstraint<StringAssertions, string>(
                subject.Should(),
                subject);
        }
    }
}