// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Documents
{
    public class InteractiveDocumentElement
    {
        public InteractiveDocumentElement(
            string language,
            string contents,
            IList<InteractiveDocumentOutputElement>? outputs = null)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(language));
            }

            Language = language;
            Contents = contents;
            Outputs = outputs ?? new List<InteractiveDocumentOutputElement>();
        }

        public string Language { get; }

        public string Contents { get; }

        public IList<InteractiveDocumentOutputElement> Outputs { get; }
    }
}