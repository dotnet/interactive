// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
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

        public static IReadOnlyCollection<FormattedValue> FromObject(object value)
        {
            var type = value?.GetType();

            var mimeTypes = MimeTypesFor(type);

            return mimeTypes
                .Select(mimeType =>
                    new FormattedValue(mimeType, value.ToDisplayString(mimeType)))
                .ToArray();
        }

        private static IEnumerable<string> MimeTypesFor(Type returnValueType)
        {
            var mimeTypes = new HashSet<string> ();

            if (returnValueType != null)
            {
                var preferredMimeType = Formatter.PreferredMimeTypeFor(returnValueType) ??
                      (returnValueType?.IsPrimitive == true
                          ? PlainTextFormatter.MimeType
                          : Formatter.DefaultMimeType);

                if (!string.IsNullOrWhiteSpace(preferredMimeType))
                {
                    mimeTypes.Add(preferredMimeType);
                }
            }

            return mimeTypes;
        }
    }
}