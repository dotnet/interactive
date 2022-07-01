// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Documents
{
    public class InteractiveDocument
    {
        public InteractiveDocument(IList<InteractiveDocumentElement> elements)
        {
            Elements = elements ?? new List<InteractiveDocumentElement>();
        }

        public IList<InteractiveDocumentElement> Elements { get; }
    }
}
