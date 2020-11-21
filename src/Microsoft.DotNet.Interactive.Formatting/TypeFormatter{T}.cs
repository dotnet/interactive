// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public abstract class TypeFormatter<T> : ITypeFormatter<T>
    {
        protected TypeFormatter(Type type = null)
        {
            Type = type ?? typeof(T);
        }

        public abstract bool Format(FormatContext context, T value, TextWriter writer);

        public Type Type { get; }

        public abstract string MimeType { get; }

        bool ITypeFormatter.Format(FormatContext context, object instance, TextWriter writer)
        {
            return Format(context, (T) instance, writer);
        }
    }
}