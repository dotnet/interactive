// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public class TabularDataResourceFormatter<T> : TypeFormatter<T>
    {
        private readonly Func<FormatContext, T, TextWriter, bool> _format;

        public TabularDataResourceFormatter(Func<FormatContext, T, TextWriter, bool> format)
        {
            _format = format;
        }

        public override bool Format(T value, TextWriter writer, FormatContext context)
        {
            return _format(context, value, writer);
        }

        public override string MimeType => TabularDataResourceFormatter.MimeType;
    }
}