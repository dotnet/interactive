// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using FluentAssertions;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.DotNet.Interactive.Formatting.Tests
{
    public class FormatterTests : FormatterTestBase
    {
        public class Defaults : FormatterTestBase
        {
            [Fact]
            public void Default_formatter_for_Type_displays_generic_parameter_name_for_single_parameter_generic_type()
            {
                typeof(List<string>).ToDisplayString()
                                    .Should().Be("System.Collections.Generic.List<System.String>");
                new List<string>().GetType().ToDisplayString()
                                  .Should().Be("System.Collections.Generic.List<System.String>");
            }

            [Fact]
            public void Default_formatter_for_Type_displays_generic_parameter_name_for_multiple_parameter_generic_type()
            {
                typeof(Dictionary<string, IEnumerable<int>>).ToDisplayString()
                                                            .Should().Be(
                                                                "System.Collections.Generic.Dictionary<System.String,System.Collections.Generic.IEnumerable<System.Int32>>");
            }

            [Fact]
            public void Default_formatter_for_Type_displays_generic_parameter_names_for_open_generic_types()
            {
                typeof(IList<>).ToDisplayString()
                               .Should().Be("System.Collections.Generic.IList<T>");
                typeof(IDictionary<,>).ToDisplayString()
                                      .Should().Be("System.Collections.Generic.IDictionary<TKey,TValue>");
            }

            [Fact]
            public void Custom_formatter_for_Type_can_be_registered()
            {
                Formatter<Type>.Register(t => t.GUID.ToString());

                GetType().ToDisplayString()
                         .Should().Be(GetType().GUID.ToString());
            }

            [Fact]
            public void Default_formatter_for_null_Nullable_indicates_null()
            {
                int? nullable = null;

                var output = nullable.ToDisplayString();

                output.Should().Be(((object) null).ToDisplayString());
            }

            [Fact]
            public void Exceptions_always_get_properties_formatters()
            {
                var exception = new ReflectionTypeLoadException(
                    new[]
                    {
                        typeof(FileStyleUriParser),
                        typeof(AssemblyKeyFileAttribute)
                    },
                    new Exception[]
                    {
                        new DataMisalignedException()
                    });

                var message = exception.ToDisplayString();

                message.Should().Contain(nameof(DataMisalignedException.Data));
                message.Should().Contain(nameof(DataMisalignedException.HResult));
                message.Should().Contain(nameof(DataMisalignedException.StackTrace));
            }

            [Fact]
            public void Exception_Data_is_included_by_default()
            {
                var ex = new InvalidOperationException("oh noes!", new NullReferenceException());
                var key = "a very important int";
                ex.Data[key] = 123456;

                var msg = ex.ToDisplayString();

                msg.Should().Contain(key);
                msg.Should().Contain("123456");
            }

            [Fact]
            public void Exception_StackTrace_is_included_by_default()
            {
                string msg;
                var ex = new InvalidOperationException("oh noes!", new NullReferenceException());

                try
                {
                    throw ex;
                }
                catch (Exception thrownException)
                {
                    msg = thrownException.ToDisplayString();
                }

                msg.Should()
                   .Contain($"StackTrace:    at {typeof(FormatterTests)}.{nameof(Defaults)}.{MethodInfo.GetCurrentMethod().Name}");
            }

            [Fact]
            public void Exception_Type_is_included_by_default()
            {
                var ex = new InvalidOperationException("oh noes!", new NullReferenceException());

                var msg = ex.ToDisplayString();

                msg.Should().Contain("InvalidOperationException");
            }

            [Fact]
            public void Exception_Message_is_included_by_default()
            {
                var ex = new InvalidOperationException("oh noes!", new NullReferenceException());

                var msg = ex.ToDisplayString();

                msg.Should().Contain("oh noes!");
            }

            [Fact]
            public void Exception_InnerExceptions_are_included_by_default()
            {
                var ex = new InvalidOperationException("oh noes!", new NullReferenceException("oh my.", new DataException("oops!")));

                ex.ToDisplayString()
                  .Should()
                  .Contain("NullReferenceException");
                ex.ToDisplayString()
                  .Should()
                  .Contain("DataException");
            }

            [Fact]
            public void When_ResetToDefault_is_called_then_default_formatters_are_immediately_reregistered()
            {
                var widget = new Widget { Name = "hola!" };

                var defaultValue = widget.ToDisplayString();

                Formatter<Widget>.Register(e => "hello!");

                widget.ToDisplayString().Should().NotBe(defaultValue);

                Formatter.ResetToDefault();

                widget.ToDisplayString().Should().Be(defaultValue);
            }

            [Fact]
            public void When_JObject_is_formatted_it_outputs_its_string_representation()
            {
                JObject jObject = JObject.Parse(JsonConvert.SerializeObject(new
                {
                    SomeString = "hello",
                    SomeInt = 123
                }));

                var output = jObject.ToDisplayString();

                output.Should().Be(jObject.ToString());
            }
        }

        [Fact]
        public void ToDisplayString_uses_actual_type_formatter_and_not_compiled_type()
        {
            Widget widget = new InheritedWidget();
            bool widgetFormatterCalled = false;
            bool inheritedWidgetFormatterCalled = false;

            Formatter<Widget>.Register(w =>
            {
                widgetFormatterCalled = true;
                return "";
            });
            Formatter<InheritedWidget>.Register(w =>
            {
                inheritedWidgetFormatterCalled = true;
                return "";
            });

            widget.ToDisplayString();

            widgetFormatterCalled.Should().BeFalse();
            inheritedWidgetFormatterCalled.Should().BeTrue();
        }

        [Theory]
        [InlineData("text/html")]
        [InlineData("text/plain")]
        [InlineData("text/markdown")]
        [InlineData("application/json")]
        public void When_input_is_a_string_then_it_is_not_modified(string mimeType)
        {
            var input = "# { This is the <input> }";

            var output = input.ToDisplayString(mimeType);

            output.Should().Be(input);
        }

        [Theory]
        [InlineData("text/plain", false)]
        [InlineData("text/plain", true)]
        [InlineData("text/html", false)]
        [InlineData("text/html", true)]
        public void Formatters_can_be_registered_for_concrete_types(string mimeType, bool useGenericRegisterMethod)
        {
            if (useGenericRegisterMethod)
            {
                Formatter.Register<FileInfo>(
                    formatter: (filInfo, writer) => writer.Write("hello"),
                    mimeType);
            }
            else
            {
                Formatter.Register(
                    type: typeof(FileInfo),
                    formatter: (filInfo, writer) => writer.Write("hello"),
                    mimeType);
            }

            new FileInfo(@"c:\temp\foo.txt").ToDisplayString(mimeType)
                                            .Should()
                                            .Be("hello");
        }

        [Theory]
        [InlineData("text/plain", false)]
        [InlineData("text/plain", true)]
        [InlineData("text/html", false)]
        [InlineData("text/html", true)]
        public void Formatters_can_be_registered_on_demand_for_non_generic_interfaces(string mimeType, bool useGenericRegisterMethod)
        {
            if (useGenericRegisterMethod)
            {
                Formatter.Register<IEnumerable>(
                    formatter: (obj, writer) =>
                    {
                        var i = 0;
                        foreach (var item in obj)
                        {
                            i++;
                        }

                        writer.Write(i);
                    }, mimeType);
            }
            else
            {
                Formatter.Register(
                    type: typeof(IEnumerable),
                    formatter: (obj, writer) =>
                    {
                        var i = 0;
                        foreach (var item in (IEnumerable) obj)
                        {
                            i++;
                        }

                        writer.Write(i);
                    }, mimeType);
            }

            var list = new ArrayList { 1, 2, 3, 4, 5 };

            list.ToDisplayString(mimeType)
                .Should()
                .Be(list.Count.ToString());
        }

        [Theory]
        [InlineData("text/plain", false)]
        [InlineData("text/plain", true)]
        [InlineData("text/html", false)]
        [InlineData("text/html", true)]
        public void Formatters_can_be_registered_on_demand_for_abstract_classes(string mimeType, bool useGenericRegisterMethod)
        {
            if (useGenericRegisterMethod)
            {
                Formatter.Register<AbstractClass>(
                    (obj, writer) => writer.Write(obj.GetType().Name.ToUpperInvariant()), mimeType);
            }
            else
            {
                Formatter.Register(
                    type: typeof(AbstractClass),
                    formatter: (obj, writer) => writer.Write(obj.GetType().Name.ToUpperInvariant()), mimeType);
            }

            var instanceOf1 = new ConcreteClass1InheritingAbstractClass();

            instanceOf1.ToDisplayString(mimeType)
                       .Should()
                       .Be(instanceOf1.GetType().Name.ToUpperInvariant());

            var instanceOf2 = new ConcreteClass2InheritingAbstractClass();

            instanceOf2.ToDisplayString(mimeType)
                       .Should()
                       .Be(instanceOf2.GetType().Name.ToUpperInvariant());
        }

        public abstract class AbstractClass
        {
        };

        public class ConcreteClass1InheritingAbstractClass : AbstractClass
        {
        };

        public class ConcreteClass2InheritingAbstractClass : AbstractClass
        {
        };

        [Theory]
        [InlineData("text/plain")]
        [InlineData("text/html")]
        public void Formatters_can_be_registered_on_demand_for_open_generic_classes(string mimeType)
        {
            Formatter.Register(
                type: typeof(List<>),
                formatter: (obj, writer) =>
                {
                    var i = 0;
                    foreach (var item in (IEnumerable) obj)
                    {
                        i++;
                    }

                    writer.Write(i);
                }, mimeType);

            var list = new List<int> { 1, 2, 3, 4, 5 };

            list.ToDisplayString(mimeType)
                .Should()
                .Be(list.Count.ToString());
        }

        [Theory]
        [InlineData("text/plain")]
        [InlineData("text/html")]
        public void Formatters_can_be_registered_on_demand_for_open_generic_interfaces(string mimeType)
        {
            Formatter.Register(
                type: typeof(IList<>),
                formatter: (obj, writer) =>
                {
                    var i = 0;
                    foreach (var _ in (IEnumerable) obj)
                    {
                        i++;
                    }

                    writer.Write(i);
                }, mimeType);

            var list = new List<int> { 1, 2, 3, 4, 5 };

            list.ToDisplayString(mimeType)
                .Should()
                .Be(list.Count.ToString());
        }

        [Fact]
        public void When_JArray_is_formatted_it_outputs_its_string_representation()
        {
            JArray jArray = JArray.Parse(JsonConvert.SerializeObject(Enumerable.Range(1, 10).Select(
                                                                         i => new
                                                                         {
                                                                             SomeString = "hello",
                                                                             SomeInt = 123
                                                                         }).ToArray()));

            jArray.ToDisplayString()
                  .Should()
                  .Be(jArray.ToString());
        }

        [Fact]
        public void ListExpansionLimit_can_be_specified_per_type()
        {
            Formatter<Dictionary<string, int>>.ListExpansionLimit = 1000;
            Formatter.ListExpansionLimit = 4;
            var dictionary = new Dictionary<string, int>
            {
                { "zero", 0 },
                { "two", 2 },
                { "three", 3 },
                { "four", 4 },
                { "five", 5 },
                { "six", 6 },
                { "seven", 7 },
                { "eight", 8 },
                { "nine", 9 },
                { "ninety-nine", 99 }
            };

            var output = dictionary.ToDisplayString();

            output.Should().Contain("zero");
            output.Should().Contain("0");
            output.Should().Contain("ninety-nine");
            output.Should().Contain("99");
        }

        [Fact]
        public void Formatting_can_be_chosen_based_on_mime_type()
        {
            Formatter.Register(
                new PlainTextFormatter<DateTime>((time, writer) => writer.Write("plain")));
            Formatter.Register(
                new HtmlFormatter<DateTime>((time, writer) => writer.Write("html")));

            var now = DateTime.Now;

            now.ToDisplayString(PlainTextFormatter.MimeType).Should().Be("plain");
            now.ToDisplayString(HtmlFormatter.MimeType).Should().Be("html");
        }
    }
}