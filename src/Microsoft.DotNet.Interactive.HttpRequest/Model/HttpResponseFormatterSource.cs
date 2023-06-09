// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal sealed class HttpResponseFormatterSource : ITypeFormatterSource
{
    IEnumerable<ITypeFormatter> ITypeFormatterSource.CreateTypeFormatters()
    {
        yield return new HttpResponseHtmlFormatter();
        yield return new HttpResponsePlainTextFormatter();
    }

    private sealed class HttpResponseHtmlFormatter : TypeFormatter<HttpResponse>
    {
        public override string MimeType => HtmlFormatter.MimeType;

        public override bool Format(HttpResponse value, FormatContext context)
        {
            if (value is null)
            {
                context.Writer.Write($"<pre>{nameof(HttpResponse)}: null</pre>");
            }
            else
            {
                value.FormatAsHtml(context);
            }

            return true;
        }
    }

    private sealed class HttpResponsePlainTextFormatter : TypeFormatter<HttpResponse>
    {
        public override string MimeType => PlainTextFormatter.MimeType;

        public override bool Format(HttpResponse value, FormatContext context)
        {
            if (value is null)
            {
                context.Writer.Write($"{nameof(HttpResponse)}: null");
            }
            else
            {
                value.FormatAsPlainText(context);
            }

            return true;
        }
    }
}
