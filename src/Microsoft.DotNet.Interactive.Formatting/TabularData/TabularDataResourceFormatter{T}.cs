// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Formatting.TabularData
{
    public class TabularDataResourceFormatter<T> : TypeFormatter<T>
    {
        private readonly FormatDelegate<T> _format;

        public TabularDataResourceFormatter(FormatDelegate<T> format)
        {
            _format = format;
        }

        public override bool Format(T value, FormatContext context)
        {
            return _format(value, context);
        }

        public override string MimeType => TabularDataResourceFormatter.MimeType;
    }
}