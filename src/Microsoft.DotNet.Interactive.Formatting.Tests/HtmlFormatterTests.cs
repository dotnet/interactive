// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;

namespace Microsoft.DotNet.Interactive.Formatting.Tests
{
    public class HtmlFormatterTests : FormatterTestBase
    {
        public class Objects : FormatterTestBase
        {
            [Fact]
            public void Formatters_are_generated_on_the_fly_when_HTML_mime_type_is_requested()
            {
                var output = new { a = 123 }.ToDisplayString(HtmlFormatter.MimeType);

                output.Should()
                      .Be("<table><thead><tr><th>a</th></tr></thead><tbody><tr><td>123</td></tr></tbody></table>");
            }

            [Fact]
            public void Null_references_are_indicated()
            {
                string value = null;

                value.ToDisplayString(HtmlFormatter.MimeType)
                     .Should()
                     .Be("&lt;null&gt;");
            }

            [Fact]
            public void Formatter_does_not_put_span_around_string()
            {
                var formatter = HtmlFormatter.GetPreferredFormatterFor<string>();

                var s = "hello".ToDisplayString(formatter);

                s.Should().Be("hello");
            }

            [Fact]
            public void Formatter_expands_properties_of_ExpandoObjects()
            {
                dynamic expando = new ExpandoObject();
                expando.Name = "socks";
                expando.Count = 2;

                var formatter = HtmlFormatter.GetPreferredFormatterFor<ExpandoObject>();

                var output = ((object) expando).ToDisplayString(formatter);

                output.Should().Be("<table><thead><tr><th>Count</th><th>Name</th></tr></thead><tbody><tr><td>2</td><td>socks</td></tr></tbody></table>");
            }

            [Fact]
            public void It_formats_objects_as_tables_having_properties_on_the_y_axis()
            {
                var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(EntityId));

                var writer = new StringWriter();

                var instance = new EntityId("TheEntity", "123");

                formatter.Format(instance, writer);

                writer.ToString()
                      .Should()
                      .Be("<table><thead><tr><th>TypeName</th><th>Id</th></tr></thead><tbody><tr><td>TheEntity</td><td>123</td></tr></tbody></table>");
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

                writer.ToString()
                      .Should()
                      .Be("<table><thead><tr><th>PropertyA</th><th>PropertyB</th></tr></thead><tbody><tr><td>123</td><td>hello</td></tr></tbody></table>");
            }

            [Fact]
            public void It_formats_tuples_as_tables_having_properties_on_the_y_axis()
            {
                var writer = new StringWriter();

                var instance = (123, "hello");

                var formatter = HtmlFormatter.GetPreferredFormatterFor(instance.GetType());

                formatter.Format(instance, writer);

                writer.ToString()
                      .Should()
                      .Be("<table><thead><tr><th>Item1</th><th>Item2</th></tr></thead><tbody><tr><td>123</td><td>hello</td></tr></tbody></table>");
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

                writer.ToString()
                      .Should()
                      .Contain("<table><thead><tr><th>A</th><th>B</th></tr></thead><tbody><tr><td>123</td><td>{ BA = 456 }</td></tr></tbody></table>");
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
                      .Contain("<table><thead><tr><th>PropertyA</th><th>PropertyB</th></tr></thead><tbody><tr><td>123</td><td>[ 1, 2, 3 ]</td></tr></tbody></table>");
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
                      .Contain("<td>System.Exception: not ok");
            }

            [Fact]
            public void Type_instances_do_not_have_properties_expanded()
            {
                var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(Type));

                var writer = new StringWriter();

                formatter.Format(typeof(Dummy.DummyNotInSystemNamespace), writer);

                writer.ToString()
                      .Should()
                      .Be("Dummy.DummyNotInSystemNamespace");
            }

            [Fact]
            public void Type_instances_have_link_added_for_System_namespace_type()
            {
                var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(Type));

                var writer = new StringWriter();

                formatter.Format(typeof(string), writer);

                writer.ToString()
                      .Should()
                      .Be("<span><a href=\"https://docs.microsoft.com/dotnet/api/system.string?view=netcore-3.0\">System.String</a></span>");
            }


            [Fact]
            public void Type_instances_have_link_added_for_Microsoft_namespace_type()
            {
                var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(Type));

                var writer = new StringWriter();

                formatter.Format(typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException), writer);

                writer.ToString()
                      .Should()
                      .Be("<span><a href=\"https://docs.microsoft.com/dotnet/api/microsoft.csharp.runtimebinder.runtimebinderexception?view=netcore-3.0\">Microsoft.CSharp.RuntimeBinder.RuntimeBinderException</a></span>");
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

        public class PreformatPlainText : FormatterTestBase
        {
            [Fact]
            public void It_can_format_with_plain_text_preformatted()
            {
                var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(long));
                HtmlFormatter.PlainTextPreformat = true;

                var writer = new StringWriter();

                long instance = 6L;

                formatter.Format(instance, writer);

                writer.ToString()
                      .Should()
                      .BeEquivalentHtmlTo(
                          $"<pre style=\"text-align: left;\">6</pre>");
            }

            [Fact]
            public void It_can_format_with_plain_text_formatted_with_default_font()
            {
                var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(long));
                HtmlFormatter.PlainTextPreformat = true;
                HtmlFormatter.PlainTextPreformatDefaultFont = true;

                var writer = new StringWriter();

                long instance = 6L;

                formatter.Format(instance, writer);

                writer.ToString()
                      .Should()
                      .BeEquivalentHtmlTo(
                          $"<div style=\"white-space: pre; text-align: left;\">6</div>");
            }

            [Fact]
            public void It_can_format_with_plain_text_formatted_with_default_font_no_left_justify()
            {
                var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(long));
                HtmlFormatter.PlainTextPreformat = true;
                HtmlFormatter.PlainTextPreformatDefaultFont = true;
                HtmlFormatter.PlainTextPreformatNoLeftJustify = true;

                var writer = new StringWriter();

                long instance = 6L;

                formatter.Format(instance, writer);

                writer.ToString()
                      .Should()
                      .BeEquivalentHtmlTo(
                          $"<div style=\"white-space: pre;\">6</div>");
            }

            [Fact]
            public void It_can_format_with_plain_text_formatted_with_no_left_justify()
            {
                var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(long));
                HtmlFormatter.PlainTextPreformat = true;
                HtmlFormatter.PlainTextPreformatNoLeftJustify = true;

                var writer = new StringWriter();

                long instance = 6L;

                formatter.Format(instance, writer);

                writer.ToString()
                      .Should()
                      .BeEquivalentHtmlTo(
                          $"<pre>6</pre>");
            }

        }
        public class Sequences : FormatterTestBase
        {
            [Fact]
            public void It_formats_sequences_as_tables_with_an_index_on_the_y_axis()
            {
                var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(List<EntityId>));

                var writer = new StringWriter();

                var instance = new List<EntityId>
                {
                    new EntityId("entity one", "123"),
                    new EntityId("entity two", "456")
                };

                formatter.Format(instance, writer);

                writer.ToString()
                      .Should()
                      .BeEquivalentHtmlTo(
                          "<table><thead><tr><th><i>index</i></th><th>TypeName</th><th>Id</th></tr></thead><tbody><tr><td>0</td><td>entity one</td><td>123</td></tr><tr><td>1</td><td>entity two</td><td>456</td></tr></tbody></table>");
            }

            [Fact]
            public void It_formats_sequence_properties_using_plain_text_formatting()
            {
                var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(List<float[]>));

                var writer = new StringWriter();

                var listOfArrays = new List<float[]>
                {
                    new[]
                    {
                        1.1f,
                        2.2f,
                        3.3f
                    }
                };

                formatter.Format(listOfArrays, writer);

                writer.ToString()
                      .Should()
                      .Contain($"<td>{listOfArrays.First().ToDisplayString("text/plain")}</td>");
            }
            
            [Fact]
            public void It_formats_generic_dictionaries_that_arent_non_generic_as_tables_with_the_key_on_the_y_axis()
            {
                var writer = new StringWriter();

                IDictionary<string, EntityId> instance = new GenericDictionary<string, EntityId>
                {
                    { "first", new EntityId("entity one", "123") },
                    { "second", new EntityId("entity two", "456") }
                };

                var formatter = HtmlFormatter.GetPreferredFormatterFor(instance.GetType());

                formatter.Format(instance, writer);

                writer.ToString()
                      .Should()
                      .BeEquivalentHtmlTo(
                          "<table><thead><tr><th><i>key</i></th><th>TypeName</th><th>Id</th></tr></thead><tbody><tr><td>first</td><td>entity one</td><td>123</td></tr><tr><td>second</td><td>entity two</td><td>456</td></tr></tbody></table>");
            }

            [Fact]
            public void It_formats_non_generic_dictionaries_that_arent_generic_as_tables_with_the_key_on_the_y_axis()
            {
                var writer = new StringWriter();

                IDictionary instance = new NonGenericDictionary
                {
                    { "first", new EntityId("entity one", "123") },
                    { "second", new EntityId("entity two", "456") }
                };

                var formatter = HtmlFormatter.GetPreferredFormatterFor(instance.GetType());

                formatter.Format(instance, writer);

                writer.ToString()
                      .Should()
                      .BeEquivalentHtmlTo(
                          "<table><thead><tr><th><i>key</i></th><th>TypeName</th><th>Id</th></tr></thead><tbody><tr><td>first</td><td>entity one</td><td>123</td></tr><tr><td>second</td><td>entity two</td><td>456</td></tr></tbody></table>");
            }

            [Fact]
            public void It_formats_DateTime_correctly()
            {
                var date1 = DateTime.Now;

                var html = date1.ToDisplayString("text/html");

                html.Should().BeEquivalentHtmlTo(
                    $"<span>{date1.ToString("u")}</span>");
            }

            [Fact]
            public void It_formats_DateTimeOffset_correctly()
            {
                var date1 = DateTimeOffset.Now;

                var html = date1.ToDisplayString("text/html");

                html.Should().BeEquivalentHtmlTo(
                    $"<span>{date1.ToString("u")}</span>");
            }

            [Fact]
            public void It_formats_enum_value_using_the_name()
            {
                var day = DayOfWeek.Monday;

                var html = day.ToDisplayString("text/html");

                html.Should().BeEquivalentHtmlTo(
                    $"<span>{day.ToString()}</span>");
            }

            [Fact]
            public void It_formats_string_with_encoding_and_preserving_whitespace_but_without_a_span()
            {
                var text = "hello<b>world  </b>  \n\n  ";

                var html = text.ToDisplayString("text/html");

                html.Should().Be("hello&lt;b&gt;world  &lt;/b&gt;  \n\n  ");
            }

            [Fact]
            public void It_formats_nested_string_with_encoding_and_whitespace_but_without_a_span()
            {
                // Note: although the whitespace in the string is emitted, it is not
                // yet in a `<pre>` section, so will be treated according the HTML whitespace
                // rules.
                //
                // Note: the absence of a span is dubious in the inner position.  It comes from this:
                //    case string s:
                //        writer.Write(s.HtmlEncode());
                //        break;
                //
                // This test is added to capture the status quo, rather than because this is necessarily correct.

                var text = new Tuple<string>("hello<b>world  </b>  \n\n  ");

                var html = text.ToDisplayString("text/html");

                html.Should().Be("<table><thead><tr><th>Item1</th></tr></thead><tbody><tr><td>hello&lt;b&gt;world  &lt;/b&gt;  \n\n  </td></tr></tbody></table>");
            }

            [Fact]
            public void It_formats_string_arrays_correctly()
            {
                var strings = new[] { "apple", "banana", "cherry" };

                strings.ToDisplayString("text/html")
                       .Should()
                       .BeEquivalentHtmlTo(
                           "<table><thead><tr><th><i>index</i></th><th>value</th></tr></thead><tbody><tr><td>0</td><td>apple</td></tr><tr><td>1</td><td>banana</td></tr><tr><td>2</td><td>cherry</td></tr></tbody></table>");
            }

            [Fact]
            public void It_formats_ordered_enumerables_correctly()
            {
                var sorted = new[]
                        { "kiwi", "plantain", "apple" }
                    .OrderBy(fruit => fruit.Length);

                var html = sorted.ToDisplayString("text/html");

                html.Should()
                    .BeEquivalentHtmlTo(
                        "<table><thead><tr><th><i>index</i></th><th>value</th></tr></thead><tbody><tr><td>0</td><td>kiwi</td></tr><tr><td>1</td><td>apple</td></tr><tr><td>2</td><td>plantain</td></tr></tbody></table>");
            }

            [Fact]
            public void Empty_sequences_are_indicated()
            {
                var list = new List<string>();

                var html = list.ToDisplayString("text/html");

                html.Should().BeEquivalentHtmlTo("<i>(empty)</i>");
            }

            [Fact]
            public void Empty_dictionaries_are_indicated()
            {
                var list = new Dictionary<int, int>();

                var html = list.ToDisplayString("text/html");

                html.Should().BeEquivalentHtmlTo("<i>(empty)</i>");
            }

            [Fact]
            public void Formatter_truncates_expansion_of_long_IEnumerable()
            {
                var list = new List<string>();
                for (var i = 1; i < 11; i++)
                {
                    list.Add("number " + i);
                }

                Formatter.ListExpansionLimit = 4;

                var formatter = HtmlFormatter.GetPreferredFormatterFor(list.GetType());

                var formatted = list.ToDisplayString(formatter);

                formatted.Contains("number 1").Should().BeTrue();
                formatted.Contains("number 4").Should().BeTrue();
                formatted.Should().NotContain("number 5");
                formatted.Contains("6 more").Should().BeTrue();
            }

            [Fact]
            public void Formatter_truncates_expansion_of_long_IDictionary()
            {
                var list = new Dictionary<string, int>();

                for (var i = 1; i < 11; i++)
                {
                    list.Add("number " + i, i);
                }

                Formatter.ListExpansionLimit = 4;

                var formatter = HtmlFormatter.GetPreferredFormatterFor(list.GetType());

                var formatted = list.ToDisplayString(formatter);

                formatted.Contains("number 1").Should().BeTrue();
                formatted.Contains("number 4").Should().BeTrue();
                formatted.Should().NotContain("number 5");
                formatted.Contains("6 more").Should().BeTrue();
            }

            [Fact]
            public void DateTime_is_not_destructured()
            {
                var date1 = DateTime.Now;
                var date2 = DateTime.Now.AddHours(1.23);

                var objects = new object[] { date1, date2 };

                var html = objects.ToDisplayString("text/html");

                html.Should().BeEquivalentHtmlTo(
                    $"<table><thead><tr><th><i>index</i></th><th>value</th></tr></thead><tbody><tr><td>0</td><td><span>{date1.ToDisplayString("text/plain")}</span></td></tr><tr><td>1</td><td><span>{date2.ToDisplayString("text/plain")}</span></td></tr></tbody></table>");
            }

            [Fact]
            public void System_Type_is_not_destructured()
            {
                var objects = new object[] { typeof(string), typeof(int) };

                var html = objects.ToDisplayString("text/html");

                html.Should().BeEquivalentHtmlTo($@"<table>
      <thead>
        <tr>
          <th>
            <i>index</i>
          </th>
          <th>value</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td>0</td>
          <td>
            <span>
              <a href={"\"https://docs.microsoft.com/dotnet/api/system.string?view=netcore-3.0\""}>System.String</a>
            </span>
          </td>
        </tr>
        <tr>
          <td>1</td>
          <td>
            <span>
              <a href={"\"https://docs.microsoft.com/dotnet/api/system.int32?view=netcore-3.0\""}>System.Int32</a>
            </span>
          </td>
        </tr>
      </tbody>
    </table>");
            }

            [Fact]
            public void ReadOnlyMemory_of_char_is_formatted_like_a_string()
            {
                var formatter = HtmlFormatter.GetPreferredFormatterFor<ReadOnlyMemory<char>>();

                var writer = new StringWriter();

                var readOnlyMemory = "Hi!".AsMemory();

                formatter.Format(readOnlyMemory, writer);

                writer.ToString()
                      .Should()
                      .BeEquivalentHtmlTo("<span>Hi!</span>");
            }

            [Fact]
            public void ReadOnlyMemory_of_int_is_formatted_like_a_int_array()
            {
                var formatter = HtmlFormatter.GetPreferredFormatterFor<ReadOnlyMemory<int>>();

                var writer = new StringWriter();

                var readOnlyMemory = new ReadOnlyMemory<int>(new[] { 7, 8, 9 });

                formatter.Format(readOnlyMemory, writer);

                writer.ToString()
                      .Should()
                      .BeEquivalentHtmlTo(
                          "<table><thead><tr><th><i>index</i></th><th>value</th></tr></thead><tbody><tr><td>0</td><td>7</td></tr><tr><td>1</td><td>8</td></tr><tr><td>2</td><td>9</td></tr></tbody></table>");
            }

            [Fact]
            public void It_shows_null_items_in_the_sequence_as_null()
            {
                var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(object[]));

                var writer = new StringWriter();

                formatter.Format(new object[] { 8, null, 9 }, writer);

                writer.ToString().Should()
                      .BeEquivalentHtmlTo(
                          "<table><thead><tr><th><i>index</i></th><th>value</th></tr></thead><tbody><tr><td>0</td><td>8</td></tr><tr><td>1</td><td>&lt;null&gt;</td></tr><tr><td>2</td><td>9</td></tr></tbody></table>");
            }

            [Fact]
            public void Sequences_can_contain_different_types_of_elements()
            {
                IEnumerable<object> GetCollection()
                {
                    yield return true;
                    yield return 99;
                    yield return "Hello, World";
                }

                var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(IEnumerable<object>));

                var writer = new StringWriter();

                formatter.Format(GetCollection(), writer);

                writer.ToString().Should()
                      .BeEquivalentHtmlTo($@"<table>
      <thead>
        <tr>
          <th>
            <i>index</i>
          </th>
          <th>
            <i>type</i>
          </th>
          <th>value</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td>0</td>
          <td>
            <span>
              <a href={"\"https://docs.microsoft.com/dotnet/api/system.boolean?view=netcore-3.0\""}>System.Boolean</a>
            </span>
          </td>
          <td>True</td>
        </tr>
        <tr>
          <td>1</td>
          <td>
            <span>
              <a href={"\"https://docs.microsoft.com/dotnet/api/system.int32?view=netcore-3.0\""}>System.Int32</a>
            </span>
          </td>
          <td>99</td>
        </tr>
        <tr>
          <td>2</td>
          <td>
            <span>
              <a href={"\"https://docs.microsoft.com/dotnet/api/system.string?view=netcore-3.0\""}>System.String</a>
            </span>
          </td>
          <td>Hello, World</td>
        </tr>
      </tbody>
    </table>");
            }
            
            [Fact]
            public void All_properties_are_shown_when_sequences_contain_different_types()
            {
                var objects = new object[]
                {
                    1, 
                    (2, "two"), 
                    Enumerable.Range(1, 3),
                    new { name = "apple", color = "green" },
                };

                var result= objects.ToDisplayString("text/html");
                result
                       .Should()
                       .BeEquivalentHtmlTo(
                          $@"<table>
      <thead>
        <tr>
          <th>
            <i>index</i>
          </th>
          <th>
            <i>type</i>
          </th>
          <th>value</th>
          <th>Item1</th>
          <th>Item2</th>
          <th>name</th>
          <th>color</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td>0</td>
          <td>
            <span>
              <a href={"\"https://docs.microsoft.com/dotnet/api/system.int32?view=netcore-3.0\""}>System.Int32</a>
            </span>
          </td>
          <td>1</td>
          <td></td>
          <td></td>
          <td></td>
          <td></td>
        </tr>
        <tr>
          <td>1</td>
          <td>
            <span>
              <a href={"\"https://docs.microsoft.com/dotnet/api/system.valuetuple-2?view=netcore-3.0\""}>System.ValueTuple&lt;System.Int32,System.String&gt;</a>
            </span>
          </td>
          <td></td>
          <td>2</td>
          <td>two</td>
          <td></td>
          <td></td>
        </tr>
        <tr>
          <td>2</td>
          <td>
            <span>
              <a href={"\"https://docs.microsoft.com/dotnet/api/system.linq.enumerable.rangeiterator?view=netcore-3.0\""}>System.Linq.Enumerable+RangeIterator</a>
            </span>
          </td>
          <td>[ 1, 2, 3 ]</td>
          <td></td>
          <td></td>
          <td></td>
          <td></td>
        </tr>
        <tr>
          <td>3</td>
          <td>(anonymous)</td>
          <td></td>
          <td></td>
          <td></td>
          <td>apple</td>
          <td>green</td>
        </tr>
      </tbody>
    </table>");
            }
        }
    }
}

namespace Dummy
{
    public class DummyNotInSystemNamespace { } 
}