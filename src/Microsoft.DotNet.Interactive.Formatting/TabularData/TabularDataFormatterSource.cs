// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Formatting.Csv;
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
                         .Select(f => (IHtmlContent)td(span(f.Name)))
                         .ToArray();

                IReadOnlyList<IHtmlContent> rows =
                    value.Data
                         .Select(d => (IHtmlContent)tr(d.Values.Select(v => td(v))))
                         .ToArray();

                Html.Table(headers, rows).WriteTo(context);
            });

            yield return new JsonFormatter<TabularDataResource>((value, context) =>
            {
                var json = JsonSerializer.Serialize(value.Data, TabularDataResourceFormatter.JsonSerializerOptions);

                context.Writer.Write(json);
            });

            yield return new TabularDataResourceFormatter<TabularDataResource>((value, context) =>
            {
                var json = JsonSerializer.Serialize(value, TabularDataResourceFormatter.JsonSerializerOptions);

                context.Writer.Write(json);
            });

            yield return new CsvFormatter<TabularDataResource>((value, context) =>
            {
                for (var i = 0; i < value.Data.Count; i++)
                {
                    var row = value.Data[i];

                    row.FormatTo(context, CsvFormatter.MimeType);
                }

                return true;
            });
        }
    }
}