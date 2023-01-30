// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.TabularData;

namespace Microsoft.DotNet.Interactive;

public class TabularDataResourceSummaryExplorer : DataExplorer<TabularDataResource>
{
    public TabularDataResourceSummaryExplorer(TabularDataResource data) : base(data)
    {
    }

    protected override IHtmlContent ToHtml()
    {
        return new HtmlString(Data.ToDisplayString(HtmlFormatter.MimeType));
    }
}