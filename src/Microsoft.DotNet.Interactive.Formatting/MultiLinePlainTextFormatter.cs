// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Formatting;

internal class MultiLinePlainTextFormatter : IPlainTextFormatter
{
    public void WriteStartObject(FormatContext context)
    {
    }

    public void WriteEndObject(FormatContext context)
    {
    }

    public void WriteEndHeader(FormatContext context)
    {
        context.Writer.WriteLine();
        // context.Indent++;
    }

    public void WriteStartProperty(FormatContext context)
    {
        if (context.IsStartingObjectWithinSequence)
        {
            WriteIndent(context, "  - ");
            context.IsStartingObjectWithinSequence = false;
        }
        else
        {
            WriteIndent(context);
        }
    }

    public void WriteEndProperty(FormatContext context)
    {
    }

    public void WriteObjectSequenceItemSeparator(FormatContext context)
    {
        context.Writer.WriteLine();
    }

    public void WriteEndObjectWithinSequence(FormatContext context)
    {
        // context.Writer.WriteLine();
    }

    public void WriteStartSequenceOfObjects(FormatContext context)
    {
        // WriteIndent(context);
    }

    public void WriteStartSequenceOfValues(FormatContext context)
    {
        context.Writer.Write("[ ");
    }

    public void WriteEndSequenceOfObjects(FormatContext context)
    {
    }

    public void WriteEndSequenceOfValues(FormatContext context)
    {
        context.Writer.Write(" ]");
    }

    public void WriteStartTuple(FormatContext context)
    {
        context.Writer.Write("( ");
    }

    public void WriteEndTuple(FormatContext context)
    {
        context.Writer.Write(" )");
    }

    public void WriteNameValueDelimiter(FormatContext context)
    {
        context.Writer.Write(": ");
    }

    public void WritePropertyListSeparator(FormatContext context)
    {
        context.Writer.WriteLine();
    }

    public void WriteElidedPropertiesMarker(FormatContext context)
    {
        WriteIndent(context);
        context.Writer.Write("...");
    }

    public void WriteValueSequenceItemSeparator(FormatContext context)
    {
        context.Writer.Write(", ");
    }

    private void WriteIndent(FormatContext context, string bonus = "    ")
    {
        var effectiveIndent = context.Depth * 4;
        var indent = new string(' ', effectiveIndent);
        context.Writer.Write(indent);
        context.Writer.Write(bonus);
    }
}