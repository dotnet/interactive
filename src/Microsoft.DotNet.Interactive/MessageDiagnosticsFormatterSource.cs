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
        yield return new HtmlFormatter<KernelCommandResult>((result, context) =>
        {
            // FIX: (CreateTypeFormatters) plain text

            // if (result.KernelEventsInternal.IsCompleted)
            // {
            //     var KernelEvents = result.KernelEvents.ToEnumerable().ToArray();
            //
            //     var displayObj = new
            //     {
            //         KernelEvents,
            //         result.Command
            //     };
            //
            //     displayObj.FormatTo(context, HtmlFormatter.MimeType);
            // }

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