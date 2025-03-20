// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Formatting.Csv;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Microsoft.DotNet.Interactive.Formatting.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Formatting.Tests;

public class CsvFormatterTests : FormatterTestBase
{
    private static readonly List<SomethingWithLotsOfProperties> _objects = new()
    {
        new()
        {
            DateProperty = DateTimeOffset.Parse("2021-11-03T08:56:24.8079327+00:00").DateTime,
            StringProperty = "hi",
            IntProperty = 123,
            BoolProperty = false,
            UriProperty = new Uri("https://example.com")
        },
        new()
        {
            DateProperty = DateTimeOffset.Parse("2021-11-04T18:37:03.8155442+00:00").DateTime,
            StringProperty = "hello there!",
            IntProperty = 456,
            BoolProperty = true,
            UriProperty = new Uri("https://example.com/something?query=string")
        },
    };

    [TestClass]
    public class Escaping : IDisposable
    {
        private readonly StringWriter _writer = new();

        [TestMethod]
        public void Values_containing_commas_are_wrapped_in_quotes()
        {
            var obj = new
            {
                Property1 = "no-comma-here",
                Property2 = "this one, has a comma",
                Property3 = 123,
            };

            var formatter = CsvFormatter.GetPreferredFormatterFor(obj.GetType());

            formatter.Format(obj, _writer);

            _writer.ToString().SplitIntoLines()[1].Should().Be("no-comma-here,\"this one, has a comma\",123");
        }

        [TestMethod]
        public void Values_containing_newlines_are_wrapped_in_quotes()
        {
            var obj = new
            {
                Property1 = "with\r\ntwo\nnewlines",
                Property2 = "with one newline at the end\n",
                Property3 = 123,
            };

            var formatter = CsvFormatter.GetPreferredFormatterFor(obj.GetType());

            formatter.Format(obj, _writer);

            _writer.ToString().Should().Contain("\"with\r\ntwo\nnewlines\",\"with one newline at the end\n\",123");
        }

        [TestMethod]
        public void Double_quotes_are_replaced_with_two_double_quotes()
        {
            var obj = new
            {
                Property1 = "this is one column value which includes a \" character",
                Property2 = 123,
            };

            var formatter = CsvFormatter.GetPreferredFormatterFor(obj.GetType());

            formatter.Format(obj, _writer);

            _writer.ToString().Should().Contain("\"this is one column value which includes a \"\" character\",123");
        }

        public void Dispose() => _writer?.Dispose();
    }

    [TestClass]
    public class Objects : IDisposable
    {
        private readonly ITypeFormatter<SomethingWithLotsOfProperties> _formatter;
        private readonly StringWriter _writer = new();

        public Objects()
        {
            _formatter = CsvFormatter.GetPreferredFormatterFor<SomethingWithLotsOfProperties>();
        }

        [TestMethod]
        public void It_creates_row_headers_based_on_property_names()
        {
            _formatter.Format(_objects.First(), _writer);

            var lines = _writer.ToString().SplitIntoLines();
            lines[0].Should().Be("DateProperty,StringProperty,IntProperty,BoolProperty,UriProperty");
        }

        [TestMethod]
        public void It_creates_rows_containing_values()
        {
            _formatter.Format(_objects.First(), _writer);

            var lines = _writer.ToString().SplitIntoLines();

            lines[1].Should().Be("2021-11-03T08:56:24.8079327,hi,123,False,https://example.com/");
        }

        public void Dispose() => _writer?.Dispose();
    }

    [TestClass]
    public class SequencesOfObjects : IDisposable
    {
        private readonly ITypeFormatter _formatter;
        private readonly StringWriter _writer = new();

        public SequencesOfObjects()
        {
            _formatter = CsvFormatter.GetPreferredFormatterFor(_objects.GetType());
        }

        [TestMethod]
        public void It_creates_row_headers_based_on_property_names()
        {
            _formatter.Format(_objects, _writer);

            var lines = _writer.ToString().SplitIntoLines();
            lines[0].Should().Be("DateProperty,StringProperty,IntProperty,BoolProperty,UriProperty");
        }

        [TestMethod]
        public void It_creates_rows_containing_values()
        {
            _formatter.Format(_objects, _writer);

            var lines = _writer.ToString().SplitIntoLines();
            lines[1].Should().Be("2021-11-03T08:56:24.8079327,hi,123,False,https://example.com/");
        }

        public void Dispose() => _writer?.Dispose();
    }

    [TestClass]
    public class Dictionaries : IDisposable
    {
        private readonly ITypeFormatter _formatter;
        private readonly StringWriter _writer = new();
        private readonly IDictionary<string, object> _dictionary;

        public Dictionaries()
        {
            _dictionary = Destructurer.GetOrCreate(_objects.First().GetType()).Destructure(_objects.First());
            _formatter = CsvFormatter.GetPreferredFormatterFor(_dictionary.GetType());
        }

        [TestMethod]
        public void It_creates_row_headers_based_on_keys()
        {
            _formatter.Format(_dictionary, _writer);

            var lines = _writer.ToString().SplitIntoLines();
            lines[0].Should().Be("DateProperty,StringProperty,IntProperty,BoolProperty,UriProperty");
        }

        [TestMethod]
        public void It_creates_rows_containing_values()
        {
            _formatter.Format(_dictionary, _writer);

            var lines = _writer.ToString().SplitIntoLines();
            lines[1].Should().Be("2021-11-03T08:56:24.8079327,hi,123,False,https://example.com/");
        }

        public void Dispose() => _writer?.Dispose();
    }

    [TestClass]
    public class TabularDataResources : IDisposable
    {
        private readonly ITypeFormatter<TabularDataResource> _formatter;
        private readonly StringWriter _writer = new();
        private readonly TabularDataResource _tabularDataResource;

        public TabularDataResources()
        {
            _formatter = CsvFormatter.GetPreferredFormatterFor<TabularDataResource>();
            _tabularDataResource = _objects.ToTabularDataResource();
        }

        [TestMethod]
        public void It_creates_row_headers_based_on_property_names()
        {
            _formatter.Format(_tabularDataResource, _writer);

            var lines = _writer.ToString().SplitIntoLines();
            lines[0].Should().Be("DateProperty,StringProperty,IntProperty,BoolProperty,UriProperty");
        }

        [TestMethod]
        public void It_creates_rows_containing_values()
        {
            _formatter.Format(_tabularDataResource, _writer);

            var lines = _writer.ToString().SplitIntoLines();
            lines[1].Should().Be("2021-11-03T08:56:24.8079327,hi,123,False,https://example.com/");
        }

        public void Dispose() => _writer?.Dispose();
    }
}