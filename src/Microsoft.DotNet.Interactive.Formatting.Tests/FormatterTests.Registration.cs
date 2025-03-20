// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Formatting.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Formatting.Tests;

public sealed partial class FormatterTests
{
    [TestClass]
    public class Registration : FormatterTestBase
    {
        [TestMethod]
        public void Can_Register_formatter_for_type_string()
        {
            var value = "hola!";

            var defaultValue = value.ToDisplayString();

            Formatter.Register<string>(e => "hello!");

            value.ToDisplayString().Should().NotBe(defaultValue);

            Formatter.ResetToDefault();

            value.ToDisplayString().Should().Be(defaultValue);
        }


        [TestMethod]
        public void ToDisplayString_uses_actual_type_formatter_and_not_compiled_type()
        {
            Widget widget = new InheritedWidget();
            var widgetFormatterCalled = false;
            var inheritedWidgetFormatterCalled = false;

            Formatter.Register<Widget>(w =>
            {
                widgetFormatterCalled = true;
                return "";
            });
            Formatter.Register<InheritedWidget>(w =>
            {
                inheritedWidgetFormatterCalled = true;
                return "";
            });

            widget.ToDisplayString();

            widgetFormatterCalled.Should().BeFalse();
            inheritedWidgetFormatterCalled.Should().BeTrue();
        }

        [TestMethod]
        [DataRow("text/html", "<div class=\"dni-plaintext\"><pre># { This is the &lt;input&gt; &quot;yes&quot;\t\b\n\r }</pre></div>")]
        [DataRow("text/plain", "# { This is the <input> \"yes\"\t\b\n\r }")]
        [DataRow("application/json", "\"# { This is the <input> \\\"yes\\\"\\t\\b\\n\\r }\"")]
        public void When_input_is_a_string_with_unusual_characters_then_it_is_encoded_appropriately(string mimeType, string expected)
        {
            var input = "# { This is the <input> \"yes\"\t\b\n\r }";

            var result = input.ToDisplayString(mimeType).RemoveStyleElement();

            result.Should().Be(expected);
        }

        [TestMethod]
        [DataRow("text/plain", false)]
        [DataRow("text/plain", true)]
        [DataRow("text/html", false)]
        [DataRow("text/html", true)]
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

        [TestMethod]
        [DataRow("text/plain", false)]
        [DataRow("text/plain", true)]
        [DataRow("text/html", false)]
        [DataRow("text/html", true)]
        public void Formatters_can_be_registered_for_obj_type(string mimeType, bool useGenericRegisterMethod)
        {
            if (useGenericRegisterMethod)
            {
                Formatter.Register<object>(
                    formatter: (filInfo, writer) => writer.Write("hello"),
                    mimeType);
            }
            else
            {
                Formatter.Register(
                    type: typeof(object),
                    formatter: (filInfo, writer) => writer.Write("hello"),
                    mimeType);
            }

            new FileInfo(@"c:\temp\foo.txt").ToDisplayString(mimeType)
                                            .Should()
                                            .Be("hello");
        }

        [TestMethod]
        [DataRow("text/plain", false)]
        [DataRow("text/plain", true)]
        [DataRow("text/html", false)]
        [DataRow("text/html", true)]
        public void Formatters_choose_exact_type_amongst_user_defined_formatters(string mimeType, bool useGenericRegisterMethod)
        {
            if (useGenericRegisterMethod)
            {
                Formatter.Register<FileInfo>(
                    formatter: (filInfo, writer) => writer.Write("hello"),
                    mimeType);
                Formatter.Register<object>(
                    formatter: (filInfo, writer) => writer.Write("world"),
                    mimeType);
            }
            else
            {
                Formatter.Register(
                    type: typeof(FileInfo),
                    formatter: (filInfo, writer) => writer.Write("hello"),
                    mimeType);
                Formatter.Register(
                    type: typeof(object),
                    formatter: (filInfo, writer) => writer.Write("world"),
                    mimeType);
            }

            // The FileInfo formatter is chosen for FileInfo
            new FileInfo(@"c:\temp\foo.txt").ToDisplayString(mimeType)
                                            .Should()
                                            .Be("hello");

            // The object formatter is chosen for System.Object
            (new object()).ToDisplayString(mimeType)
                          .Should()
                          .Be("world");

            // The object formatter is chosen for DirectoryInfo (which is a FileSystemInfo but not a FileInfo).
            new DirectoryInfo(@"c:\temp").ToDisplayString(mimeType)
                                         .Should()
                                         .Be("world");

        }

        [TestMethod]
        [DataRow("text/plain", false)]
        [DataRow("text/plain", true)]
        [DataRow("text/html", false)]
        [DataRow("text/html", true)]
        public void Formatters_choose_most_specific_type_amongst_user_defined_formatters(string mimeType, bool useGenericRegisterMethod)
        {
            if (useGenericRegisterMethod)
            {
                Formatter.Register<IComparable>(
                    formatter: (filInfo, writer) => writer.Write("hello"),
                    mimeType);
                Formatter.Register<object>(
                    formatter: (filInfo, writer) => writer.Write("world"),
                    mimeType);
            }
            else
            {
                Formatter.Register(
                    type: typeof(IComparable),
                    formatter: (filInfo, writer) => writer.Write("hello"),
                    mimeType);
                Formatter.Register(
                    type: typeof(object),
                    formatter: (filInfo, writer) => writer.Write("world"),
                    mimeType);
            }

            // The IComparable formatter is chosen for System.Int32, which supports 'IComparable'
            (100).ToDisplayString(mimeType)
                 .Should()
                 .Be("hello");

            // The IComparable formatter is chosen for System.DateTime, which supports 'IComparable'
            DateTime.Now.ToDisplayString(mimeType)
                    .Should()
                    .Be("hello");

            // The object formatter is chosen for something not supporting 'IComparable'
            // Note System.Type doesn't support IComparable.
            typeof(int).ToDisplayString(mimeType)
                       .Should()
                       .Be("world");
        }

        [TestMethod]
        [DataRow("text/plain")]
        [DataRow("text/html")]
        public void Formatters_choose_most_recently_registered_formatter_is_preferred(string mimeType)
        {
            Formatter.Register<IComparable>(
                formatter: (filInfo, writer) => writer.Write("hello"),
                mimeType);
            Formatter.Register<object>(
                formatter: (filInfo, writer) => writer.Write("world"),
                mimeType);

            // The first IComparable formatter is chosen for System.Int32, which supports 'IComparable'
            (100).ToDisplayString(mimeType)
                 .Should()
                 .Be("hello");

            Formatter.Register<IComparable>(
                formatter: (filInfo, writer) => writer.Write("hello again!"),
                mimeType);

            // Now the second IComparable formatter is chosen for System.Int32, which supports 'IComparable'
            (100).ToDisplayString(mimeType)
                 .Should()
                 .Be("hello again!");

            // The object formatter is chosen for something not supporting 'IComparable'
            // Note System.Type doesn't support IComparable.
            typeof(int).ToDisplayString(mimeType)
                       .Should()
                       .Be("world");
        }

        [TestMethod]
        [DataRow("text/plain", false)]
        [DataRow("text/plain", true)]
        [DataRow("text/html", false)]
        [DataRow("text/html", true)]
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

        [TestMethod]
        [DataRow("text/plain", false)]
        [DataRow("text/plain", true)]
        [DataRow("text/html", false)]
        [DataRow("text/html", true)]
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

        [TestMethod]
        [DataRow("text/plain")]
        [DataRow("text/html")]
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

        [TestMethod]
        [DataRow("text/plain")]
        [DataRow("text/html")]
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

        [TestMethod]
        public void Formatting_can_be_chosen_based_on_mime_type()
        {
            Formatter.Register(
                new PlainTextFormatter<DateTime>((time, c) => c.Writer.Write("plain")));
            Formatter.Register(
                new HtmlFormatter<DateTime>((time, c) => c.Writer.Write("html")));

            var now = DateTime.Now;

            now.ToDisplayString(PlainTextFormatter.MimeType).Should().Be("plain");
            now.ToDisplayString(HtmlFormatter.MimeType).Should().Be("html");
        }

    }
}

