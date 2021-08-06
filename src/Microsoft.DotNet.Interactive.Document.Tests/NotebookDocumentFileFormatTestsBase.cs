// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Document.Tests
{
    public abstract class NotebookDocumentFileFormatTestsBase
    {
        public Dictionary<string, string> KernelLanguageAliases { get; }

        protected NotebookDocumentFileFormatTestsBase()
        {
            KernelLanguageAliases = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                ["fsharp"] = "fsharp",
                ["fs"] = "fsharp",
                ["f#"] = "fsharp",
                ["csharp"] = "csharp",
                ["cs"] = "csharp",
                ["c#"] = "csharp",
                ["powershell"] = "pwsh",
                ["pwsh"] = "pwsh",
                ["markdown"] = "markdown", 
                ["md"] = "markdown"

            };
        }
    }
}