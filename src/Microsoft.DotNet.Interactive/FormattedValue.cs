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
            // FIX: (FromObject) consolidate
            var type = value?.GetType();

            var mimeTypes = MimeTypesFor(type).ToArray();

            var preferredMimeType = Formatter.GetPreferredMimeTypeFor(type ?? typeof(object));

            if (mimeTypes.Length != 1)
            {
                
            }
            else if (preferredMimeType != mimeTypes[0])
            {

            }

            var formattedValues = mimeTypes
                                     .Select(mimeType =>
                                                 new FormattedValue(mimeType, value.ToDisplayString(mimeType)))
                                     .ToArray();

            if (formattedValues.Length == 0)
            {
                
            }

            return formattedValues;
        }

        private static IEnumerable<string> MimeTypesFor(Type type)
        {
            var mimeTypes = new HashSet<string> ();

            if (type != null)
            {
                var preferredMimeType = Formatter.GetPreferredMimeTypeFor(type);

                if (preferredMimeType == null)
                {
                    if (type?.IsPrimitive == true)
                    {
                        preferredMimeType = PlainTextFormatter.MimeType;
                    }
                    else
                    {
                        preferredMimeType = Formatter.DefaultMimeType;
                    }
                }

                if (!string.IsNullOrWhiteSpace(preferredMimeType))
                {
                    mimeTypes.Add(preferredMimeType);
                }
            }

            return mimeTypes;
        }
    }
}