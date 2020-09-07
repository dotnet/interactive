// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.DotNet.Interactive.Notebook
{
    public abstract class NotebookCellOutput
    {
    }

    public class NotebookCellDisplayOutput : NotebookCellOutput
    {
        public IReadOnlyDictionary<string, object> Data { get; }

        public NotebookCellDisplayOutput(IDictionary<string, object> data)
        {
            Data = (IReadOnlyDictionary<string, object>)data;
        }
    }

    public class NotebookCellErrorOutput : NotebookCellOutput
    {
        public string ErrorName { get; }
        public string ErrorValue { get; }
        public IReadOnlyCollection<string> StackTrace { get; }

        public NotebookCellErrorOutput(string errorName, string errorValue, IEnumerable<string> stackTrace)
        {
            ErrorName = errorName;
            ErrorValue = errorValue;
            StackTrace = stackTrace.ToImmutableArray();
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
