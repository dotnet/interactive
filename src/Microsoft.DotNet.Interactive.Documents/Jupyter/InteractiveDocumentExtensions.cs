// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Documents.Utility;

namespace Microsoft.DotNet.Interactive.Documents.Jupyter;

public static class InteractiveDocumentExtensions
{
    public static InteractiveDocument WithJupyterMetadataIfNotSet(this InteractiveDocument document)
    {
        document.Metadata.GetOrAdd("kernelspec", _ => new Dictionary<string, object>())
                .MergeWith(new Dictionary<string, object>
                {
                    ["display_name"] = ".NET (C#)",
                    ["language"] = "C#",
                    ["name"] = ".net-csharp"
                });

        document.Metadata.GetOrAdd("language_info", _ => new Dictionary<string, object>())
                .MergeWith(new Dictionary<string, object>
                {
                    ["file_extension"] = ".cs",
                    ["mimetype"] = "text/x-csharp",
                    ["name"] = "C#",
                    ["pygments_lexer"] = "csharp",
                    ["version"] = "8.0"
                });

        return document;
    }
}