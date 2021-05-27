// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Notebook
{
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
}