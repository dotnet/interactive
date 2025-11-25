// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Formatting.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Formatting.Tests;

public class BinaryFormatterTests : FormatterTestBase
{
    [Fact]
    public void Byte_array_formats_as_hex_dump_in_plain_text()
    {
        var bytes = new byte[] { 
            0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x20, 0x57, 0x6F, 0x72, 0x6C, 0x64, 0x21,  // "Hello World!"
            0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x20, 0x57, 0x6F, 0x72, 0x6C, 0x64, 0x21
        };

        var formatted = bytes.ToDisplayString(PlainTextFormatter.MimeType);

        formatted.Should().Contain("00000000");
        formatted.Should().Contain("48 65 6C 6C 6F 20 57 6F");
        formatted.Should().Contain("|Hello World!");
    }

    [Fact]
    public void Byte_array_formats_as_hex_dump_in_html()
    {
        var bytes = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };

        var formatted = bytes.ToDisplayString(HtmlFormatter.MimeType).RemoveStyleElement();

        formatted.Should().Contain("<pre class=\"dni-binary\">");
        formatted.Should().Contain("00000000");
        formatted.Should().Contain("48 65 6C 6C 6F");
        formatted.Should().Contain("</pre>");
    }

    [Fact]
    public void Empty_byte_array_produces_empty_output()
    {
        var bytes = Array.Empty<byte>();

        var formatted = bytes.ToDisplayString(PlainTextFormatter.MimeType);

        formatted.Should().BeEmpty();
    }

    [Fact]
    public void Null_byte_array_shows_null_string()
    {
        byte[] bytes = null;

        var formatted = bytes.ToDisplayString(PlainTextFormatter.MimeType);

        formatted.Should().Contain(Formatter.NullString);
    }

    [Fact]
    public void Long_byte_array_spans_multiple_lines()
    {
        var bytes = new byte[32]; // Two lines worth
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)i;
        }

        var formatted = bytes.ToDisplayString(PlainTextFormatter.MimeType);

        formatted.Should().Contain("00000000");
        formatted.Should().Contain("00000010");
    }

    [Fact]
    public void Non_printable_characters_show_as_dots()
    {
        var bytes = new byte[] { 0x00, 0x01, 0x02, 0xFF };

        var formatted = bytes.ToDisplayString(PlainTextFormatter.MimeType);

        formatted.Should().Contain("|....|");
    }

    [Fact]
    public void ReadOnlyMemory_byte_formats_like_byte_array()
    {
        var bytes = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };
        var memory = new ReadOnlyMemory<byte>(bytes);

        var formatted = memory.ToDisplayString(PlainTextFormatter.MimeType);

        formatted.Should().Contain("00000000");
        formatted.Should().Contain("48 65 6C 6C 6F");
    }

    [Fact]
    public void Partial_last_line_is_padded_correctly()
    {
        var bytes = new byte[] { 0x01, 0x02, 0x03, 0x04 }; // Only 4 bytes

        var formatted = bytes.ToDisplayString(PlainTextFormatter.MimeType);

        // Should have proper spacing even with fewer than 16 bytes
        formatted.Should().Contain("01 02 03 04");
        formatted.Should().Contain("|....|");
    }
}