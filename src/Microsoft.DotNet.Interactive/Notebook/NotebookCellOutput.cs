// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Notebook
{
    public abstract class NotebookCellOutput
    {
    }

    public class NotebookCellDisplayOutput : NotebookCellOutput
    {
        public Dictionary<string, object> Data { get; }

        public NotebookCellDisplayOutput(Dictionary<string, object> data)
        {
            Data = data;
        }
    }

    public class NotebookCellErrorOutput : NotebookCellOutput
    {
        public string ErrorName { get; }
        public string ErrorValue { get; }
        public string[] StackTrace { get; }

        public NotebookCellErrorOutput(string errorName, string errorValue, string[] stackTrace)
        {
            ErrorName = errorName;
            ErrorValue = errorValue;
            StackTrace = stackTrace;
        }
    }

    public class NotebookCellTextOutput : NotebookCellOutput
    {
        public string Text { get; }

        public NotebookCellTextOutput(string text)
        {
            Text = text;
        }
    }
}
