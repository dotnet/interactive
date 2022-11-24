// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.Csv;
using Microsoft.DotNet.Interactive.Formatting.TabularData;

namespace Microsoft.DotNet.Interactive;

[TypeFormatterSource(typeof(DataExplorerFormatterSource))]
public abstract class DataExplorer<TData>
{
    static DataExplorer()
    {
        RegisterFormatters();
    }

    public string Id { get; } = Guid.NewGuid().ToString("N");

    public TData Data { get; }

    protected DataExplorer(TData data)
    {
        Data = data;
    }

    public static void RegisterFormatters()
    {
        Formatter.Register<DataExplorer<TData>>((explorer, writer) =>
        {
            explorer.ToHtml().WriteTo(writer, HtmlEncoder.Default);
        }, HtmlFormatter.MimeType);

        // TODO: (RegisterFormatters) this should go somewhere else
        Formatter.SetPreferredMimeTypesFor(
            typeof(DataExplorer<TabularDataResource>),
            HtmlFormatter.MimeType,
            CsvFormatter.MimeType);
    }

    protected abstract IHtmlContent ToHtml();

    public static void Register<TDataExplorer>() where TDataExplorer : DataExplorer<TData>
    {
        DataExplorer.Register(typeof(TData), typeof(TDataExplorer));
    }
}