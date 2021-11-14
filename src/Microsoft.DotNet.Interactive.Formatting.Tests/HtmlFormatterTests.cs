﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.DotNet.Interactive.Formatting.Tests.Tags;

namespace Microsoft.DotNet.Interactive.Formatting.Tests
{
    public static class Tags
    {
        public const string PlainTextBegin = "<div class=\"dni-plaintext\">";
        public const string PlainTextEnd = "</div>";
    }

    public class HtmlFormatterTests : FormatterTestBase
    {
        public class Objects : FormatterTestBase
        {
            [Fact]
            public void Formatters_are_generated_on_the_fly_when_HTML_mime_type_is_requested()
            {
                var output = new { a = 123 }.ToDisplayString(HtmlFormatter.MimeType);

                output.Should()
                      .Be($"<table><thead><tr><th>a</th></tr></thead><tbody><tr><td>{PlainTextBegin}123{PlainTextEnd}</td></tr></tbody></table>");
            }

            [Fact]
            public void Null_references_are_indicated()
            {
                string value = null;

                value.ToDisplayString(HtmlFormatter.MimeType)
                     .Should()
                     .Be($"{PlainTextBegin}&lt;null&gt;{PlainTextEnd}");
            }

            [Fact]
            public void Formatter_puts_div_with_class_around_string()
            {
                var formatter = HtmlFormatter.GetPreferredFormatterFor<string>();

                var s = "hello".ToDisplayString(formatter);

                s.Should().Be($"{PlainTextBegin}hello{PlainTextEnd}");
            }

            [Fact]
            public void Formatter_expands_properties_of_ExpandoObjects()
            {
                dynamic expando = new ExpandoObject();
                expando.Name = "socks";
                expando.Count = 2;

                var formatter = HtmlFormatter.GetPreferredFormatterFor<ExpandoObject>();

                var output = ((object) expando).ToDisplayString(formatter);

                output.Should().BeEquivalentHtmlTo($@"
<table>
    <thead>
       <tr><th>Count</th><th>Name</th></tr>
    </thead>
    <tbody><tr><td>{PlainTextBegin}2{PlainTextEnd}</td><td>socks</td></tr>
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

                writer.ToString()
                      .Should()
                      .BeEquivalentHtmlTo($@"
<table>
   <thead><tr><th>TypeName</th><th>Id</th></tr></thead>
   <tbody>
     <tr><td>{PlainTextBegin}TheEntity{PlainTextEnd}</td><td>{PlainTextBegin}123{PlainTextEnd}</td></tr>
  </tbody>
</table>");
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
                      .BeEquivalentHtmlTo($@"
<table>
 <thead><tr><th>PropertyA</th><th>PropertyB</th></tr></thead>
 <tbody><tr><td>{PlainTextBegin}123{PlainTextEnd}</td><td>{PlainTextBegin}hello{PlainTextEnd}</td></tr></tbody>
</table>");
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
                      .BeEquivalentHtmlTo($@"<table><thead><tr><th>Item1</th><th>Item2</th></tr></thead>
<tbody><tr><td>{PlainTextBegin}123{PlainTextEnd}</td><td>{PlainTextBegin}hello{PlainTextEnd}</td></tr></tbody></table>");
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
                      .Contain($"<table><thead><tr><th>A</th><th>B</th></tr></thead><tbody><tr><td>{PlainTextBegin}123{PlainTextEnd}</td><td>{PlainTextBegin}{{ BA: 456 }}{PlainTextEnd}</td></tr></tbody></table>");
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
                      .Contain($"<table><thead><tr><th>PropertyA</th><th>PropertyB</th></tr></thead><tbody><tr><td>{PlainTextBegin}123{PlainTextEnd}</td><td>{PlainTextBegin}[ 1, 2, 3 ]{PlainTextEnd}</td></tr></tbody></table>");
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
                      .Contain($"<td>{PlainTextBegin}{{ System.Exception: not ok");
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
                      .Be("<span><a href=\"https://docs.microsoft.com/dotnet/api/system.string?view=net-5.0\">System.String</a></span>");
            }


            [Fact]
            public void Type_instances_have_link_added_for_Microsoft_namespace_type()
            {
                var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(Type));

                var writer = new StringWriter();

                formatter.Format(typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException), writer);

                writer.ToString()
                      .Should()
                      .Be("<span><a href=\"https://docs.microsoft.com/dotnet/api/microsoft.csharp.runtimebinder.runtimebinderexception?view=net-5.0\">Microsoft.CSharp.RuntimeBinder.RuntimeBinderException</a></span>");
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
            public void It_can_format_a_String_with_class()
            {
                var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(string));

                var writer = new StringWriter();

                var instance = @"this
is a 
   multiline<>
string";

                formatter.Format(instance, writer);

                writer.ToString()
                      .Should()
                      .BeEquivalentHtmlTo(
                          $"{PlainTextBegin}{instance.HtmlEncode()}{PlainTextEnd}");
            }

            [Fact]
            public void HtmlFormatter_returns_plain_for_BigInteger()
            {
                var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(BigInteger));

                var writer = new StringWriter();

                var instance = BigInteger.Parse("78923589327589332402359");

                formatter.Format(instance, writer);

                writer.ToString()
                    .Should()
                    .Be($"{PlainTextBegin}78923589327589332402359{PlainTextEnd}");
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
                    new("entity one", "123"),
                    new("entity two", "456")
                };

                formatter.Format(instance, writer);

                writer.ToString()
                      .Should()
                      .BeEquivalentHtmlTo(
                          @$"
<table>
   <thead>
     <tr><th><i>index</i></th><th>TypeName</th><th>Id</th></tr>
   </thead>
   <tbody>
     <tr><td>0</td><td>entity one</td><td>123</td></tr>
     <tr><td>1</td><td>entity two</td><td>456</td></tr>
   </tbody>
</table>");
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
                      .Contain($"<td>{PlainTextBegin}{listOfArrays.First().ToDisplayString("text/plain")}{PlainTextEnd}</td>");
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
                          $"<table><thead><tr><th><i>key</i></th><th>TypeName</th><th>Id</th></tr></thead><tbody><tr><td>first</td><td>entity one</td><td>123</td></tr><tr><td>second</td><td>entity two</td><td>456</td></tr></tbody></table>");
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
                          $@"<table>
    <thead>
       <tr><th><i>key</i></th><th>TypeName</th><th>Id</th></tr>
    </thead>
    <tbody>
      <tr><td>first</td><td>entity one</td><td>123</td></tr>
      <tr><td>second</td><td>entity two</td><td>456</td></tr>
    </tbody>
</table>");
            }

            [Fact]
            public void It_formats_DateTime_correctly()
            {
                var date1 = DateTime.Now;

                var html = date1.ToDisplayString("text/html");

                html.Should().BeEquivalentHtmlTo(
                    $"<span>{date1:u}</span>");
            }

            [Fact]
            public void It_formats_DateTimeOffset_correctly()
            {
                var date1 = DateTimeOffset.Now;

                var html = date1.ToDisplayString("text/html");

                html.Should().BeEquivalentHtmlTo(
                    $"<span>{date1:u}</span>");
            }

            [Fact]
            public void It_formats_enum_value_using_the_name()
            {
                var day = DayOfWeek.Monday;

                var html = day.ToDisplayString("text/html");

                html.Should().BeEquivalentHtmlTo(
                    $"<span>{day}</span>");
            }

            [Fact]
            public void It_formats_string_with_encoding_and_preserving_whitespace_and_with_tags()
            {
                var text = "hello<b>world  </b>  \n\n  ";

                var html = text.ToDisplayString("text/html");

                html.Should().Be($"{PlainTextBegin}hello&lt;b&gt;world  &lt;/b&gt;  \n\n  {PlainTextEnd}");
            }

            [Fact]
            public void It_formats_string_arrays_correctly()
            {
                var strings = new[] { "apple", "banana", "cherry" };

                strings.ToDisplayString("text/html")
                       .Should()
                       .BeEquivalentHtmlTo(
                           $@"
<table>
  <thead>
    <tr><th><i>index</i></th><th>value</th></tr>
  </thead>
  <tbody>
    <tr><td>0</td><td>apple</td></tr>
    <tr><td>1</td><td>banana</td></tr>
    <tr><td>2</td><td>cherry</td></tr>
  </tbody>
</table>");
            }

            [Fact]
            public void It_formats_ordered_enumerables_correctly()
            {
                var sorted = new[]
                        { "kiwi", "plantain", "apple" }
                    .OrderBy(fruit => fruit.Length);

                var html = sorted.ToDisplayString("text/html");

                html.Should()
                    .BeEquivalentHtmlTo($@"
<table>
  <thead>
     <tr><th><i>index</i></th><th>value</th></tr>
  </thead>
  <tbody>
     <tr><td>0</td><td>kiwi</td></tr>
     <tr><td>1</td><td>apple</td></tr>
     <tr><td>2</td><td>plantain</td></tr>
  </tbody>
</table>");
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
            public void Formatter_truncates_expansion_of_ICollection()
            {
                var list = new List<string>();
                for (var i = 1; i < 11; i++)
                {
                    list.Add("number " + i);
                }

                Formatter.ListExpansionLimit = 4;

                var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(ICollection));

                var formatted = list.ToDisplayString(formatter);

                formatted.Should().Contain("number 1");
                formatted.Should().Contain("number 4");
                formatted.Should().NotContain("number 5");
                formatted.Should().Contain("<i>(6 more)</i>");
            }

            [Fact]
            public void Formatter_truncates_expansion_of_IDictionary()
            {
                var list = new Dictionary<string, int>();

                for (var i = 1; i < 11; i++)
                {
                    list.Add("number " + i, i);
                }

                Formatter.ListExpansionLimit = 4;

                var formatter = HtmlFormatter.GetPreferredFormatterFor(list.GetType());

                var formatted = list.ToDisplayString(formatter);

                formatted.Should().Contain("number 1");
                formatted.Should().Contain("number 4");
                formatted.Should().NotContain("number 5");
                formatted.Should().Contain("6 more");
            }

            [Fact]
            public void Formatter_truncates_expansion_of_IEnumerable()
            {
                Formatter.ListExpansionLimit = 4;

                var formatter = HtmlFormatter.GetPreferredFormatterFor<IEnumerable<string>>();

                var formatted = InfiniteSequence().ToDisplayString(formatter);

                formatted.Should().Contain("number 9");
                formatted.Should().Contain("<i>... (more)</i>");

                static IEnumerable<string> InfiniteSequence()
                {
                    while(true)
                    {
                        yield return "number 9"; 
                    }
                }
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
              <a href={"\"https://docs.microsoft.com/dotnet/api/system.string?view=net-5.0\""}>System.String</a>
            </span>
          </td>
        </tr>
        <tr>
          <td>1</td>
          <td>
            <span>
              <a href={"\"https://docs.microsoft.com/dotnet/api/system.int32?view=net-5.0\""}>System.Int32</a>
            </span>
          </td>
        </tr>
      </tbody>
    </table>");
            }

            [Fact]
            public void Dictionary_with_non_string_keys_are_formatted_correctly()
            {
                var dict = new SomeDictUsingInterfaceImpls();

                var html = dict.ToDisplayString("text/html");

                html.Should().BeEquivalentHtmlTo(
                    $"<table><thead><tr><th><i>key</i></th><th>value</th></tr></thead><tbody><tr><td>{PlainTextBegin}1{PlainTextEnd}</td><td>2</td></tr></tbody></table>");
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
                          $@"
<table>
  <thead>
    <tr><th><i>index</i></th><th>value</th></tr>
  </thead>
  <tbody>
    <tr><td>0</td><td>{PlainTextBegin}7{PlainTextEnd}</td></tr>
    <tr><td>1</td><td>{PlainTextBegin}8{PlainTextEnd}</td></tr>
    <tr><td>2</td><td>{PlainTextBegin}9{PlainTextEnd}</td></tr>
  </tbody>
</table>");
            }

            [Fact]
            public void It_shows_null_items_in_the_sequence_as_null()
            {
                var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(object[]));

                var writer = new StringWriter();

                formatter.Format(new object[] { 8, null, 9 }, writer);

                writer.ToString().Should()
                      .BeEquivalentHtmlTo(
                          $@"
<table>
  <thead>
    <tr><th><i>index</i></th><th>value</th></tr>
  </thead>
  <tbody>
    <tr><td>0</td><td>{PlainTextBegin}8{PlainTextEnd}</td></tr>
    <tr><td>1</td><td>{PlainTextBegin}&lt;null&gt;{PlainTextEnd}</td></tr>
    <tr><td>2</td><td>{PlainTextBegin}9{PlainTextEnd}</td></tr>
  </tbody>
</table>");
            }

            [Fact]
            public void It_shows_properties_up_to_default_max()
            {
                var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(Dummy.DummyClassWithManyProperties));

                var writer = new StringWriter();

                formatter.Format(new Dummy.DummyClassWithManyProperties(), writer);

                writer.ToString().Should()
                      .BeEquivalentHtmlTo($@"<table>
      <thead>
        <tr>
          <th>X1</th>
          <th>X2</th>
          <th>X3</th>
          <th>X4</th>
          <th>X5</th>
          <th>X6</th>
          <th>X7</th>
          <th>X8</th>
          <th>X9</th>
          <th>X10</th>
          <th>X11</th>
          <th>X12</th>
          <th>X13</th>
          <th>X14</th>
          <th>X15</th>
          <th>X16</th>
          <th>X17</th>
          <th>X18</th>
          <th>X19</th>
          <th>X20</th>
          <th>..</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td>{PlainTextBegin}1{PlainTextEnd}</td>
          <td>{PlainTextBegin}2{PlainTextEnd}</td>
          <td>{PlainTextBegin}3{PlainTextEnd}</td>
          <td>{PlainTextBegin}4{PlainTextEnd}</td>
          <td>{PlainTextBegin}5{PlainTextEnd}</td>
          <td>{PlainTextBegin}6{PlainTextEnd}</td>
          <td>{PlainTextBegin}7{PlainTextEnd}</td>
          <td>{PlainTextBegin}8{PlainTextEnd}</td>
          <td>{PlainTextBegin}9{PlainTextEnd}</td>
          <td>{PlainTextBegin}10{PlainTextEnd}</td>
          <td>{PlainTextBegin}11{PlainTextEnd}</td>
          <td>{PlainTextBegin}12{PlainTextEnd}</td>
          <td>{PlainTextBegin}13{PlainTextEnd}</td>
          <td>{PlainTextBegin}14{PlainTextEnd}</td>
          <td>{PlainTextBegin}15{PlainTextEnd}</td>
          <td>{PlainTextBegin}16{PlainTextEnd}</td>
          <td>{PlainTextBegin}17{PlainTextEnd}</td>
          <td>{PlainTextBegin}18{PlainTextEnd}</td>
          <td>{PlainTextBegin}19{PlainTextEnd}</td>
          <td>{PlainTextBegin}20{PlainTextEnd}</td>
        </tr>
      </tbody>
    </table>");
            }

            [Fact]
            public void It_shows_properties_up_to_custom_max()
            {
                var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(Dummy.DummyClassWithManyProperties));

                var writer = new StringWriter();
                HtmlFormatter.MaxProperties = 1;

                formatter.Format(new Dummy.DummyClassWithManyProperties(), writer);

                writer.ToString().Should()
                      .BeEquivalentHtmlTo($@"<table>
      <thead>
        <tr>
          <th>X1</th>
          <th>..</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td>{PlainTextBegin}1{PlainTextEnd}</td>
        </tr>
      </tbody>
    </table>");
            }

            [Fact]
            public void Setting_properties_to_zero_means_no_table_formatting_and_plaintext_gets_used()
            {
                var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(Dummy.DummyClassWithManyProperties));

                var writer = new StringWriter();
                HtmlFormatter.MaxProperties = 0;
                PlainTextFormatter.MaxProperties = 0;

                formatter.Format(new Dummy.DummyClassWithManyProperties(), writer);

                writer.ToString().Should().Be($"{PlainTextBegin}Dummy.DummyClassWithManyProperties{PlainTextEnd}");
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
              <a href={"\"https://docs.microsoft.com/dotnet/api/system.boolean?view=net-5.0\""}>System.Boolean</a>
            </span>
          </td>
          <td>{PlainTextBegin}True{PlainTextEnd}</td>
        </tr>
        <tr>
          <td>1</td>
          <td>
            <span>
              <a href={"\"https://docs.microsoft.com/dotnet/api/system.int32?view=net-5.0\""}>System.Int32</a>
            </span>
          </td>
          <td>{PlainTextBegin}99{PlainTextEnd}</td>
        </tr>
        <tr>
          <td>2</td>
          <td>
            <span>
              <a href={"\"https://docs.microsoft.com/dotnet/api/system.string?view=net-5.0\""}>System.String</a>
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
                    new { name = "apple", color = "green" }
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
              <a href={"\"https://docs.microsoft.com/dotnet/api/system.int32?view=net-5.0\""}>System.Int32</a>
            </span>
          </td>
          <td>{PlainTextBegin}1{PlainTextEnd}</td>
          <td></td>
          <td></td>
          <td></td>
          <td></td>
        </tr>
        <tr>
          <td>1</td>
          <td>
            <span>
              <a href={"\"https://docs.microsoft.com/dotnet/api/system.valuetuple-2?view=net-5.0\""}>System.ValueTuple&lt;System.Int32,System.String&gt;</a>
            </span>
          </td>
          <td></td>
          <td>{PlainTextBegin}2{PlainTextEnd}</td>
          <td>two</td>
          <td></td>
          <td></td>
        </tr>
        <tr>
          <td>2</td>
          <td>
            <span>
              <a href={"\"https://docs.microsoft.com/dotnet/api/system.linq.enumerable.rangeiterator?view=net-5.0\""}>System.Linq.Enumerable+RangeIterator</a>
            </span>
          </td>
          <td>{PlainTextBegin}[ 1, 2, 3 ]{PlainTextEnd}</td>
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

            [Fact]
            public void All_properties_are_shown_when_sequences_contain_different_types_in_order_they_are_encountered()
            {
                var objects = new object[]
                {
                    new { name = "apple", Item2 = "green" },
                    (2, "two"),
                    1,
                    Enumerable.Range(1, 3),
                };

                var result = objects.ToDisplayString("text/html");
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
          <th>name</th>
          <th>Item2</th>
          <th>Item1</th>
          <th>value</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td>0</td>
          <td>(anonymous)</td>
          <td>apple</td>
          <td>green</td>
          <td></td>
          <td></td>
        </tr>
        <tr>
          <td>1</td>
          <td>
            <span>
              <a href={"\"https://docs.microsoft.com/dotnet/api/system.valuetuple-2?view=net-5.0\""}>System.ValueTuple&lt;System.Int32,System.String&gt;</a>
            </span>
          </td>
          <td></td>
          <td>two</td>
          <td>{PlainTextBegin}2{PlainTextEnd}</td>
          <td></td>
        </tr>
        <tr>
          <td>2</td>
          <td>
            <span>
              <a href={"\"https://docs.microsoft.com/dotnet/api/system.int32?view=net-5.0\""}>System.Int32</a>
            </span>
          </td>
          <td></td>
          <td></td>
          <td></td>
          <td>{PlainTextBegin}1{PlainTextEnd}</td>
        </tr>
        <tr>
          <td>3</td>
          <td>
            <span>
              <a href={"\"https://docs.microsoft.com/dotnet/api/system.linq.enumerable.rangeiterator?view=net-5.0\""}>System.Linq.Enumerable+RangeIterator</a>
            </span>
          </td>
          <td></td>
          <td></td>
          <td></td>
          <td>{PlainTextBegin}[ 1, 2, 3 ]{PlainTextEnd}</td>
        </tr>
      </tbody>
    </table>");
            }

            [Fact]
            public void Respective_HTML_formatters_are_used_when_sequences_contain_different_types()
            {
                var anonymousObj = new { name = "apple", color = "green" };

                Formatter.Register(anonymousObj.GetType(), (o, writer) => writer.Write($"<i>{o}</i>"), HtmlFormatter.MimeType);
              
                var objects = new object[]
                {
                    anonymousObj,
                    (123, "two"),
                    456,
                    new [] { 7, 8, 9 } 
                };

                var result = objects.ToDisplayString("text/html");
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
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>0</td>
      <td>(anonymous)</td>
      <td>
        <i>{{ name = apple, color = green }}</i>
      </td>
      <td></td>
      <td></td>
    </tr>
    <tr>
      <td>1</td>
      <td>
        <span>
          <a href=""https://docs.microsoft.com/dotnet/api/system.valuetuple-2?view=net-5.0"">System.ValueTuple&lt;System.Int32,System.String&gt;</a>
        </span>
      </td>
      <td></td>
      <td>
        <div class=""dni-plaintext"">123</div>
      </td>
      <td>two</td>
    </tr>
    <tr>
      <td>2</td>
      <td>
        <span>
          <a href=""https://docs.microsoft.com/dotnet/api/system.int32?view=net-5.0"">System.Int32</a>
        </span>
      </td>
      <td>
        <div class=""dni-plaintext"">456</div>
      </td>
      <td></td>
      <td></td>
    </tr>
    <tr>
      <td>3</td>
      <td>
        <span>
          <a href=""https://docs.microsoft.com/dotnet/api/system.int32[]?view=net-5.0"">System.Int32[]</a>
        </span>
      </td>
      <td>
        <div class=""dni-plaintext"">[ 7, 8, 9 ]</div>
      </td>
      <td></td>
      <td></td>
    </tr>
  </tbody>
</table>");
            }

            class SomeDictUsingInterfaceImpls : IDictionary<int, string>
            {
                string IDictionary<int, string>.this[int key] { get => "2"; set => throw new NotImplementedException(); }

                ICollection<int> IDictionary<int, string>.Keys => new[] { 1 };

                ICollection<string> IDictionary<int, string>.Values => new[] { "2" };

                int ICollection<KeyValuePair<int, string>>.Count => 1;

                bool ICollection<KeyValuePair<int, string>>.IsReadOnly => true;

                void IDictionary<int, string>.Add(int key, string value)
                {
                }

                void ICollection<KeyValuePair<int, string>>.Add(KeyValuePair<int, string> item)
                {
                }

                void ICollection<KeyValuePair<int, string>>.Clear()
                {
                }

                bool ICollection<KeyValuePair<int, string>>.Contains(KeyValuePair<int, string> item)
                {
                    return (item.Key == 1 && item.Value == "2");
                }

                bool IDictionary<int, string>.ContainsKey(int key)
                {
                    return (key == 1);
                }

                void ICollection<KeyValuePair<int, string>>.CopyTo(KeyValuePair<int, string>[] array, int arrayIndex)
                {
                    throw new NotImplementedException();
                }

                IEnumerator<KeyValuePair<int, string>> IEnumerable<KeyValuePair<int, string>>.GetEnumerator()
                {
                    return ((IEnumerable < KeyValuePair<int, string> > ) new[] { new KeyValuePair<int, string>(1, "2") }).GetEnumerator();
                }

                bool IDictionary<int, string>.Remove(int key)
                {
                    throw new NotImplementedException();
                }

                bool ICollection<KeyValuePair<int, string>>.Remove(KeyValuePair<int, string> item)
                {
                    return false;
                }

                bool IDictionary<int, string>.TryGetValue(int key, out string value)
                {
                    value = "2";
                    return (key == 1);
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return ((IEnumerable)new[] { new KeyValuePair<int, string>(1, "2") }).GetEnumerator();
                }
            }
        }

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

                var html = jsonDocument.ToDisplayString(HtmlFormatter.MimeType);

                html.Should().Contain(
                    "<tr><td><span>&quot;apple&quot;</span></td></tr><tr><td><span>&quot;banana&quot;</span></td></tr><tr><td><span>&quot;cherry&quot;</span></td></tr>");
            }

            [Fact]
            public void Strings_with_escaped_sequences_are_encoded()
            {
                var value = "hola! \n \t \" \" ' ' the joy of escapes! ==> &   white  space  ";

                var text = value.ToDisplayString("text/html");

                text.Should().Be("<div class=\"dni-plaintext\">hola! \n \t &quot; &quot; &#39; &#39; the joy of escapes! ==&gt; &amp;   white  space  </div>");
            }

            [Fact]
            public void Strings_with_unicode_sequences_are_encoded()
            {
                var value = "hola! ʰ˽˵ΘϱϪԘÓŴ𝓌🦁♿🌪🍒☝🏿";

                var text = value.ToDisplayString("text/html");

                text.Should().Be($"<div class=\"dni-plaintext\">{value.HtmlEncode()}</div>");
            }
        }
    }
}
