// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Formatting;

internal class SingleLinePlainTextFormatter : IPlainTextFormatter
{
    private const string EndObject = " }";
    private const string EndSequence = " ]";
    private const string EndTuple = " )";
    private const string SequenceItemSeparator = ", ";
    private const string NameValueDelimiter = ": ";
    private const string PropertyListSeparator = ", ";
    private const string StartObject = "{ ";
    private const string StartSequence = "[ ";
    private const string StartTuple = "( ";

    public void WriteStartProperty(FormatContext context)
    {
    }

    public void WriteEndProperty(FormatContext context)
    {
    }

    public void WriteStartObject(FormatContext context) => context.Writer.Write(StartObject);

    public void WriteEndObject(FormatContext context) => context.Writer.Write(EndObject);

    public void WriteStartSequenceOfValues(FormatContext context) => context.Writer.Write(StartSequence);

    public void WriteStartSequenceOfObjects(FormatContext context) => context.Writer.Write(StartSequence);

    public void WriteEndObjectWithinSequence(FormatContext context)
    {
    }

    public void WriteEndSequenceOfObjects(FormatContext context) => context.Writer.Write(EndSequence);

    public void WriteEndSequenceOfValues(FormatContext context) => context.Writer.Write(EndSequence);

    public void WriteStartTuple(FormatContext context) => context.Writer.Write(StartTuple);

    public void WriteEndTuple(FormatContext context) => context.Writer.Write(EndTuple);

    public void WriteNameValueDelimiter(FormatContext context) => context.Writer.Write(NameValueDelimiter);

    public void WritePropertyListSeparator(FormatContext context) => context.Writer.Write(PropertyListSeparator);

    public void WriteValueSequenceItemSeparator(FormatContext context) => context.Writer.Write(SequenceItemSeparator);

    public void WriteObjectSequenceItemSeparator(FormatContext context) => context.Writer.Write(", ");

    public void WriteEndHeader(FormatContext context) => context.Writer.Write(": ");

    public void WriteElidedPropertiesMarker(FormatContext context) => context.Writer.Write("..");

    public static IPlainTextFormatter Instance { get; } = new SingleLinePlainTextFormatter();
}