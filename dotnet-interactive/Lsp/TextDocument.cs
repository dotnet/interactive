// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.App.Lsp
{
    public class TextDocument
    {
        [JsonRequired]
        public string Uri { get; set; }

        public TextDocument(string uri)
        {
            Uri = uri;
        }

        public static TextDocument FromDocumentContents(string code)
        {
            return new TextDocument(RequestHoverTextCommand.MakeDataUriFromContents(code));
        }
    }
}
