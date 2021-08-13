// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Documents
{
    public class InteractiveDocumentElement
    {
        public string Language { get; }
        public string Contents { get; }
        public InteractiveDocumentOutputElement[] Outputs { get; }

        public InteractiveDocumentElement(string language, string contents, InteractiveDocumentOutputElement[] outputs = null)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(language));
            }
            Language = language;
            Contents = contents;
            Outputs = outputs ?? Array.Empty<InteractiveDocumentOutputElement>();
        }
    }
}
