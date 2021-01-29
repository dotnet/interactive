// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Notebook;

namespace Microsoft.DotNet.Interactive.Events
{
    public class NotebookParsed : KernelEvent
    {
        public NotebookDocument Notebook { get; }

        public NotebookParsed(NotebookDocument notebook, KernelCommand command)
            : base(command)
        {
            Notebook = notebook;
        }
    }
}
