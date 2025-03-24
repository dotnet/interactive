// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.Documents.Jupyter;

internal static class InteractiveDocumentExtensions
{
    public static InteractiveDocument WithJupyterMetadata(
        this InteractiveDocument document,
        string language = "C#")
    {
        var (kernelName, canonicalLanguageName, langVersion, fileExtension) =
            language.ToLowerInvariant() switch
            {
                "c#" or "csharp" => ("csharp", "C#", "13.0", ".cs"),
                "f#" or "fsharp" => ("fsharp", "F#", "7.0", ".fs"),
                "powershell" or "pwsh" => ("pwsh", "PowerShell", "7.5", ".ps1"),
                _ => throw new ArgumentException($"Unrecognized language: {language}")
            };

        document.Metadata.GetOrAdd("kernelspec", _ => new Dictionary<string, object>())
                .MergeWith(new Dictionary<string, object>
                {
                    ["display_name"] = $".NET ({canonicalLanguageName})",
                    ["language"] = canonicalLanguageName,
                    ["name"] = $".net-{kernelName}"
                });

        document.Metadata.GetOrAdd("language_info", _ => new Dictionary<string, object>())
                .MergeWith(new Dictionary<string, object>
                {
                    ["file_extension"] = fileExtension,
                    ["mimetype"] = $"text/x-{kernelName}",
                    ["name"] = canonicalLanguageName,
                    ["pygments_lexer"] = kernelName,
                    ["version"] = langVersion
                });

        var kernelInfos = document.Metadata.GetOrAdd("polyglot_notebook", _ => new KernelInfoCollection());

        kernelInfos.DefaultKernelName = kernelName;
        kernelInfos.Add(new(kernelName));

        return document;
    }
}