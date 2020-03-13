// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.LanguageService
{
    public class TextDocumentHoverResponse : LspResponse
    {
        public MarkupContent Contents { get; set; }
        public Range Range { get; set; }
    }
}
