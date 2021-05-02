﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal class AnonymousTypeFormatter<T> : TypeFormatter<T>
    {
        private readonly FormatDelegate<T> _format;

        public AnonymousTypeFormatter(
            FormatDelegate<T> format,
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

        public override bool Format(T instance, FormatContext context)
        {
            return _format(instance, context);
        }

        public override string MimeType { get; }
    }
}