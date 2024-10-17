// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive;

internal class ScriptContentFormatterSource : ITypeFormatterSource
{
    public IEnumerable<ITypeFormatter> CreateTypeFormatters()
    {
        yield return new ScriptContentFormatter();
    }

    internal class ScriptContentFormatter : TypeFormatter<ScriptContent>
    {
        public override string MimeType => "text/html";

        public override bool Format(ScriptContent script, FormatContext context)
        {
            IHtmlContent content =
                PocketViewTags.script[type: "text/javascript"](script.ScriptValue.ToHtmlContent());
            content.WriteTo(context.Writer, HtmlEncoder.Default);

            return true;
        }
    }
}