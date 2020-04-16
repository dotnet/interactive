// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.App.Lsp
{
    public enum MarkupKind
    {
        Plaintext,
        Markdown,
    }

    internal static class MarkupKindExtensions
    {
        public static MarkupKind ConvertMarkupKind(this LanguageService.MarkupKind markupKind)
        {
            return markupKind switch
            {
                LanguageService.MarkupKind.Markdown => MarkupKind.Markdown,
                LanguageService.MarkupKind.Plaintext => MarkupKind.Plaintext,
                _ => throw new ArgumentOutOfRangeException(nameof(markupKind)),
            };
        }
    }
}
