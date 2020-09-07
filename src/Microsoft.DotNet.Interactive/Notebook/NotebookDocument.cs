// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.DotNet.Interactive.Notebook
{
    public class NotebookDocument
    {
        public IReadOnlyCollection<NotebookCell> Cells { get; }

        public NotebookDocument(IEnumerable<NotebookCell> cells)
        {
            Cells = cells.ToImmutableArray();
        }
    }
}
