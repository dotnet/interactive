// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal class AnonymousTypeFormatter<T> : TypeFormatter<T>
    {
        private readonly Action<T, TextWriter> _format;

        public AnonymousTypeFormatter(Action<T, TextWriter> format, string mimeType)
        {
            if (string.IsNullOrWhiteSpace(mimeType))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(mimeType));
            }

            MimeType = mimeType;

            _format = format ?? throw new ArgumentNullException(nameof(format));
        }

        public override void Format(T instance, TextWriter writer)
        {
            _format(instance, writer);
        }

        public override string MimeType { get; }
    }
}