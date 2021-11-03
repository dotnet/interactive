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

        public override bool Format(T instance, FormatContext context)
        {
            return _format(instance, context);
        }

        public JsonFormatter()
        {
            _format = FormatInstance;

            bool FormatInstance(T instance, FormatContext context)
            {
                var json = JsonSerializer.Serialize(instance, JsonFormatter.SerializerOptions);
                context.Writer.Write(json);
                return true;
            }
        }

        public JsonFormatter(FormatDelegate<T> format)
        {
            _format = format;
        }

        public JsonFormatter(Action<T, FormatContext> format)
        {
            _format = FormatInstance;

            bool FormatInstance(T instance, FormatContext context)
            {
                format(instance, context);
                return true;
            }
        }

        public override string MimeType { get; } = JsonFormatter.MimeType;
    }
}