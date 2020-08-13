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
        public void Embedded_objects_are_formatted_using_custom_html_formatter_and_encoded()
        {
            var date = DateTime.Parse("1/1/2019 12:30pm");

            Formatter.Register<DateTime>(_ => "&lt;hello&gt;", mimeType: HtmlFormatter.MimeType);

            string output = div(date).ToString();

            output.Should().Be("<div>&lt;hello&gt;</div>");
        }
        [Fact]
        public void Embedded_objects_are_not_formatted_using_custom_plaintext_formatter_when_formating_as_html()
        {
            var date = DateTime.Parse("1/1/2019 12:30pm");

            // This formatter is not used because there is a default HTML formatter available.
            Formatter.Register<DateTime>(_ => "no no no", mimeType: PlainTextFormatter.MimeType);

            string output = div(date).ToString();

            output.Should().Be($"<div><span>{date.ToString("u")}</span></div>");
        }
    }
}