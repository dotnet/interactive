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
        [Fact]
        public void Non_generic_Create_creates_generic_formatter()
        {
            HtmlFormatter.Create(typeof(Widget))
                         .Should()
                         .BeOfType<HtmlFormatter<Widget>>();
        }

        public class Objects : FormatterTestBase
        {
            [Fact]
            public void Formatters_are_generated_on_the_fly_when_HTML_mime_type_is_requested()
            {
                var output = new { a = 123 }.ToDisplayString(HtmlFormatter.MimeType);

                output.Should().StartWith("<table>");
            }

            [Fact]
            public void Formatter_does_not_expand_string()
            {
                var formatter = HtmlFormatter<string>.Create();

                var s = "hello".ToDisplayString(formatter);

                s.Should().Be("<span>hello</span>");
            }

            [Fact]
            public void Formatter_expands_properties_of_ExpandoObjects()
            {
                dynamic expando = new ExpandoObject();
                expando.Name = "socks";
                expando.Count = 2;

                var formatter = HtmlFormatter<ExpandoObject>.Create();

                var output = ((object) expando).ToDisplayString(formatter);

                output.Should().Be("<table><thead><tr><th>Count</th><th>Name</th></tr></thead><tbody><tr><td>2</td><td>socks</td></tr></tbody></table>");
            }

            [Fact]
            public void It_formats_objects_as_tables_having_properties_on_the_y_axis()
            {
                var formatter = HtmlFormatter.Create(typeof(EntityId));

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

                var formatter = HtmlFormatter.Create(instance.GetType());

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

                var formatter = HtmlFormatter.Create(instance.GetType());

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

                var formatter = HtmlFormatter.Create(instance.GetType());

                formatter.Format(instance, writer);

                writer.ToString()
                      .Should()
                      .Contain("<table><thead><tr><th>A</th><th>B</th></tr></thead><tbody><tr><td>123</td><td>{ BA: 456 }</td></tr></tbody></table>");
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

                var formatter = HtmlFormatter.Create(instance.GetType());

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

                var formatter = HtmlFormatter.Create(instance.GetType());

                formatter.Format(instance, writer);

                writer.ToString()
                      .Should()
                      .Contain("[ 1, 2, 3 ]");
            }

            [Fact]
            public void It_displays_exceptions_thrown_by_properties_in_the_property_value_cell()
            {
                var formatter = HtmlFormatter.Create(typeof(SomePropertyThrows));

                var writer = new StringWriter();

                var widget = new SomePropertyThrows();

                formatter.Format(widget, writer);

                writer.ToString()
                      .Should()
                      .Contain("<td>{ System.Exception:");
            }

            [Fact]
            public void Properies_of_System_Type_instances_are_not_expanded()
            {
                var formatter = HtmlFormatter.Create(typeof(Type));

                var writer = new StringWriter();

                formatter.Format(typeof(string), writer);

                writer.ToString()
                      .Should()
                      .Be("<span><a href=\"https://docs.microsoft.com/dotnet/api/System.String?view=netcore-3.0\">System.String</a></span>");
            }

            
            [Fact]
            public void Enums_are_formatted_using_their_names()
            {
                var formatter = HtmlFormatter.Create(typeof(FileAccess));

                var writer = new StringWriter();

                formatter.Format(FileAccess.ReadWrite, writer);

                writer.ToString().Should().Contain("ReadWrite");
            }

            [Fact]
            public void TimeSpan_is_not_destructured()
            {
                var formatter = HtmlFormatter.Create(typeof(TimeSpan));

                var writer = new StringWriter();

                var timespan = 25.Milliseconds();

                formatter.Format(timespan, writer);

                writer.ToString().Should().Contain(timespan.ToString());
            }
        }

        public class Sequences : FormatterTestBase
        {
            [Fact]
            public void It_formats_sequences_as_tables_with_an_index_on_the_y_axis()
            {
                var formatter = HtmlFormatter.Create(typeof(List<EntityId>));

                var writer = new StringWriter();

                var instance = new List<EntityId>
                {
                    new EntityId("entity one", "123"),
                    new EntityId("entity two", "456")
                };

                formatter.Format(instance, writer);

                writer.ToString()
                      .Should()
                      .Be(
                          "<table><thead><tr><th><i>index</i></th><th>TypeName</th><th>Id</th></tr></thead><tbody><tr><td>0</td><td>entity one</td><td>123</td></tr><tr><td>1</td><td>entity two</td><td>456</td></tr></tbody></table>");
            }

            [Fact]
            public void It_formats_sequence_properties_using_plain_text_formatting()
            {
                var formatter = HtmlFormatter.Create(typeof(List<float[]>));

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

                var formatter = HtmlFormatter.Create(instance.GetType());

                formatter.Format(instance, writer);

                writer.ToString()
                      .Should()
                      .Be(
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

                var formatter = HtmlFormatter.Create(instance.GetType());

                formatter.Format(instance, writer);

                writer.ToString()
                      .Should()
                      .Be(
                          "<table><thead><tr><th><i>key</i></th><th>TypeName</th><th>Id</th></tr></thead><tbody><tr><td>first</td><td>entity one</td><td>123</td></tr><tr><td>second</td><td>entity two</td><td>456</td></tr></tbody></table>");
            }

            [Fact]
            public void It_formats_string_arrays_correctly()
            {
                var strings = new[] { "apple", "banana", "cherry" };

                strings.ToDisplayString("text/html")
                       .Should()
                       .Be(
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
                    .Be(
                        "<table><thead><tr><th><i>index</i></th><th>value</th></tr></thead><tbody><tr><td>0</td><td>kiwi</td></tr><tr><td>1</td><td>apple</td></tr><tr><td>2</td><td>plantain</td></tr></tbody></table>");
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

                var formatter = HtmlFormatter.Create(list.GetType());

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

                var formatter = HtmlFormatter.Create(list.GetType());

                var formatted = list.ToDisplayString(formatter);

                formatted.Contains("number 1").Should().BeTrue();
                formatted.Contains("number 4").Should().BeTrue();
                formatted.Should().NotContain("number 5");
                formatted.Contains("6 more").Should().BeTrue();
            }

            [Fact(Skip = "wip")]
            public void It_formats_arrays_of_disparate_types_correctly()
            {
                var objects = new object[] { 1, (2, "two"), Enumerable.Range(1, 3) };

                var obj0 = objects[0].ToDisplayString();
                var obj1 = objects[1].ToDisplayString();
                var obj2 = objects[2].ToDisplayString();

                objects.ToDisplayString("text/html")
                       .Should()
                       .Be(
                           $"<table><thead><tr><th><i>index</i></th><th>value</th></tr></thead><tbody><tr><td>0</td><td>{obj0}</td></tr><tr><td>1</td><td>{obj1}</td></tr><tr><td>2</td><td>{obj2}</td></tr></tbody></table>");
            }

            [Fact]
            public void ReadOnlyMemory_of_char_is_formatted_like_a_string()
            {
                var formatter = HtmlFormatter<ReadOnlyMemory<char>>.Create();

                var writer = new StringWriter();

                var readOnlyMemory = "Hi!".AsMemory();

                formatter.Format(readOnlyMemory, writer);

                writer.ToString()
                      .Should()
                      .Be("<span>Hi!</span>");
            }

            [Fact]
            public void ReadOnlyMemory_of_int_is_formatted_like_a_int_array()
            {
                var formatter = HtmlFormatter<ReadOnlyMemory<int>>.Create();

                var writer = new StringWriter();

                var readOnlyMemory = new ReadOnlyMemory<int>(new[] { 7, 8, 9 });

                formatter.Format(readOnlyMemory, writer);

                writer.ToString()
                      .Should()
                      .Be(
                          "<table><thead><tr><th><i>index</i></th><th>value</th></tr></thead><tbody><tr><td>0</td><td>7</td></tr><tr><td>1</td><td>8</td></tr><tr><td>2</td><td>9</td></tr></tbody></table>");
            }

            [Fact]
            public void It_shows_null_items_in_the_sequence_as_null()
            {
                var formatter = HtmlFormatter.Create(typeof(object[]));

                var writer = new StringWriter();

                formatter.Format(new object[] { 8, null, 9 }, writer);

                writer.ToString().Should()
                      .Be(
                          "<table><thead><tr><th><i>index</i></th><th>value</th></tr></thead><tbody><tr><td>0</td><td>8</td></tr><tr><td>1</td><td>&lt;null&gt;</td></tr><tr><td>2</td><td>9</td></tr></tbody></table>");
            }

            [Fact]
            public void Sequences_contain_different_types_of_elements()
            {
                IEnumerable<object> GetCollection()
                {
                    yield return true;
                    yield return 99;
                    yield return "Hello, World";
                }

                var formatter = HtmlFormatter.Create(typeof(IEnumerable<object>));

                var writer = new StringWriter();

                formatter.Format(GetCollection(), writer);

                writer.ToString().Should()
                      .Be(
                          "<table><thead><tr><th><i>index</i></th><th>value</th></tr></thead><tbody><tr><td>0</td><td>True</td></tr><tr><td>1</td><td>99</td></tr><tr><td>2</td><td>Hello, World</td></tr></tbody></table>");
            }


        }
    }
}
