// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Xunit;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.Formatting.Tests
{
    public class PocketViewWithFormatterTests : FormatterTestBase
    {
        [Theory]
        [InlineData("text/html")]
        [InlineData("text/plain")]
        public void Formatter_does_not_expand_properties_of_PocketView(string mimeType)
        {
            PocketView view = b(123);

            view.ToDisplayString(mimeType).Should().Be("<b>123</b>");
        }

        [Fact]
        public void Embedded_objects_are_formatted_using_custom_formatter_and_encoded()
        {
            var date = DateTime.Parse("1/1/2019 12:30pm");

            Formatter<DateTime>.Register(_ => "<hello>");

            string output = div(date).ToString();

            output.Should().Be("<div>&lt;hello&gt;</div>");
        }
    }
}