// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public class InteractiveDocumentParsed : KernelEvent
    {
        public InteractiveDocument Document { get; }

        public InteractiveDocumentParsed(InteractiveDocument document, KernelCommand command)
            : base(command)
        {
            Document = document;
        }
    }
}
