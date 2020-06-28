// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Assent;
using FluentAssertions;
using FluentAssertions.Primitives;

namespace Microsoft.DotNet.Interactive.Formatting.Tests
{
    public static class AssertionExtensions
    {
        public static AndWhichConstraint<StringAssertions, string> BeEquivalentHtml(
            this StringAssertions assertions,
            string expected)
        {
            var subject = assertions.Subject;

            var compareResult = new DefaultStringComparer(true).Compare(
                subject.IndentHtml(), 
                expected.IndentHtml());

            compareResult.Error.Should().BeNullOrEmpty();

            return new AndWhichConstraint<StringAssertions, string>(
                subject.Should(), subject);
        }
    }
}