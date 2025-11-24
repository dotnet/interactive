// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dummy;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Formatting.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Formatting.Tests;

public partial class HtmlFormatterTests
{
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

            var html = writer.ToString().RemoveStyleElement();

            html.Should()
                .BeEquivalentHtmlTo(
                    """
                        <table>
                            <thead>
                                <tr>
                                    <th><i>index</i></th>
                                    <th>value</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr>
                                    <td>0</td>
                                    <td>
                                        <details open="open" class="dni-treeview">
                                            <summary><span class="dni-code-hint"><code>Microsoft.DotNet.Interactive.Formatting.Tests.EntityId</code></span></summary>
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
                                                                    <pre>entity one</pre>
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
                                    </td>
                                </tr>
                                <tr>
                                    <td>1</td>
                                    <td>
                                        <details open="open" class="dni-treeview">
                                            <summary><span class="dni-code-hint"><code>Microsoft.DotNet.Interactive.Formatting.Tests.EntityId</code></span></summary>
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
                                                                    <pre>entity two</pre>
                                                                </div>
                                                            </td>
                                                        </tr>
                                                        <tr>
                                                            <td>Id</td>
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
                        """);
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
                  .Contain($"<td>{Tags.PlainTextBegin}{listOfArrays.First().ToDisplayString("text/plain")}{Tags.PlainTextEnd}</td>");
        }
            
        [Fact]
        public void It_formats_generic_dictionaries_with_tree_views_in_the_value_column()
        {
            var writer = new StringWriter();

            IDictionary<string, EntityId> instance = new GenericDictionary<string, EntityId>
            {
                { "first", new EntityId("entity one", "123") },
                { "second", new EntityId("entity two", "456") }
            };

            var formatter = HtmlFormatter.GetPreferredFormatterFor(instance.GetType());

            formatter.Format(instance, writer);

            var html = writer.ToString().RemoveStyleElement();

            html.Should()
                .BeEquivalentHtmlTo(
                    """
                        
                        <table>
                          <thead>
                            <tr>
                              <th>
                                <i>key</i>
                              </th>
                              <th>value</th>
                            </tr>
                          </thead>
                          <tbody>
                            <tr>
                              <td>
                                <div class="dni-plaintext">
                                  <pre>first</pre>
                                </div>
                              </td>
                              <td>
                                <details open="open" class="dni-treeview">
                                  <summary>
                                    <span class="dni-code-hint">
                                      <code>Microsoft.DotNet.Interactive.Formatting.Tests.EntityId</code>
                                    </span>
                                  </summary>
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
                                              <pre>entity one</pre>
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
                              </td>
                            </tr>
                            <tr>
                              <td>
                                <div class="dni-plaintext">
                                  <pre>second</pre>
                                </div>
                              </td>
                              <td>
                                <details open="open" class="dni-treeview">
                                  <summary>
                                    <span class="dni-code-hint">
                                      <code>Microsoft.DotNet.Interactive.Formatting.Tests.EntityId</code>
                                    </span>
                                  </summary>
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
                                              <pre>entity two</pre>
                                            </div>
                                          </td>
                                        </tr>
                                        <tr>
                                          <td>Id</td>
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
                        """);
        }

        [Fact]
        public void It_formats_non_generic_dictionaries_with_tree_views_in_the_value_column()
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
                  .RemoveStyleElement()
                  .Should()
                  .BeEquivalentHtmlTo(
                      """
                          <table>
                            <thead>
                              <tr>
                                <th>
                                  <i>key</i>
                                </th>
                                <th>value</th>
                              </tr>
                            </thead>
                            <tbody>
                              <tr>
                                <td>
                                  <div class="dni-plaintext">
                                    <pre>first</pre>
                                  </div>
                                </td>
                                <td>
                                  <details open="open" class="dni-treeview">
                                    <summary>
                                      <span class="dni-code-hint">
                                        <code>Microsoft.DotNet.Interactive.Formatting.Tests.EntityId</code>
                                      </span>
                                    </summary>
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
                                                <pre>entity one</pre>
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
                                </td>
                              </tr>
                              <tr>
                                <td>
                                  <div class="dni-plaintext">
                                    <pre>second</pre>
                                  </div>
                                </td>
                                <td>
                                  <details open="open" class="dni-treeview">
                                    <summary>
                                      <span class="dni-code-hint">
                                        <code>Microsoft.DotNet.Interactive.Formatting.Tests.EntityId</code>
                                      </span>
                                    </summary>
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
                                                <pre>entity two</pre>
                                              </div>
                                            </td>
                                          </tr>
                                          <tr>
                                            <td>Id</td>
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
                          """);
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

            var html = text.ToDisplayString("text/html").RemoveStyleElement();

            html.Should().Be($"{Tags.PlainTextBegin}hello&lt;b&gt;world  &lt;/b&gt;  \n\n  {Tags.PlainTextEnd}");
        }

        [Fact]
        public void It_formats_string_arrays_correctly()
        {
            var strings = new[] { "apple", "banana", "cherry" };

            strings.ToDisplayString("text/html")
                   .RemoveStyleElement()
                   .Should()
                   .BeEquivalentHtmlTo(
                       """
                           <div class="dni-plaintext">
                             <pre>[ apple, banana, cherry ]</pre>
                           </div>
                           """);
        }

        [Fact]
        public void It_formats_ordered_enumerables_correctly()
        {
            var sorted = new[]
                    { "kiwi", "plantain", "apple" }
                .OrderBy(fruit => fruit.Length);

            var html = sorted.ToDisplayString("text/html").RemoveStyleElement();

            html.Should()
                .BeEquivalentHtmlTo("""
                    <div class="dni-plaintext">
                      <pre>[ kiwi, apple, plantain ]</pre>
                    </div>
                    """);
        }

        [Fact]
        public void Empty_sequences_are_indicated()
        {
            var list = new List<string>();

            var html = list.ToDisplayString("text/html").RemoveStyleElement();

            html.Should().BeEquivalentHtmlTo(
                """
                <div class="dni-plaintext">
                  <pre>[  ]</pre>
                </div>
                """);
        }

        [Fact]
        public void Empty_dictionaries_are_indicated()
        {
            var list = new Dictionary<int, int>();

            var html = list.ToDisplayString("text/html").RemoveStyleElement();

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
            formatted.Should().Contain("(6 more)");
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
            formatted.Should().Contain("... (more)");

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

            var html = objects.ToDisplayString("text/html").RemoveStyleElement();

            html.Should().BeEquivalentHtmlTo(
                $"<table><thead><tr><th><i>index</i></th><th>value</th></tr></thead><tbody><tr><td>0</td><td><span>{date1.ToDisplayString("text/plain")}</span></td></tr><tr><td>1</td><td><span>{date2.ToDisplayString("text/plain")}</span></td></tr></tbody></table>");
        }

        [Fact]
        public void System_Type_is_not_destructured()
        {
            var objects = new object[] { typeof(string), typeof(int) };

            var html = objects.ToDisplayString("text/html").RemoveStyleElement();

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
              <a href={"\"https://docs.microsoft.com/dotnet/api/system.string?view=net-7.0\""}>System.String</a>
            </span>
          </td>
        </tr>
        <tr>
          <td>1</td>
          <td>
            <span>
              <a href={"\"https://docs.microsoft.com/dotnet/api/system.int32?view=net-7.0\""}>System.Int32</a>
            </span>
          </td>
        </tr>
      </tbody>
    </table>");
        }

        [Fact]
        public void Dictionary_with_non_string_keys_are_formatted_correctly()
        {
            var dict = new ClassImplementingIDictionary_of_int_string();

            var html = dict.ToDisplayString("text/html").RemoveStyleElement();

            html.Should().BeEquivalentHtmlTo(
                $"""
                    <table><thead><tr><th><i>key</i></th><th>value</th></tr></thead><tbody><tr><td>{Tags.PlainTextBegin}1{Tags.PlainTextEnd}</td><td>{Tags.PlainTextBegin}one{Tags.PlainTextEnd}</td></tr></tbody></table>
                    """);
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
                
            writer.ToString().RemoveStyleElement()
                  .Should()
                  .BeEquivalentHtmlTo(
                      """
                          <div class="dni-plaintext">
                            <pre>[ 7, 8, 9 ]</pre>
                          </div>
                          """);
        }

        [Fact]
        public void It_shows_null_items_in_the_sequence_as_null()
        {
            var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(object[]));

            var writer = new StringWriter();

            formatter.Format(new object[] { 8, null, 9 }, writer);

            writer.ToString().RemoveStyleElement().Should()
                  .BeEquivalentHtmlTo(
                      $@"
<table>
  <thead>
    <tr><th><i>index</i></th><th>value</th></tr>
  </thead>
  <tbody>
    <tr><td>0</td><td>{Tags.PlainTextBegin}8{Tags.PlainTextEnd}</td></tr>
    <tr><td>1</td><td>{Tags.PlainTextBegin}&lt;null&gt;{Tags.PlainTextEnd}</td></tr>
    <tr><td>2</td><td>{Tags.PlainTextBegin}9{Tags.PlainTextEnd}</td></tr>
  </tbody>
</table>");
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

            writer.ToString().RemoveStyleElement().Should()
                  .BeEquivalentHtmlTo($"""
                      <table>
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
                                <a href="https://docs.microsoft.com/dotnet/api/system.boolean?view=net-7.0">System.Boolean</a>
                              </span>
                            </td>
                            <td>
                              <div class="dni-plaintext">
                                <pre>True</pre>
                              </div>
                            </td>
                          </tr>
                          <tr>
                            <td>1</td>
                            <td>
                              <span>
                                <a href="https://docs.microsoft.com/dotnet/api/system.int32?view=net-7.0">System.Int32</a>
                              </span>
                            </td>
                            <td>
                              <div class="dni-plaintext">
                                <pre>99</pre>
                              </div>
                            </td>
                          </tr>
                          <tr>
                            <td>2</td>
                            <td>
                              <span>
                                <a href="https://docs.microsoft.com/dotnet/api/system.string?view=net-7.0">System.String</a>
                              </span>
                            </td>
                            <td>
                              <div class="dni-plaintext">
                                <pre>Hello, World</pre>
                              </div>
                            </td>
                          </tr>
                        </tbody>
                      </table>
                      """);
        }
            
        [Fact]
        public void All_element_properties_are_shown_when_sequences_contain_different_types()
        {
            var objects = new object[]
            {
                1,
                (2, "two"),
                Enumerable.Range(1, 3),
                new { name = "apple", color = "green" }
            };

            var result = objects.ToDisplayString("text/html").RemoveStyleElement();

            result
                .Should()
                .BeEquivalentHtmlTo(
                    """
                        <table>
                            <thead>
                                <tr>
                                    <th><i>index</i></th>
                                    <th><i>type</i></th>
                                    <th>value</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr>
                                    <td>0</td>
                                    <td><span><a href="https://docs.microsoft.com/dotnet/api/system.int32?view=net-7.0">System.Int32</a></span>
                                    </td>
                                    <td>
                                        <div class="dni-plaintext">
                                            <pre>1</pre>
                                        </div>
                                    </td>
                                </tr>
                                <tr>
                                    <td>1</td>
                                    <td><span><a
                                                href="https://docs.microsoft.com/dotnet/api/system.valuetuple-2?view=net-7.0">System.ValueTuple&lt;System.Int32,System.String&gt;</a></span>
                                    </td>
                                    <td>
                                        <details class="dni-treeview">
                                            <summary><span class="dni-code-hint"><code>(2, two)</code></span></summary>
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
                                                                    <pre>2</pre>
                                                                </div>
                                                            </td>
                                                        </tr>
                                                        <tr>
                                                            <td>Item2</td>
                                                            <td>
                                                                <div class="dni-plaintext">
                                                                    <pre>two</pre>
                                                                </div>
                                                            </td>
                                                        </tr>
                                                    </tbody>
                                                </table>
                                            </div>
                                        </details>
                                    </td>
                                </tr>
                                <tr>
                                    <td>2</td>
                                    <td><span><a href="https://docs.microsoft.com/dotnet/api/system.linq.enumerable.rangeiterator-1?view=net-7.0">System.Linq.Enumerable+RangeIterator&lt;System.Int32&gt;</a></span>
                                    </td>
                                    <td>
                                        <div class="dni-plaintext">
                                            <pre>[ 1, 2, 3 ]</pre>
                                        </div>
                                    </td>
                                </tr>
                                <tr>
                                    <td>3</td>
                                    <td>(anonymous)</td>
                                    <td>
                                        <details class="dni-treeview">
                                            <summary><span class="dni-code-hint"><code>{ name = apple, color = green }</code></span></summary>
                                            <div>
                                                <table>
                                                    <thead>
                                                        <tr></tr>
                                                    </thead>
                                                    <tbody>
                                                        <tr>
                                                            <td>name</td>
                                                            <td>
                                                                <div class="dni-plaintext">
                                                                    <pre>apple</pre>
                                                                </div>
                                                            </td>
                                                        </tr>
                                                        <tr>
                                                            <td>color</td>
                                                            <td>
                                                                <div class="dni-plaintext">
                                                                    <pre>green</pre>
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
                        """);
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

            var html = objects.ToDisplayString("text/html").RemoveStyleElement();

            html.Should()
                .BeEquivalentHtmlTo(
                    """
                        <table>
                            <thead>
                                <tr>
                                    <th><i>index</i></th>
                                    <th><i>type</i></th>
                                    <th>value</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr>
                                    <td>0</td>
                                    <td>(anonymous)</td>
                                    <td><i>{ name = apple, color = green }</i></td>
                                </tr>
                                <tr>
                                    <td>1</td>
                                    <td><span><a
                                                href="https://docs.microsoft.com/dotnet/api/system.valuetuple-2?view=net-7.0">System.ValueTuple&lt;System.Int32,System.String&gt;</a></span>
                                    </td>
                                    <td>
                                        <details class="dni-treeview">
                                            <summary><span class="dni-code-hint"><code>(123, two)</code></span></summary>
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
                                                                    <pre>two</pre>
                                                                </div>
                                                            </td>
                                                        </tr>
                                                    </tbody>
                                                </table>
                                            </div>
                                        </details>
                                    </td>
                                </tr>
                                <tr>
                                    <td>2</td>
                                    <td><span><a href="https://docs.microsoft.com/dotnet/api/system.int32?view=net-7.0">System.Int32</a></span>
                                    </td>
                                    <td>
                                        <div class="dni-plaintext">
                                            <pre>456</pre>
                                        </div>
                                    </td>
                                </tr>
                                <tr>
                                    <td>3</td>
                                    <td><span><a
                                                href="https://docs.microsoft.com/dotnet/api/system.int32[]?view=net-7.0">System.Int32[]</a></span>
                                    </td>
                                    <td>
                                        <div class="dni-plaintext">
                                            <pre>[ 7, 8, 9 ]</pre>
                                        </div>
                                    </td>
                                </tr>
                            </tbody>
                        </table>
                        """);
        }

        [Fact]
        public void When_an_IEnumerable_type_has_properties_it_shows_both_properties_and_elements()
        {
            var instance = new ClassWithPropertiesThatIsAlsoIEnumerable(new[] { "apple", "banana" })
            {
                Property = "cherry"
            };

            var html = instance.ToDisplayString("text/html").RemoveStyleElement();

            html.Should().BeEquivalentHtmlTo("""
                <details open="open" class="dni-treeview">
                  <summary>
                    <span class="dni-code-hint">
                      <code>[ apple, banana ]</code>
                    </span>
                  </summary>
                  <div>
                    <table>
                      <thead>
                        <tr></tr>
                      </thead>
                      <tbody>
                        <tr>
                          <td>Property</td>
                          <td>
                            <div class="dni-plaintext">
                              <pre>cherry</pre>
                            </div>
                          </td>
                        </tr>
                        <tr>
                          <td>
                            <i>(values)</i>
                          </td>
                          <td>
                            <table>
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
                                    <div class="dni-plaintext">
                                      <pre>apple</pre>
                                    </div>
                                  </td>
                                </tr>
                                <tr>
                                  <td>1</td>
                                  <td>
                                    <div class="dni-plaintext">
                                      <pre>banana</pre>
                                    </div>
                                  </td>
                                </tr>
                              </tbody>
                            </table>
                          </td>
                        </tr>
                      </tbody>
                    </table>
                  </div>
                </details>
                """);
        }

        [Fact]
        public void When_an_IEnumerable_T_type_has_properties_it_shows_both_properties_and_elements()
        {
            var instance = new ClassWithPropertiesThatIsAlsoIEnumerable<string>(new[] { "apple", "banana" })
            {
                Property = "cherry"
            };

            var html = instance.ToDisplayString("text/html").RemoveStyleElement();

            html.Should().BeEquivalentHtmlTo("""
                <details open="open" class="dni-treeview">
                  <summary>
                    <span class="dni-code-hint">
                      <code>[ apple, banana ]</code>
                    </span>
                  </summary>
                  <div>
                    <table>
                      <thead>
                        <tr></tr>
                      </thead>
                      <tbody>
                        <tr>
                          <td>Property</td>
                          <td>
                            <div class="dni-plaintext">
                              <pre>cherry</pre>
                            </div>
                          </td>
                        </tr>
                        <tr>
                          <td>
                            <i>(values)</i>
                          </td>
                          <td>
                            <div class="dni-plaintext">
                              <pre>[ apple, banana ]</pre>
                            </div>
                          </td>
                        </tr>
                      </tbody>
                    </table>
                  </div>
                </details>
                """);
        }
    }
}