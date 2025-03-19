// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Formatting.Tests.Utility;
using static Microsoft.DotNet.Interactive.Formatting.Tests.Tags;

namespace Microsoft.DotNet.Interactive.Formatting.Tests;

public partial class HtmlFormatterTests : FormatterTestBase
{
    [TestClass]
    public class Json : FormatterTestBase
    {
        private readonly TestContext _output;

        public Json(TestContext output)
        {
            _output = output;
        }

        [TestMethod]
        public void JsonDocument_and_JsonDocument_RootElement_output_the_same_HTML()
        {
            var jsonString = JsonSerializer.Serialize(new { Name = "cherry", Deliciousness = 9000 });

            var jsonDocument = JsonDocument.Parse(jsonString);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            var jsonDocumentHtml = jsonDocument.ToDisplayString(HtmlFormatter.MimeType);
            var jsonElementHtml = jsonElement.ToDisplayString(HtmlFormatter.MimeType);

            jsonDocumentHtml.Should().Be(jsonElementHtml);
        }

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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
            
        [TestMethod]
        public void JSON_array_output_contains_table_of_items_within_details_tag()
        {
            var jsonString = JsonSerializer.Serialize(new object[] { "apple", "banana", "cherry" });

            var jsonDocument = JsonDocument.Parse(jsonString);

            var html = jsonDocument.ToDisplayString(HtmlFormatter.MimeType).RemoveStyleElement();

            html.Should().Contain(
                "<tr><td><span>&quot;apple&quot;</span></td></tr><tr><td><span>&quot;banana&quot;</span></td></tr><tr><td><span>&quot;cherry&quot;</span></td></tr>");
        }

        [TestMethod]
        public void Strings_with_escaped_sequences_are_encoded()
        {
            var value = "hola! \n \t \" \" ' ' the joy of escapes! ==> &   white  space  ";

            var text = value.ToDisplayString("text/html").RemoveStyleElement();

            text.Should().Be($"{PlainTextBegin}hola! \n \t &quot; &quot; &#39; &#39; the joy of escapes! ==&gt; &amp;   white  space  {PlainTextEnd}");
        }

        [TestMethod]
        public void Strings_with_unicode_sequences_are_encoded()
        {
            var value = "hola! ʰ˽˵ΘϱϪԘÓŴ𝓌🦁♿🌪🍒☝🏿";

            var text = value.ToDisplayString("text/html").RemoveStyleElement();

            text.Should().Be($"{PlainTextBegin}{value.HtmlEncode()}{PlainTextEnd}");
        }
    }
}