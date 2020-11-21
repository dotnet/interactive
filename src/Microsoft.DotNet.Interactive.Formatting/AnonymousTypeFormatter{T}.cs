// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal class AnonymousTypeFormatter<T> : TypeFormatter<T>
    {
        private readonly Func<FormatContext, T, TextWriter, bool> _format;

        public AnonymousTypeFormatter(
            Func<FormatContext, T, TextWriter, bool> format, 
            string mimeType, 
            Type type = null)
            : base(type)
        {
            if (string.IsNullOrWhiteSpace(mimeType))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(mimeType));
            }

            MimeType = mimeType;

            _format = format ?? throw new ArgumentNullException(nameof(format));
        }

        public override bool Format(FormatContext context, T instance, TextWriter writer)
        {
            return _format(context, instance, writer);
        }

        public override string MimeType { get; }
    }
}