// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Documents
{
    public partial class InteractiveDocument
    {
        public InteractiveDocumentElement[] Elements { get; }

        public InteractiveDocument(InteractiveDocumentElement[] elements)
        {
            Elements = elements;
        }
    }
}
