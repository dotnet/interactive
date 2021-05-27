// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Html;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.Formatting.TabularData
{
    internal class TabularDataFormatterSource : ITypeFormatterSource
    {
        public IEnumerable<ITypeFormatter> CreateTypeFormatters()
        {
            yield return new HtmlFormatter<TabularDataResource>((value, context) =>
            {
                IReadOnlyList<IHtmlContent> headers =
                    value.Schema
                         .Fields
                         .Select(f => (IHtmlContent) td(span(f.Name)))
                         .ToArray();

                IReadOnlyList<IHtmlContent> rows =
                    value.Data
                         .Select(d => (IHtmlContent) tr(d.Values.Select(v => td(v))))
                         .ToArray();

                Html.Table(headers, rows).WriteTo(context);
            });
        }
    }
}