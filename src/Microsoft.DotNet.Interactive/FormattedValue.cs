﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
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

        public static IReadOnlyCollection<FormattedValue> FromObject(object value, params string[] mimeTypes)
        {
            if (mimeTypes is null || 
                mimeTypes.Length == 0)
            {
                mimeTypes = Formatter.GetPreferredMimeTypesFor(value?.GetType()).ToArray();
            }

            var formattedValues = mimeTypes.Select(mimeType => new FormattedValue(
                                                       mimeType,
                                                       value.ToDisplayString(mimeType))).ToArray();

            return formattedValues;
        }
    }
}