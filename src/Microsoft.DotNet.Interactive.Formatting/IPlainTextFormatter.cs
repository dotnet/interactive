// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal interface IPlainTextFormatter
    {
        void WriteStartProperty(TextWriter writer);
        void WriteEndProperty(TextWriter writer);
        void WriteStartObject(TextWriter writer);
        void WriteEndObject(TextWriter writer);
        void WriteStartSequence(TextWriter writer);
        void WriteEndSequence(TextWriter writer);
        void WriteStartTuple(TextWriter writer);
        void WriteEndTuple(TextWriter writer);
        void WriteNameValueDelimiter(TextWriter writer);
        void WritePropertyDelimiter(TextWriter writer);
        void WriteSequenceDelimiter(TextWriter writer);
        void WriteEndHeader(TextWriter writer);
        void WriteStartSequenceItem(TextWriter writer);
    }
}