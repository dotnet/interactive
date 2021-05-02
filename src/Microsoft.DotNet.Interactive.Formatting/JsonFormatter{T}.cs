// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text.Json;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public class JsonFormatter<T> : TypeFormatter<T>
    {
        private readonly FormatDelegate<T> _format;

        public override bool Format(T instance, TextWriter writer, FormatContext context)
        {
            return _format(instance, writer, context);
        }

        public JsonFormatter()
        {
            _format = (instance, writer, context) => {
                var json = JsonSerializer.Serialize(instance, JsonFormatter.SerializerOptions);

                writer.Write(json);
                return true;
            };
        }

        public JsonFormatter(FormatDelegate<T> format)
        {
            _format = format;
        }

        public override string MimeType { get; } = JsonFormatter.MimeType;
    }
}