// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Dummy;
using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.Interactive.Formatting.Tests;

public partial class PlainTextFormatterTests : FormatterTestBase
{
    public class Sequences : FormatterTestBase
    {
        [Fact]
        public void Formatter_truncates_expansion_of_ICollection()
        {
            var list = new List<string>();
            for (var i = 1; i < 11; i++)
            {
                list.Add("number " + i);
            }

            Formatter.ListExpansionLimit = 4;

            var formatter = PlainTextFormatter.GetPreferredFormatterFor(typeof(ICollection));

            var formatted = list.ToDisplayString(formatter);

            formatted.Contains("number 1").Should().BeTrue();
            formatted.Contains("number 4").Should().BeTrue();
            formatted.Should().NotContain("number 5");
            formatted.Should().Contain("6 more");
        }

        [Fact]
        public void Formatter_expands_IEnumerable()
        {
            var list = new List<string> { "this", "that", "the other thing" };

            var formatter = PlainTextFormatter.GetPreferredFormatterFor(list.GetType());

            var formatted = list.ToDisplayString(formatter);

            formatted.Should()
                     .Be("[ this, that, the other thing ]");
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

            var formatter = PlainTextFormatter.GetPreferredFormatterFor(list.GetType());

            var formatted = list.ToDisplayString(formatter);

            formatted.Should().Contain("number 1");
            formatted.Should().Contain("number 4");
            formatted.Should().NotContain("number 5");
            formatted.Should().Contain("6 more");
        }

        [Fact]
        public void Formatter_truncates_expansion_of_IEnumerable()
        {
            Formatter.ListExpansionLimit = 3;

            var formatter = PlainTextFormatter.GetPreferredFormatterFor<IEnumerable<int>>();

            var formatted = InfiniteSequence().ToDisplayString(formatter);

            formatted.Should().Contain("[ number 9, number 9, number 9 ... (more) ]");

            static IEnumerable<string> InfiniteSequence()
            {
                while (true)
                {
                    yield return "number 9";
                }
            }
        }

        [Fact]
        public void Formatter_iterates_IEnumerable_property_when_its_actual_type_is_an_array_of_objects()
        {
            var node = new Node
            {
                Id = "1",
                Nodes =
                    new[]
                    {
                        new Node { Id = "1.1" },
                        new Node { Id = "1.2" },
                        new Node { Id = "1.3" },
                    }
            };

            var formatter = PlainTextFormatter.GetPreferredFormatterFor<Node>();

            var output = node.ToDisplayString(formatter);

            output.Should().Contain("1.1");
            output.Should().Contain("1.2");
            output.Should().Contain("1.3");
        }

        [Fact]
        public void Formatter_iterates_IEnumerable_property_when_its_actual_type_is_an_array_of_structs()
        {
            var ints = new[] { 1, 2, 3, 4, 5 };

            var formatter = PlainTextFormatter.GetPreferredFormatterFor(ints.GetType());

            ints.ToDisplayString(formatter)
                .Should()
                .Be("[ 1, 2, 3, 4, 5 ]");
        }

        [Fact]
        public void Formatter_recursively_formats_types_within_IEnumerable()
        {
            var list = new List<Widget>
            {
                new() { Name = "widget x" },
                new() { Name = "widget y" },
                new() { Name = "widget z" }
            };

            var formatter = PlainTextFormatter.GetPreferredFormatterFor<List<Widget>>();

            var formatted = list.ToDisplayString(formatter);

            formatted.Should().Be(@"List<Widget>
    - Name: widget x
      Parts: <null>
    - Name: widget y
      Parts: <null>
    - Name: widget z
      Parts: <null>".ReplaceLineEndings());

            /* or ... ?
TheWidgets: Widget[]
├─ Name: widget x
│  Parts: <null>
├─ Name: widget y
│  Parts: <null>
└─ Name: widget z
   Parts: <null>
             */
        }

        [Fact]
        public void Properties_of_System_Type_instances_are_not_expanded()
        {
            var formatter = PlainTextFormatter.GetPreferredFormatterFor(typeof(Type));

            var writer = new StringWriter();

            formatter.Format(typeof(string), writer);

            writer.ToString()
                  .Should()
                  .Be("System.String");
        }

        [Fact]
        public void ReadOnlyMemory_of_char_is_formatted_like_a_string()
        {
            var formatter = PlainTextFormatter.GetPreferredFormatterFor(typeof(ReadOnlyMemory<char>));

            ReadOnlyMemory<char> readOnlyMemory = "Hi!".AsMemory();

            var writer = new StringWriter();

            formatter.Format(readOnlyMemory, writer);

            writer.ToString().Should().Be("Hi!");
        }

        [Fact]
        public void ReadOnlyMemory_of_int_is_formatted_like_a_int_array()
        {
            var formatter = PlainTextFormatter.GetPreferredFormatterFor(typeof(ReadOnlyMemory<int>));

            var readOnlyMemory = new ReadOnlyMemory<int>(new[] { 1, 2, 3 });

            var writer = new StringWriter();

            formatter.Format(readOnlyMemory, writer);

            writer.ToString().Should().Be("[ 1, 2, 3 ]");
        }

        [Fact]
        public void It_shows_null_items_in_the_sequence_as_null()
        {
            var formatter = PlainTextFormatter.GetPreferredFormatterFor(typeof(object[]));

            var writer = new StringWriter();

            formatter.Format(new object[] { 1, null, 3 }, writer);

            writer.ToString().Should().Be(@"Object[]
  - 1
  - <null>
  - 3".ReplaceLineEndings());
        }

        [Fact]
        public void Strings_with_escaped_sequences_are_preserved()
        {
            var formatter = PlainTextFormatter.GetPreferredFormatterFor(typeof(string));

            var value = "hola! \n \t \" \" ' ' the joy of escapes! and    white  space  ";

            var writer = new StringWriter();

            formatter.Format(value, writer);

            writer.ToString().Should().Be(value);
        }

        [Fact]
        public void Sequences_in_an_object_property_are_indented()
        {
            var node = new Node("1");
            var node1_2 = new Node("1.2");
            node1_2.Nodes = new Node[]
            {
                new("1.2.1"),
            };
            node.Nodes = new Node[]
            {
                new("1.1"),
                node1_2,
                new("1.3")
            };

            var formatter = PlainTextFormatter.GetPreferredFormatterFor(node.GetType());

            var writer = new StringWriter();
            formatter.Format(node, writer);

            writer.ToString().Should().Contain(
            """
                Node
                    Id: 1
                    Nodes: Node[]
                      - Id: 1.1
                        Nodes: <null>
                      - Id: 1.2
                        Nodes: Node[]
                          - Id: 1.2.1
                            Nodes: <null>
                      - Id: 1.3
                        Nodes: <null>
                """.ReplaceLineEndings());
        }

        [Fact]
        public void When_an_IEnumerable_type_has_properties_it_shows_both_properties_and_elements()
        {
            var instance = new ClassWithPropertiesThatIsAlsoIEnumerable(new[] { "apple", "banana" })
            {
                Property = "cherry"
            };



            // FIX (When_an_IEnumerable_type_has_properties_it_shows_both_properties_and_elements) write test
            throw new NotImplementedException();
        }

        [Fact]
        public void When_an_IEnumerable_T_type_has_properties_it_shows_both_properties_and_elements()
        {
            var instance = new ClassWithPropertiesThatIsAlsoIEnumerable<string>(new[] { "durian", "elderberry" })
            {
                Property = "fig"
            };

            // FIX (When_an_IEnumerable_T_type_has_properties_it_shows_both_properties_and_elements) write test
            throw new NotImplementedException();
        }
    }
}