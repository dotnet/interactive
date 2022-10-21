// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Formatting.TabularData;

internal static class DefaultTabularDataFormatterSet
{
    internal static readonly ITypeFormatter[] DefaultFormatters =
    {
        new TabularDataResourceFormatter<IEnumerable>((value, context) =>
        {
            var tabularDataSet = value.Cast<object>().ToTabularDataResource();
            var tabularData = tabularDataSet.ToJsonString();
            context.Writer.Write(tabularData.ToString());
        })
    };
}