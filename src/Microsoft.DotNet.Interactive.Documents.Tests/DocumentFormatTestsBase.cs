// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Documents.Tests
{
    public abstract class DocumentFormatTestsBase
    {
        public IReadOnlyList<KernelName> KernelLanguageAliases { get; }

        protected DocumentFormatTestsBase()
        {
            KernelLanguageAliases = new List<KernelName>
            {
                new("csharp", new[] { "cs", "C#", "c#" }),
                new("fsharp", new[] { "fs", "F#", "f#" }),
                new("pwsh", new[] { "powershell" }),
            };
        }
    }
}