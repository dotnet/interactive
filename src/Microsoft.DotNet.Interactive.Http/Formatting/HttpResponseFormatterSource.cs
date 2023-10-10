// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.Http;

internal sealed class HttpResponseFormatterSource : ITypeFormatterSource
{
    IEnumerable<ITypeFormatter> ITypeFormatterSource.CreateTypeFormatters()
    {
        yield return new HttpResponseHtmlFormatter();
        yield return new HttpResponsePlainTextFormatter();
    }

    private sealed class HttpResponseHtmlFormatter : TypeFormatter<EmptyHttpResponse>
    {
        public override string MimeType => HtmlFormatter.MimeType;

        public override bool Format(EmptyHttpResponse value, FormatContext context)
        {
            value.FormatAsHtml(context);
            return true;
        }
    }

    private sealed class HttpResponsePlainTextFormatter : TypeFormatter<EmptyHttpResponse>
    {
        public override string MimeType => PlainTextFormatter.MimeType;

        public override bool Format(EmptyHttpResponse value, FormatContext context)
        {
            value.FormatAsPlainText(context);
            return true;
        }
    }
}
