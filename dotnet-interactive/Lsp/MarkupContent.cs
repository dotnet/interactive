// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.App.Lsp
{
    public class MarkupContent
    {
        public MarkupKind Kind { get; set; }
        public string Value { get; set; }

        public MarkupContent(MarkupKind kind, string value)
        {
            Kind = kind;
            Value = value;
        }

        public static MarkupContent FromLanguageServiceMarkupContent(LanguageService.MarkupContent contents)
        {
            return new MarkupContent(
                contents.Kind.ConvertMarkupKind(),
                contents.Value);
        }
    }
}
