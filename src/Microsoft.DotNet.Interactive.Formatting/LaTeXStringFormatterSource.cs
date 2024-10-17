// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Formatting;

internal class LaTeXStringFormatterSource : ITypeFormatterSource
{
    public IEnumerable<ITypeFormatter> CreateTypeFormatters()
    {
        Formatter.SetPreferredMimeTypesFor(typeof(LaTeXString), "text/latex");

        yield return new LaTeXStringFormatter();
    }

    private class LaTeXStringFormatter : TypeFormatter<LaTeXString>
    {
        public override string MimeType => "text/latex";

        public override bool Format(LaTeXString value, FormatContext context)
        {
            context.Writer.Write(value.ToString());
            return true;
        }
    }
}