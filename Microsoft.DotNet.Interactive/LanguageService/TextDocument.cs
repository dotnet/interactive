// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;

namespace Microsoft.DotNet.Interactive.LanguageService
{
    public class TextDocument
    {
        public string Uri { get; set; }

        public TextDocument(string uri)
        {
            Uri = uri;
        }

        public static TextDocument FromDocumentContents(string code)
        {
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(code));
            return new TextDocument($"data:text/plain;base64,{encoded}");
        }
    }
}
