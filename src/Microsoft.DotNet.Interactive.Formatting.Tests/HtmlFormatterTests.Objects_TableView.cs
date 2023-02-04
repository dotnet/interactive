// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Dynamic;
using System.IO;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.DotNet.Interactive.Formatting.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Formatting.Tests;

public partial class HtmlFormatterTests
{
    public class Objects_TableView : FormatterTestBase
    {
        // FIX: (Objects_TableView) delete?
        [Fact]
        public void Formatters_are_generated_on_the_fly_when_HTML_mime_type_is_requested()
        {
            var output = new { a = 123 }.ToDisplayString(HtmlFormatter.MimeType);

            output
                .RemoveStyleElement()
                .Should()
                .Be($"<table><thead><tr><th>a</th></tr></thead><tbody><tr><td>{Tags.PlainTextBegin}123{Tags.PlainTextEnd}</td></tr></tbody></table>");
        }

        [Fact]
        public void Null_references_are_indicated()
        {
            string value = null;

            value.ToDisplayString(HtmlFormatter.MimeType).RemoveStyleElement()
                 .Should()
                 .Be($"{Tags.PlainTextBegin}&lt;null&gt;{Tags.PlainTextEnd}");
        }

        [Fact]
        public void Formatter_puts_div_with_class_around_string()
        {
            var formatter = HtmlFormatter.GetPreferredFormatterFor<string>();

            var s = "hello".ToDisplayString(formatter).RemoveStyleElement();

            s.Should().Be($"{Tags.PlainTextBegin}hello{Tags.PlainTextEnd}");
        }
        
        [Fact]
        public void Formatter_expands_properties_of_ExpandoObjects()
        {
            dynamic expando = new ExpandoObject();
            expando.Name = "socks";
            expando.Count = 2;

            var formatter = HtmlFormatter.GetPreferredFormatterFor<ExpandoObject>();

            var output = ((object) expando).ToDisplayString(formatter).RemoveStyleElement();

            output.Should().BeEquivalentHtmlTo($@"
<table>
    <thead>
       <tr><th>Count</th><th>Name</th></tr>
    </thead>
    <tbody><tr><td>{Tags.PlainTextBegin}2{Tags.PlainTextEnd}</td><td>socks</td></tr>
    </tbody>
</table>");
        }

        [Fact]
        public void It_formats_objects_as_tables_having_properties_on_the_y_axis()
        {
            var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(EntityId));

            var writer = new StringWriter();

            var instance = new EntityId("TheEntity", "123");

            formatter.Format(instance, writer);

            var html = writer.ToString().RemoveStyleElement();

            html
                .Should()
                .BeEquivalentHtmlTo($@"
<table>
   <thead><tr><th>TypeName</th><th>Id</th></tr></thead>
   <tbody>
     <tr><td>{Tags.PlainTextBegin}TheEntity{Tags.PlainTextEnd}</td><td>{Tags.PlainTextBegin}123{Tags.PlainTextEnd}</td></tr>
  </tbody>
</table>");
        }

        [Fact]
        public void Formatted_objects_include_custom_styles()
        {
            var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(FileInfo));

            var writer = new StringWriter();

            var instance = new FileInfo("a.txt");

            formatter.Format(instance, writer);

            var html = writer.ToString();

            html.Should().Contain(Tags.DefaultStyles);
        }

        [Fact]
        public void It_formats_anonymous_types_as_tables_having_properties_on_the_y_axis()
        {
            var writer = new StringWriter();

            var instance = new
            {
                PropertyA = 123,
                PropertyB = "hello"
            };

            var formatter = HtmlFormatter.GetPreferredFormatterFor(instance.GetType());

            formatter.Format(instance, writer);

            writer.ToString().RemoveStyleElement()
                  .Should()
                  .BeEquivalentHtmlTo($@"
<table>
 <thead><tr><th>PropertyA</th><th>PropertyB</th></tr></thead>
 <tbody><tr><td>{Tags.PlainTextBegin}123{Tags.PlainTextEnd}</td><td>{Tags.PlainTextBegin}hello{Tags.PlainTextEnd}</td></tr></tbody>
</table>");
        }

        [Fact]
        public void It_formats_tuples_as_tables_having_properties_on_the_y_axis()
        {
            var writer = new StringWriter();

            var instance = (123, "hello");

            var formatter = HtmlFormatter.GetPreferredFormatterFor(instance.GetType());

            formatter.Format(instance, writer);

            writer.ToString().RemoveStyleElement()
                  .Should()
                  .BeEquivalentHtmlTo($@"<table><thead><tr><th>Item1</th><th>Item2</th></tr></thead>
<tbody><tr><td>{Tags.PlainTextBegin}123{Tags.PlainTextEnd}</td><td>{Tags.PlainTextBegin}hello{Tags.PlainTextEnd}</td></tr></tbody></table>");
        }

        [Fact]
        public void Object_properties_are_formatted_using_plain_text_formatter()
        {
            var writer = new StringWriter();

            var instance = new
            {
                A = 123,
                B = new { BA = 456 }
            };

            var formatter = HtmlFormatter.GetPreferredFormatterFor(instance.GetType());

            formatter.Format(instance, writer);

            writer.ToString().RemoveStyleElement()
                  .Should()
                  .Match($"<table><thead><tr><th>A</th><th>B</th></tr></thead><tbody><tr><td>{Tags.PlainTextBegin}123{Tags.PlainTextEnd}</td><td>{Tags.PlainTextBegin}&lt;&gt;f__AnonymousType*&lt;Int32&gt;{Environment.NewLine}      BA: 456{Tags.PlainTextEnd}</td></tr></tbody></table>");
        }

        [Fact]
        public void Sequence_properties_are_formatted_using_plain_text_formatter()
        {
            var writer = new StringWriter();

            var instance = new
            {
                PropertyA = 123,
                PropertyB = Enumerable.Range(1, 3)
            };

            var formatter = HtmlFormatter.GetPreferredFormatterFor(instance.GetType());

            formatter.Format(instance, writer);

            writer.ToString()
                  .Should()
                  .Contain($"<table><thead><tr><th>PropertyA</th><th>PropertyB</th></tr></thead><tbody><tr><td>{Tags.PlainTextBegin}123{Tags.PlainTextEnd}</td><td>{Tags.PlainTextBegin}[ 1, 2, 3 ]{Tags.PlainTextEnd}</td></tr></tbody></table>");
        }

        [Fact]
        public void Collection_properties_are_formatted_using_plain_text_formatting()
        {
            var writer = new StringWriter();

            var instance = new
            {
                PropertyA = Enumerable.Range(1, 3)
            };

            var formatter = HtmlFormatter.GetPreferredFormatterFor(instance.GetType());

            formatter.Format(instance, writer);

            writer.ToString()
                  .Should()
                  .Contain("[ 1, 2, 3 ]");
        }

        [Fact]
        public void It_displays_exceptions_thrown_by_properties_in_the_property_value_cell()
        {
            var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(SomePropertyThrows));

            var writer = new StringWriter();

            var widget = new SomePropertyThrows();

            formatter.Format(widget, writer);

            writer.ToString()
                  .Should()
                  .Contain($"<td>{Tags.PlainTextBegin}System.Exception");
        }

        [Fact]
        public void Type_instances_do_not_have_properties_expanded()
        {
            var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(Type));

            var writer = new StringWriter();

            formatter.Format(typeof(Dummy.ClassNotInSystemNamespace), writer);

            writer.ToString()
                  .Should()
                  .Be("Dummy.ClassNotInSystemNamespace");
        }

        [Fact]
        public void Type_instances_have_link_added_for_System_namespace_type()
        {
            var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(Type));

            var writer = new StringWriter();

            formatter.Format(typeof(string), writer);

            writer.ToString()
                  .Should()
                  .Be("<span><a href=\"https://docs.microsoft.com/dotnet/api/system.string?view=net-7.0\">System.String</a></span>");
        }


        [Fact]
        public void Type_instances_have_link_added_for_Microsoft_namespace_type()
        {
            var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(Type));

            var writer = new StringWriter();

            formatter.Format(typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException), writer);

            writer.ToString()
                  .Should()
                  .Be("<span><a href=\"https://docs.microsoft.com/dotnet/api/microsoft.csharp.runtimebinder.runtimebinderexception?view=net-7.0\">Microsoft.CSharp.RuntimeBinder.RuntimeBinderException</a></span>");
        }


        [Fact]
        public void Enums_are_formatted_using_their_names()
        {
            var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(FileAccess));

            var writer = new StringWriter();

            formatter.Format(FileAccess.ReadWrite, writer);

            writer.ToString().Should().Contain("ReadWrite");
        }

        [Fact]
        public void TimeSpan_is_not_destructured()
        {
            var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(TimeSpan));

            var writer = new StringWriter();

            var timespan = 25.Milliseconds();

            formatter.Format(timespan, writer);

            writer.ToString().Should().Contain(timespan.ToString());
        }

    }
}