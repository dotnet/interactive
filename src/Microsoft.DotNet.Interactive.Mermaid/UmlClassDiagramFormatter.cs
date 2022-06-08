// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.Mermaid;

public class UmlClassDiagramFormatter : ITypeFormatterSource
{
    public IEnumerable<ITypeFormatter> CreateTypeFormatters()
    {
        yield return new HtmlFormatter<UmlClassDiagram>((value, context) =>
        {
            var markDown = value.ToMarkdown();
            markDown.FormatTo(context,HtmlFormatter.MimeType);
        });
    }
}