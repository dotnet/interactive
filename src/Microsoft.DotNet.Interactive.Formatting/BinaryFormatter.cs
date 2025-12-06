// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive.Formatting;

/// <summary>
/// Provides formatting for binary data (byte arrays) with hexadecimal representation.
/// </summary>
public static class BinaryFormatter
{
    private const int BytesPerLine = 16;
    
    public static string FormatBytes(byte[] bytes)
    {
        if (bytes is null || bytes.Length == 0)
        {
            return string.Empty;
        }

        using var writer = new StringWriter();
        FormatBytesTo(bytes, writer);
        return writer.ToString();
    }

    public static void FormatBytesTo(byte[] bytes, TextWriter writer)
    {
        if (bytes is null || bytes.Length == 0)
        {
            return;
        }

        for (int offset = 0; offset < bytes.Length; offset += BytesPerLine)
        {
            // Write address offset
            writer.Write($"{offset:X8}  ");

            // Write hex values
            int bytesInLine = Math.Min(BytesPerLine, bytes.Length - offset);
            
            for (int i = 0; i < BytesPerLine; i++)
            {
                if (i < bytesInLine)
                {
                    writer.Write($"{bytes[offset + i]:X2} ");
                }
                else
                {
                    writer.Write("   ");
                }

                // Add extra space after 8 bytes for readability
                if (i == 7)
                {
                    writer.Write(" ");
                }
            }

            // Write ASCII representation
            writer.Write(" |");
            for (int i = 0; i < bytesInLine; i++)
            {
                byte b = bytes[offset + i];
                char c = (b >= 32 && b < 127) ? (char)b : '.';
                writer.Write(c);
            }
            writer.Write("|");

            if (offset + BytesPerLine < bytes.Length)
            {
                writer.WriteLine();
            }
        }
    }

    internal static ITypeFormatter[] DefaultFormatters { get; } =
    {
        // PlainText formatter for byte arrays
        new PlainTextFormatter<byte[]>((bytes, context) =>
        {
            if (bytes is null)
            {
                context.Writer.Write(Formatter.NullString);
                return true;
            }

            FormatBytesTo(bytes, context.Writer);
            return true;
        }),

        // HTML formatter for byte arrays
        new HtmlFormatter<byte[]>((bytes, context) =>
        {
            if (bytes is null)
            {
                context.Writer.Write(Formatter.NullString);
                return true;
            }

            context.Writer.Write("<pre class=\"dni-binary\">");
            FormatBytesTo(bytes, context.Writer);
            context.Writer.Write("</pre>");
            return true;
        }),

        // PlainText formatter for ReadOnlyMemory<byte>
        new AnonymousTypeFormatter<object>(
            type: typeof(ReadOnlyMemory<byte>),
            mimeType: PlainTextFormatter.MimeType,
            format: (value, context) =>
            {
                var readOnlyMemory = (ReadOnlyMemory<byte>)value;
                var bytes = readOnlyMemory.ToArray();
                FormatBytesTo(bytes, context.Writer);
                return true;
            }),

        // HTML formatter for ReadOnlyMemory<byte>
        new AnonymousTypeFormatter<object>(
            type: typeof(ReadOnlyMemory<byte>),
            mimeType: HtmlFormatter.MimeType,
            format: (value, context) =>
            {
                var readOnlyMemory = (ReadOnlyMemory<byte>)value;
                var bytes = readOnlyMemory.ToArray();
                
                context.Writer.Write("<pre class=\"dni-binary\">");
                FormatBytesTo(bytes, context.Writer);
                context.Writer.Write("</pre>");
                return true;
            })
    };
}