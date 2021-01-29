// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text.Json;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public class JsonFormatter<T> : TypeFormatter<T>
    {
        private readonly Func<FormatContext, T, TextWriter, bool> _format;

        public override bool Format(FormatContext context, T instance, TextWriter writer)
        {
            return _format(context, instance, writer);
        }

        public JsonFormatter()
        {
            _format = (context, instance, writer) => {
                var json = JsonSerializer.Serialize(instance, JsonFormatter.SerializerOptions);

                writer.Write(json);
                return true;
            };
        }

        public JsonFormatter(Func<FormatContext, T, TextWriter, bool> format)
        {
            _format = format;
        }

        public override string MimeType { get; } = JsonFormatter.MimeType;
    }
}