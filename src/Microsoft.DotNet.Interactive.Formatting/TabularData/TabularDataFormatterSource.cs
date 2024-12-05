// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Formatting.Csv;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.Formatting.TabularData;

internal class TabularDataFormatterSource : ITypeFormatterSource
{
    public IEnumerable<ITypeFormatter> CreateTypeFormatters()
    {
        Formatter.SetPreferredMimeTypesFor(typeof(TabularDataResource), HtmlFormatter.MimeType, CsvFormatter.MimeType);

        yield return new HtmlFormatter<TabularDataResource>((value, context) => FormatHtml(context, value));

        yield return new JsonFormatter<TabularDataResource>((value, context) =>
        {
            var json = JsonSerializer.Serialize(value, TabularDataResourceFormatter.JsonSerializerOptions);

            context.Writer.Write(json);
        });

        yield return new TabularDataResourceFormatter<TabularDataResource>((value, context) =>
        {
            var json = JsonSerializer.Serialize(value, TabularDataResourceFormatter.JsonSerializerOptions);

            context.Writer.Write(json);
        });

        yield return new CsvFormatter<TabularDataResource>((value, context) =>
        {
            var columns = value.Schema.Fields.Select(f => f.Name).ToArray();

            return CsvFormatter<IEnumerable<IEnumerable<KeyValuePair<string, object>>>>.BuildTable(value.Data, _ => columns,
                rows =>
                {
                        
                    return rows.Select(row => columns.Select(c => row.First(rc => rc.Key == c).Value));
                }, context);
        });
    }

    internal static void FormatHtml(FormatContext context, TabularDataResource value)
    {
        context.RequireDefaultStyles();

        IReadOnlyList<IHtmlContent> headers =
            value.Schema
                 .Fields
                 .Select(f => (IHtmlContent)td(span(f.Name)))
                 .ToArray();

        var (rowData, remainingCount) = value.Data.TakeAndCountRemaining(Formatter.ListExpansionLimit, true);

        var rows =
            rowData
                .Select(d => (IHtmlContent)tr(d.Select(v => td(v.Value))))
                .ToList();

        if (remainingCount > 0)
        {
            rows.Add(tr(td[colspan: $"{headers.Count}"](i($"({remainingCount} more)"))));
        }

        Html.Table(headers, rows).WriteTo(context);
    }
}