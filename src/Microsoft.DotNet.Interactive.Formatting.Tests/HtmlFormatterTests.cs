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
using static Microsoft.DotNet.Interactive.Formatting.Tests.Tags;

namespace Microsoft.DotNet.Interactive.Formatting.Tests
{
    public class Tags
    {
        public const string PlainTextBegin = "<div class=\"dni-plaintext\">";
        public const string PlainTextEnd = "</div>";

    }
    public class HtmlFormatterTests : FormatterTestBase
    {
        public class Objects : FormatterTestBase
        {

            [Fact]
            public void does_not_double_encode_HTML_string()
            {
                var htmlString = "<b>Text</b>";
                var formatter = HtmlFormatter.GetPreferredFormatterFor<string>();
                var writer = new StringWriter();

                formatter.Format(htmlString, writer);

                writer
                    .ToString()
                    .Should()
                    .Be(htmlString);
            }

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
    <tbody><tr><td>{PlainTextBegin}2{PlainTextEnd}</td><td>{PlainTextBegin}socks{PlainTextEnd}</td></tr>
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
                      .Contain($"<table><thead><tr><th>A</th><th>B</th></tr></thead><tbody><tr><td>{PlainTextBegin}123{PlainTextEnd}</td><td>{PlainTextBegin}{{ BA = 456 }}{PlainTextEnd}</td></tr></tbody></table>");
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
                      .Contain($"<td>{PlainTextBegin}System.Exception: not ok");
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
            public void It_can_format_a_String_with_class()
            {
                var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(string));

                var writer = new StringWriter();

                string instance = @"this
is a 
   multiline<>
string";

                formatter.Format(instance, writer);

                writer.ToString()
                      .Should()
                      .BeEquivalentHtmlTo(
                          $"{PlainTextBegin}{instance.HtmlEncode()}{PlainTextEnd}");
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
                          @$"
<table>
   <thead>
     <tr><th><i>index</i></th><th>TypeName</th><th>Id</th></tr>
   </thead>
   <tbody>
     <tr><td>0</td><td>{PlainTextBegin}entity one{PlainTextEnd}</td><td>{PlainTextBegin}123{PlainTextEnd}</td></tr>
     <tr><td>1</td><td>{PlainTextBegin}entity two{PlainTextEnd}</td><td>{PlainTextBegin}456{PlainTextEnd}</td></tr>
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
                          $"<table><thead><tr><th><i>key</i></th><th>TypeName</th><th>Id</th></tr></thead><tbody><tr><td>{PlainTextBegin}first{PlainTextEnd}</td><td>{PlainTextBegin}entity one{PlainTextEnd}</td><td>{PlainTextBegin}123{PlainTextEnd}</td></tr><tr><td>{PlainTextBegin}second{PlainTextEnd}</td><td>{PlainTextBegin}entity two{PlainTextEnd}</td><td>{PlainTextBegin}456{PlainTextEnd}</td></tr></tbody></table>");
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
      <tr><td>{PlainTextBegin}first{PlainTextEnd}</td><td>{PlainTextBegin}entity one{PlainTextEnd}</td><td>{PlainTextBegin}123{PlainTextEnd}</td></tr>
      <tr><td>{PlainTextBegin}second{PlainTextEnd}</td><td>{PlainTextBegin}entity two{PlainTextEnd}</td><td>{PlainTextBegin}456{PlainTextEnd}</td></tr>
    </tbody>
</table>");
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
            public void It_formats_string_with_encoding_and_preserving_whitespace_and_with_tags()
            {
                var text = "hello<b>world  </b>  \n\n  ";

                var html = text.ToDisplayString("text/html");

                html.Should().Be($"{PlainTextBegin}hello&lt;b&gt;world  &lt;/b&gt;  \n\n  {PlainTextEnd}");
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

                html.Should().BeEquivalentHtmlTo($"<table><thead><tr><th>Item1</th></tr></thead><tbody><tr><td>{PlainTextBegin}hello&lt;b&gt;world  &lt;/b&gt;  \n\n  {PlainTextEnd}</td></tr></tbody></table>");
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
    <tr><td>0</td><td>{PlainTextBegin}apple{PlainTextEnd}</td></tr>
    <tr><td>1</td><td>{PlainTextBegin}banana{PlainTextEnd}</td></tr>
    <tr><td>2</td><td>{PlainTextBegin}cherry{PlainTextEnd}</td></tr>
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
     <tr><td>0</td><td>{PlainTextBegin}kiwi{PlainTextEnd}</td></tr>
     <tr><td>1</td><td>{PlainTextBegin}apple{PlainTextEnd}</td></tr>
     <tr><td>2</td><td>{PlainTextBegin}plantain{PlainTextEnd}</td></tr>
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

            class SomeDictUsingInterfaceImpls : IDictionary<int, string>
            {
                string IDictionary<int, string>.this[int key] { get => "2"; set => throw new NotImplementedException(); }

                ICollection<int> IDictionary<int, string>.Keys => new int[] { 1 };

                ICollection<string> IDictionary<int, string>.Values => new string[] { "2" };

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
                    return ((IEnumerable < KeyValuePair<int, string> > ) new KeyValuePair<int, string>[] { new KeyValuePair<int, string>(1, "2") }).GetEnumerator();
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
                    return ((IEnumerable)new KeyValuePair<int, string>[] { new KeyValuePair<int, string>(1, "2") }).GetEnumerator();
                }
            }

            [Fact]
            public void Dictionary_with_non_string_keys_are_formatted_correctly()
            {
                var dict = new SomeDictUsingInterfaceImpls();

                var html = dict.ToDisplayString("text/html");

                html.Should().BeEquivalentHtmlTo(
                    $"<table><thead><tr><th><i>key</i></th><th>value</th></tr></thead><tbody><tr><td>{PlainTextBegin}1{PlainTextEnd}</td><td>{PlainTextBegin}2{PlainTextEnd}</td></tr></tbody></table>");
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
              <a href={"\"https://docs.microsoft.com/dotnet/api/system.boolean?view=netcore-3.0\""}>System.Boolean</a>
            </span>
          </td>
          <td>{PlainTextBegin}True{PlainTextEnd}</td>
        </tr>
        <tr>
          <td>1</td>
          <td>
            <span>
              <a href={"\"https://docs.microsoft.com/dotnet/api/system.int32?view=netcore-3.0\""}>System.Int32</a>
            </span>
          </td>
          <td>{PlainTextBegin}99{PlainTextEnd}</td>
        </tr>
        <tr>
          <td>2</td>
          <td>
            <span>
              <a href={"\"https://docs.microsoft.com/dotnet/api/system.string?view=netcore-3.0\""}>System.String</a>
            </span>
          </td>
          <td>{PlainTextBegin}Hello, World{PlainTextEnd}</td>
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
              <a href={"\"https://docs.microsoft.com/dotnet/api/system.valuetuple-2?view=netcore-3.0\""}>System.ValueTuple&lt;System.Int32,System.String&gt;</a>
            </span>
          </td>
          <td></td>
          <td>{PlainTextBegin}2{PlainTextEnd}</td>
          <td>{PlainTextBegin}two{PlainTextEnd}</td>
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
          <td>{PlainTextBegin}apple{PlainTextEnd}</td>
          <td>{PlainTextBegin}green{PlainTextEnd}</td>
        </tr>
      </tbody>
    </table>");
            }
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
          <td>{PlainTextBegin}apple{PlainTextEnd}</td>
          <td>{PlainTextBegin}green{PlainTextEnd}</td>
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
          <td>{PlainTextBegin}two{PlainTextEnd}</td>
          <td>{PlainTextBegin}2{PlainTextEnd}</td>
          <td></td>
        </tr>
        <tr>
          <td>2</td>
          <td>
            <span>
              <a href={"\"https://docs.microsoft.com/dotnet/api/system.int32?view=netcore-3.0\""}>System.Int32</a>
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
              <a href={"\"https://docs.microsoft.com/dotnet/api/system.linq.enumerable.rangeiterator?view=netcore-3.0\""}>System.Linq.Enumerable+RangeIterator</a>
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
    }
}
