// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Commands
{
    public class SerializeInteractiveDocument : KernelCommand
    {
        public string FileName { get; }
        public Documents.InteractiveDocument Document { get; }
        public string NewLine { get; }

        public SerializeInteractiveDocument(string fileName, Documents.InteractiveDocument document, string newLine, string targetKernelName = null)
            : base(targetKernelName)
        {
            FileName = fileName;
            Document = document;
            NewLine = newLine;
        }
    }
}
