// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Documents.Utility;

namespace Microsoft.DotNet.Interactive.Documents.Jupyter;

public static class InteractiveDocumentExtensions
{
    public static InteractiveDocument WithJupyterMetadata(
        this InteractiveDocument document,
        string language = "C#")
    {
        var (moniker, langVersion, fileExtension) =
            language switch
            {
                "C#" or "csharp" => ("csharp", "10.0", ".cs"),
                "F#" or "fsharp" => ("fsharp", "6.0", ".fs"),
                "PowerShell" or "pwsh" => ("powershell", "7.0", ".ps1"),
                _ => throw new ArgumentException($"Unrecognized language: {language}") 
            };

        document.Metadata.GetOrAdd("kernelspec", _ => new Dictionary<string, object>())
                .MergeWith(new Dictionary<string, object>
                {
                    ["display_name"] = $".NET ({language})",
                    ["language"] = language,
                    ["name"] = $".net-{moniker}"
                });

        document.Metadata.GetOrAdd("language_info", _ => new Dictionary<string, object>())
                .MergeWith(new Dictionary<string, object>
                {
                    ["file_extension"] = fileExtension,
                    ["mimetype"] = $"text/x-{moniker}",
                    ["name"] = language,
                    ["pygments_lexer"] = moniker,
                    ["version"] = langVersion
                });

        return document;
    }
}