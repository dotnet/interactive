// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public abstract class TypeFormatter<T> : ITypeFormatter<T>
    {
        protected TypeFormatter(Type type = null)
        {
            Type = type ?? typeof(T);
        }

        public abstract bool Format(T value, FormatContext context);

        public Type Type { get; }

        public abstract string MimeType { get; }

        bool ITypeFormatter.Format(object instance, FormatContext context)
        {
            return Format((T) instance, context);
        }
    }
}