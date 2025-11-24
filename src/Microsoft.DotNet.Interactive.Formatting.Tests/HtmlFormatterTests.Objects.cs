// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using Dummy;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.DotNet.Interactive.Formatting.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Formatting.Tests;

public partial class HtmlFormatterTests
{
    public class Objects : FormatterTestBase
    {
        [Fact]
        public void Formatters_are_generated_on_the_fly_for_anonymous_types()
        {
            var output = new { a = 123 }.ToDisplayString(HtmlFormatter.MimeType);

            output
                .RemoveStyleElement()
                .Should()
                .BeEquivalentHtmlTo($$"""
                    <details open="open" class="dni-treeview">
                        {{Tags.SummaryTextBegin}}{ a = 123 }{{Tags.SummaryTextEnd}}
                        <div>
                            <table>
                                <thead>
                                    <tr></tr>
                                </thead>
                                <tbody>
                                    <tr>
                                        <td>a</td>
                                        <td>
                                            <div class="dni-plaintext">
                                                <pre>123</pre>
                                            </div>
                                        </td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                    </details>
                    """);
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

            var output = ((object)expando).ToDisplayString(formatter).RemoveStyleElement();

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
        public void It_formats_objects_as_tree()
        {
            var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(EntityId));

            var writer = new StringWriter();

            var instance = new EntityId("TheEntity", "123");

            formatter.Format(instance, writer);

            var html = writer.ToString().RemoveStyleElement();

            html
                .Should()
                .BeEquivalentHtmlTo($"""
                    <details open="open" class="dni-treeview">
                        {Tags.SummaryTextBegin}{typeof(EntityId).FullName}{Tags.SummaryTextEnd}
                        <div>
                            <table>
                                <thead>
                                    <tr></tr>
                                </thead>
                                <tbody>
                                    <tr>
                                        <td>TypeName</td>
                                        <td>
                                            <div class="dni-plaintext">
                                                <pre>TheEntity</pre>
                                            </div>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>Id</td>
                                        <td>
                                            <div class="dni-plaintext">
                                                <pre>123</pre>
                                            </div>
                                        </td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                    </details>
                    """);
        }

        [Fact]
        public void Recursive_formatter_calls_do_not_cause_exceptions()
        {
            var widget = new Widget();
            widget.Parts = new List<Part> { new() { Widget = widget } };

            var formatter = HtmlFormatter.GetPreferredFormatterFor(widget.GetType());

            FormatContext context = new(new StringWriter());

            formatter.Invoking(f => f.Format(widget, context)).Should().NotThrow();
        }

        [Fact]
        public void Formatted_objects_include_custom_styles()
        {
            var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(DirectoryInfo));

            var writer = new StringWriter();

            var instance = new DirectoryInfo(".");

            formatter.Format(instance, writer);

            var html = writer.ToString();

            html.Should().Contain(Tags.DefaultStyles);
        }
        
        [Fact]
        public void It_formats_value_tuples_as_tree()
        {
            var writer = new StringWriter();

            var instance = (123, "hello");

            var formatter = HtmlFormatter.GetPreferredFormatterFor(instance.GetType());

            formatter.Format(instance, writer);

            writer.ToString()
                  .RemoveStyleElement()
                  .Should()
                  .BeEquivalentHtmlTo($"""
                    <details open="open" class="dni-treeview">
                        {Tags.SummaryTextBegin}(123, hello){Tags.SummaryTextEnd}
                    <div>
                        <table>
                            <thead>
                                <tr></tr>
                            </thead>
                            <tbody>
                                <tr>
                                    <td>Item1</td>
                                    <td>
                                        <div class="dni-plaintext">
                                            <pre>123</pre>
                                        </div>
                                    </td>
                                </tr>
                                <tr>
                                    <td>Item2</td>
                                    <td>
                                        <div class="dni-plaintext">
                                            <pre>hello</pre>
                                        </div>
                                    </td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </details>
                """);
        }

        [Fact]
        public void Object_properties_are_formatted_using_nested_trees()
        {
            var writer = new StringWriter();

            var instance = new
            {
                A = 123,
                B = new { BA = 456 }
            };

            var formatter = HtmlFormatter.GetPreferredFormatterFor(instance.GetType());

            formatter.Format(instance, writer);

            var expected = $$"""
                        <details open="open" class="dni-treeview">
                           <summary><span class="dni-code-hint"><code>{ A = 123, B = { BA = 456 } }</code></span></summary>
                           <div>
                               <table>
                                   <thead>
                                       <tr></tr>
                                   </thead>
                                   <tbody>
                                       <tr>
                                           <td>A</td>
                                           <td>
                                               <div class="dni-plaintext">
                                                   <pre>123</pre>
                                               </div>
                                           </td>
                                       </tr>
                                       <tr>
                                           <td>B</td>
                                           <td>
                                               <details open="open" class="dni-treeview">
                                                   <summary><span class="dni-code-hint"><code>{ BA = 456 }</code></span></summary>
                                                   <div>
                                                       <table>
                                                           <thead>
                                                               <tr></tr>
                                                           </thead>
                                                           <tbody>
                                                               <tr>
                                                                   <td>BA</td>
                                                                   <td>
                                                                       <div class="dni-plaintext">
                                                                           <pre>456</pre>
                                                                       </div>
                                                                   </td>
                                                               </tr>
                                                           </tbody>
                                                       </table>
                                                   </div>
                                               </details>
                                           </td>
                                       </tr>
                                   </tbody>
                               </table>
                           </div>
                       </details>
                       """;

            var actual = writer.ToString().RemoveStyleElement();

            actual
                  .Should()
                  .BeEquivalentHtmlTo(
                      expected);
        }

        [Fact]
        public void Scalar_sequence_properties_are_formatted_using_plain_text_formatter()
        {
            var writer = new StringWriter();

            var instance = new
            {
                PropertyA = 123,
                PropertyB = Enumerable.Range(1, 3)
            };

            var formatter = HtmlFormatter.GetPreferredFormatterFor(instance.GetType());

            formatter.Format(instance, writer);

            var html = writer.ToString().RemoveStyleElement();

            html
                .Should()
                .BeEquivalentHtmlTo(
                    $$"""
                       <details open="open" class="dni-treeview">
                           {{Tags.SummaryTextBegin}}{ PropertyA = 123, PropertyB = System.Linq.Enumerable+RangeIterator`1[System.Int32] }{{Tags.SummaryTextEnd}}
                           <div>
                               <table>
                                   <thead>
                                       <tr></tr>
                                   </thead>
                                   <tbody>
                                       <tr>
                                           <td>PropertyA</td>
                                           <td>
                                               <div class="dni-plaintext">
                                                   <pre>123</pre>
                                               </div>
                                           </td>
                                       </tr>
                                       <tr>
                                           <td>PropertyB</td>
                                           <td>
                                               <div class="dni-plaintext">
                                                   <pre>[ 1, 2, 3 ]</pre>
                                               </div>
                                           </td>
                                       </tr>
                                   </tbody>
                               </table>
                           </div>
                       </details>
                       """);
        }

        [Fact]
        public void It_displays_exceptions_thrown_by_properties_in_the_property_value_cell()
        {
            var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(SomePropertyThrows));

            var writer = new StringWriter();

            var widget = new SomePropertyThrows();

            formatter.Format(widget, writer);

            var html = writer.ToString();

            html
                  .Should()
                  .Contain("""
                    <tr>
                        <td>NotOk</td>
                        <td>
                            <details open="open" class="dni-treeview">
                                <summary><span class="dni-code-hint"><code>System.Exception: not ok
                    """.Crunch());
        }

        [Fact]
        public void Type_instances_do_not_have_properties_expanded()
        {
            var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(Type));

            var writer = new StringWriter();

            formatter.Format(typeof(ClassNotInSystemNamespace), writer);

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

            formatter.Format(typeof(RuntimeBinderException), writer);

            writer.ToString()
                  .Should()
                  .Be(
                      "<span><a href=\"https://docs.microsoft.com/dotnet/api/microsoft.csharp.runtimebinder.runtimebinderexception?view=net-7.0\">Microsoft.CSharp.RuntimeBinder.RuntimeBinderException</a></span>");
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