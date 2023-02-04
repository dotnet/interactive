// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Formatting.Tests.Utility;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.DotNet.Interactive.Formatting.Tests.Tags;

namespace Microsoft.DotNet.Interactive.Formatting.Tests;

public partial class HtmlFormatterTests : FormatterTestBase
{
    public class Json : FormatterTestBase
    {
        private readonly ITestOutputHelper _output;

        public Json(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void JsonDocument_and_JsonDocument_RootElement_output_the_same_HTML()
        {
            var jsonString = JsonSerializer.Serialize(new { Name = "cherry", Deliciousness = 9000 });

            var jsonDocument = JsonDocument.Parse(jsonString);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            var jsonDocumentHtml = jsonDocument.ToDisplayString(HtmlFormatter.MimeType);
            var jsonElementHtml = jsonElement.ToDisplayString(HtmlFormatter.MimeType);

            jsonDocumentHtml.Should().Be(jsonElementHtml);
        }

        [Fact]
        public void JSON_object_output_contains_a_text_summary()
        {
            var jsonString = JsonSerializer.Serialize(new { Name = "cherry", Deliciousness = 9000 });

            var jsonDocument = JsonDocument.Parse(jsonString);

            var html = jsonDocument.ToDisplayString(HtmlFormatter.MimeType);

            html.Should().ContainAll(
                "<code>", 
                jsonString.HtmlEncode().ToString(),
                "</code>");
        }

        [Fact]
        public void JSON_object_output_contains_table_of_properties_within_details_tag()
        {
            var jsonString = JsonSerializer.Serialize(new { Name = "cherry", Deliciousness = 9000 });

            var jsonDocument = JsonDocument.Parse(jsonString);

            var html = jsonDocument.ToDisplayString(HtmlFormatter.MimeType);

            html.Should().ContainAll(
                "<details",
                "<td>Name</td>",
                "<td><span>&quot;cherry&quot;</span></td>",
                "</details>");
        }

        [Fact]
        public void JSON_array_output_contains_a_text_summary()
        {
            var jsonString = JsonSerializer.Serialize(new object[] { "apple", "banana", "cherry" });

            var jsonDocument = JsonDocument.Parse(jsonString);

            var html = jsonDocument.ToDisplayString(HtmlFormatter.MimeType);

            html.Should().ContainAll(
                "<code>",
                jsonString.HtmlEncode().ToString(),
                "</code>");
        }
            
        [Fact]
        public void JSON_array_output_contains_table_of_items_within_details_tag()
        {
            var jsonString = JsonSerializer.Serialize(new object[] { "apple", "banana", "cherry" });

            var jsonDocument = JsonDocument.Parse(jsonString);

            var html = jsonDocument.ToDisplayString(HtmlFormatter.MimeType).RemoveStyleElement();

            html.Should().Contain(
                "<tr><td><span>&quot;apple&quot;</span></td></tr><tr><td><span>&quot;banana&quot;</span></td></tr><tr><td><span>&quot;cherry&quot;</span></td></tr>");
        }

        [Fact]
        public void Strings_with_escaped_sequences_are_encoded()
        {
            var value = "hola! \n \t \" \" ' ' the joy of escapes! ==> &   white  space  ";

            var text = value.ToDisplayString("text/html").RemoveStyleElement();

            text.Should().Be($"{PlainTextBegin}hola! \n \t &quot; &quot; &#39; &#39; the joy of escapes! ==&gt; &amp;   white  space  {PlainTextEnd}");
        }

        [Fact]
        public void Strings_with_unicode_sequences_are_encoded()
        {
            var value = "hola! ʰ˽˵ΘϱϪԘÓŴ𝓌🦁♿🌪🍒☝🏿";

            var text = value.ToDisplayString("text/html").RemoveStyleElement();

            text.Should().Be($"{PlainTextBegin}{value.HtmlEncode()}{PlainTextEnd}");
        }
    }
}