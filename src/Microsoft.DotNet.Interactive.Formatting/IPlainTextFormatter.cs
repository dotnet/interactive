// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Formatting;

internal interface IPlainTextFormatter
{
    void WriteStartProperty(FormatContext context);
    void WriteEndProperty(FormatContext context);
    void WriteStartObject(FormatContext context);
    void WriteEndObject(FormatContext context);
    void WriteStartSequenceOfValues(FormatContext context);
    void WriteStartSequenceOfObjects(FormatContext context);
    void WriteEndSequenceOfValues(FormatContext context);
    void WriteStartTuple(FormatContext context);
    void WriteEndTuple(FormatContext context);
    void WriteNameValueDelimiter(FormatContext context);
    void WritePropertyListSeparator(FormatContext context);
    void WriteElidedPropertiesMarker(FormatContext context);
    void WriteValueSequenceItemSeparator(FormatContext context);
    void WriteObjectSequenceItemSeparator(FormatContext context);
    void WriteStartObjectWithinSequence(FormatContext context);
    void WriteEndHeader(FormatContext context);
    void WriteEndSequenceOfObjects(FormatContext context);
    void WriteEndObjectWithinSequence(FormatContext context);
}