// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive;

internal class MessageDiagnosticsFormatterSource : ITypeFormatterSource
{
    public IEnumerable<ITypeFormatter> CreateTypeFormatters()
    {
        yield return new PlainTextFormatter<RoutingSlip>((slip, context) =>
        {
            var list = slip.Entries.Select(e => e.ToString());

            list.FormatTo(context, PlainTextFormatter.MimeType);

            return true;
        });

        yield return new HtmlFormatter<RoutingSlip>((slip, context) =>
        {
            var list = slip.Entries.Select(e => e.ToString());

            list.FormatTo(context, HtmlFormatter.MimeType);

            return true;
        });
    }
}