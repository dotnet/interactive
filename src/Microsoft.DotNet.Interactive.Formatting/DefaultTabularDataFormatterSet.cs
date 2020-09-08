﻿// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal static class DefaultTabularDataFormatterSet
    {
        internal static readonly ITypeFormatter[] DefaultFormatters =
            new ITypeFormatter[]
            {
                new TabularDataFormatter<IEnumerable<object>>((context, source, writer) =>
                {
                    var tabularData = source.ToTabularJsonString();
                        writer.Write(tabularData.ToString());
                        return true;
                }),

                new TabularDataFormatter<TabularDataSet>((context, source, writer) =>
                {
                    var tabularData = source.ToJson();
                    writer.Write(tabularData);
                    return true;
                }),
            };
    }
}
