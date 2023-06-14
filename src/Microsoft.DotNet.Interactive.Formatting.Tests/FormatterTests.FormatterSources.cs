// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.Interactive.Formatting.Tests;

public sealed partial class FormatterTests
{
    public class FormatterSources
    {
        [Theory]
        [InlineData("text/plain")]
        [InlineData("text/html")]
        public void Formatter_sources_can_provide_lazy_registration_of_custom_formatters(string mimeType)
        {
            var obj = new TypeWithCustomFormatter();

            var formatted = obj.ToDisplayString(mimeType);

            formatted.Should().Be($"Hello from {nameof(CustomFormatterSource)} using MIME type {mimeType}");
        }

        [Theory]
        [InlineData("text/plain")]
        [InlineData("text/html")]
        public void Convention_based_formatter_sources_can_provide_lazy_registration_of_custom_formatters(string mimeType)
        {
            var obj = new TypeWithConventionBasedFormatter();

            var formatted = obj.ToDisplayString(mimeType);

            formatted.Should().Be($"Hello from {typeof(ConventionBased.FormatterSource)} using MIME type {mimeType}");
        }

        [Fact]
        public void Formatter_sources_are_still_registered_after_formatters_are_reset()
        {
            var obj = new TypeWithCustomFormatter();

            var formattedBefore = obj.ToDisplayString("text/html");

            Formatter.ResetToDefault();

            var formattedAfter = obj.ToDisplayString("text/html");

            formattedAfter.Should().Be(formattedBefore);
        }

        [Fact]
        public void Formatter_sources_can_set_preferred_MIME_type()
        {
            var obj = new TypeWithCustomFormatterAndMimeType();

            var preferredMimeTypes = Formatter.GetPreferredMimeTypesFor(obj.GetType());

            preferredMimeTypes
                .Should()
                .BeEquivalentTo(
                    new[]
                    {
                        "text/one",
                        "text/two"
                    },
                    c => c.WithStrictOrdering());
        }

        [Fact]
        public void Convention_based_formatter_sources_are_still_registered_after_formatters_are_reset()
        {
            var obj = new TypeWithConventionBasedFormatter();

            var formattedBefore = obj.ToDisplayString("text/html");

            Formatter.ResetToDefault();

            var formattedAfter = obj.ToDisplayString("text/html");

            formattedAfter.Should().Be(formattedBefore);
        }

        [Fact]
        public void Convention_based_formatter_sources_can_set_preferred_MIME_type()
        {
            var obj = new TypeWithConventionBasedFormatterAndMimeType();

            var preferredMimeTypes = Formatter.GetPreferredMimeTypesFor(obj.GetType());

            preferredMimeTypes
                .Should()
                .BeEquivalentTo(
                    new[]
                    {
                        "text/one",
                        "text/two"
                    },
                    c => c.WithStrictOrdering());
        }

        [TypeFormatterSource(typeof(CustomFormatterSource))]
        private class TypeWithCustomFormatter
        {
        }

        [TypeFormatterSource(typeof(CustomFormatterSource), PreferredMimeTypes = new[] { "text/one", "text/two" })]
        private class TypeWithCustomFormatterAndMimeType
        {
        }

        private class CustomFormatterSource : ITypeFormatterSource
        {
            public IEnumerable<ITypeFormatter> CreateTypeFormatters()
            {
                return new ITypeFormatter[]
                {
                    new PlainTextFormatter<TypeWithCustomFormatter>(_ => $"Hello from {nameof(CustomFormatterSource)} using MIME type text/plain"),
                    new HtmlFormatter<TypeWithCustomFormatter>(_ => $"Hello from {nameof(CustomFormatterSource)} using MIME type text/html")
                };
            }
        }

        [ConventionBased.TypeFormatterSourceAttribute(typeof(ConventionBased.FormatterSource))]
        private class TypeWithConventionBasedFormatter
        {
        }

        [ConventionBased.TypeFormatterSourceAttribute(typeof(ConventionBased.FormatterSource), PreferredMimeTypes = new []{ "text/one", "text/two" })]
        private class TypeWithConventionBasedFormatterAndMimeType
        {
        }

        private static class ConventionBased
        {
            // This class is here to allow this type not to conflict with the Microsoft.DotNet.Interactive.Formatting.TypeFormatterSourceAttribute

            [AttributeUsage(AttributeTargets.Class)]
            internal class TypeFormatterSourceAttribute : Attribute
            {
                public TypeFormatterSourceAttribute(Type formatterSourceType)
                {
                    FormatterSourceType = formatterSourceType;
                }

                public Type FormatterSourceType { get; }

                public string[] PreferredMimeTypes { get; set; }
            }

            internal class FormatterSource
            {
                public IEnumerable<object> CreateTypeFormatters()
                {
                    yield return new ConventionBasedFormatter { MimeType = "text/plain" };
                    yield return new ConventionBasedFormatter { MimeType = "text/html" };
                }
            }

            internal class ConventionBasedFormatter
            {
                public string MimeType { get; init; }

                public bool Format(object instance, TextWriter writer)
                {
                    writer.Write($"Hello from {typeof(FormatterSource)} using MIME type {MimeType}");
                    return true;
                }
            }
        }
    }
}