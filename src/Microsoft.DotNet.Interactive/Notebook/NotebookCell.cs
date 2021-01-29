// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.DotNet.Interactive.Notebook
{
    public class NotebookCell
    {
        public string Language { get; }
        public string Contents { get; }
        public NotebookCellOutput[] Outputs { get; }

        public NotebookCell(string language, string contents, NotebookCellOutput[] outputs = null)
        {
            Language = language;
            Contents = contents;
            Outputs = (outputs ?? Array.Empty<NotebookCellOutput>());
        }
    }
}
