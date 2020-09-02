// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Notebook;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class SerializeNotebook : KernelCommand
    {
        public string FileName { get; }
        public NotebookDocument Notebook { get; }
        public string NewLine { get; }

        public SerializeNotebook(string fileName, NotebookDocument notebook, string newLine, string targetKernelName = null)
            : base(targetKernelName)
        {
            FileName = fileName;
            Notebook = notebook;
            NewLine = newLine;
        }
    }
}
