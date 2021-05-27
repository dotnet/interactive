// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Linq;
using Microsoft.DotNet.Interactive.Formatting.TabularData;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal static class DefaultTabularDataFormatterSet
    {
        internal static readonly ITypeFormatter[] DefaultFormatters =
            {
                new TabularDataResourceFormatter<IEnumerable>((value, context) =>
                {
                    var tabularDataSet = value.Cast<object>().ToTabularDataResource();
                    var tabularData = tabularDataSet.ToJsonString();
                    context.Writer.Write(tabularData.ToString());
                    return true;
                }),

                new TabularDataResourceFormatter<TabularDataResource>((value, context) =>
                {
                    var tabularData = value.ToJsonString();
                    context.Writer.Write(tabularData);
                    return true;
                }),
            };
    }
}
