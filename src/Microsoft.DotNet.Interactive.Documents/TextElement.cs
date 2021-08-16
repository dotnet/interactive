// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Documents
{
    public class TextElement : InteractiveDocumentOutputElement
    {
        public string Text { get; }

        public TextElement(string text)
        {
            Text = text;
        }
    }
}