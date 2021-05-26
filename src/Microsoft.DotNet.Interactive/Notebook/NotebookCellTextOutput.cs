// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Notebook
{
    public class NotebookCellTextOutput : NotebookCellOutput
    {
        public string Text { get; }

        public NotebookCellTextOutput(string text)
        {
            Text = text;
        }
    }
}