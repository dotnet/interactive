// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public class TabularDataFormatter<T> : TypeFormatter<T>
    {
        private readonly Func<FormatContext, T, TextWriter, bool> _format;

        public TabularDataFormatter(Func<FormatContext, T, TextWriter, bool> format)
        {
            _format = format;
        }

        public TabularDataFormatter(Action<T, TextWriter> format)
        {
            _format = (context, instance, writer) => { format(instance, writer); return true; };
        }

        public TabularDataFormatter(Func<T, string> format)
        {
            _format = (context, instance, writer) => { writer.Write(format(instance)); return true; };
        }

        public override bool Format(FormatContext context, T value, TextWriter writer)
        {

            return _format(context, value, writer);
        }

        public override string MimeType => TabularDataFormatter.MimeType;
    }
}