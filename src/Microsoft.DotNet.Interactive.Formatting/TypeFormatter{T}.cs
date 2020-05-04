// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public abstract class TypeFormatter<T> : ITypeFormatter<T>
    {
        public abstract void Format(T instance, TextWriter writer);

        public Type Type => typeof(T);

        public abstract string MimeType { get; }

        void ITypeFormatter.Format(object instance, TextWriter writer)
        {
            Format((T) instance, writer);
        }
    }
}