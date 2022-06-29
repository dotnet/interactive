// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.DotNet.Interactive.Formatting;

internal class MultiLinePlainTextFormatter : IPlainTextFormatter
{
    private string   _indent = "    ";

    public void WriteStartProperty(TextWriter writer)
    {
      
        writer.Write(_indent);
    }

    public void WriteEndProperty(TextWriter writer)
    {
     
    }

    public void WriteStartObject(TextWriter writer)
    {
     
    }

    public void WriteEndObject(TextWriter writer)
    {
     
    }

    public void WriteStartSequence(TextWriter writer)
    {
     
    }

    public void WriteEndSequence(TextWriter writer)
    {
     
    }

    public void WriteStartTuple(TextWriter writer)
    {
     
    }

    public void WriteEndTuple(TextWriter writer)
    {
     
    }

    public void WriteNameValueDelimiter(TextWriter writer)
    {
     
    }

    public void WritePropertyDelimiter(TextWriter writer)
    {
     
    }

    public void WriteElidedPropertiesMarker(TextWriter writer)
    {
     
    }

    public void WriteSequenceDelimiter(TextWriter writer)
    {
     
    }

    public void WriteEndHeader(TextWriter writer)
    {
     
    }

    public void WriteStartSequenceItem(TextWriter writer)
    {
     
    }
}