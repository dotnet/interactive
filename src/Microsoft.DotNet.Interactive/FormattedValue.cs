// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive
{
    public class FormattedValue
    {
        public FormattedValue(string mimeType, string value)
        {
            if (string.IsNullOrWhiteSpace(mimeType))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(mimeType));
            }

            MimeType = mimeType;
            Value = value;
        }

        public string MimeType { get; }

        public string Value { get; }

        public static IReadOnlyCollection<FormattedValue> FromObject(object value, string mimeType = null)
        {
            mimeType ??= Formatter.GetPreferredMimeTypeFor(value?.GetType());

            var formattedValue = new FormattedValue(
                mimeType,
                value.ToDisplayString(mimeType));

            return new[] { formattedValue };
        }
    }
}