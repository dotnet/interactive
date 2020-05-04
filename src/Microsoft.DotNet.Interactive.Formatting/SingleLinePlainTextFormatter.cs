// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal class SingleLinePlainTextFormatter : IPlainTextFormatter
    {
        private const string EndObject = " }";
        private const string EndSequence = " ]";
        private const string EndTuple = " )";
        private const string ItemSeparator = ", ";
        private const string NameValueDelimiter = ": ";
        private const string PropertySeparator = ", ";
        private const string StartObject = "{ ";
        private const string StartSequence = "[ ";
        private const string StartTuple = "( ";

        public void WriteStartProperty(TextWriter writer)
        {
        }

        public void WriteEndProperty(TextWriter writer)
        {
        }

        public void WriteStartObject(TextWriter writer) => writer.Write(StartObject);

        public void WriteEndObject(TextWriter writer) => writer.Write(EndObject);

        public void WriteStartSequence(TextWriter writer) => writer.Write(StartSequence);

        public void WriteEndSequence(TextWriter writer) => writer.Write(EndSequence);
        public void WriteStartTuple(TextWriter writer) => writer.Write(StartTuple);

        public void WriteEndTuple(TextWriter writer) => writer.Write(EndTuple);

        public void WriteNameValueDelimiter(TextWriter writer) => writer.Write(NameValueDelimiter);

        public void WritePropertyDelimiter(TextWriter writer) => writer.Write(PropertySeparator);

        public void WriteSequenceDelimiter(TextWriter writer) => writer.Write(ItemSeparator);

        public void WriteEndHeader(TextWriter writer) => writer.Write(": ");

        public void WriteStartSequenceItem(TextWriter writer)
        {
        }

        public static IPlainTextFormatter Instance { get; } = new SingleLinePlainTextFormatter();
    }
}