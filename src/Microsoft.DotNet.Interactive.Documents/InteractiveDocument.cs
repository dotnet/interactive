// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Documents;

public class InteractiveDocument
{
    public InteractiveDocument(IList<InteractiveDocumentElement>? elements = null)
    {
        Elements = elements ?? new List<InteractiveDocumentElement>();
    }

    public IList<InteractiveDocumentElement> Elements { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, object>? Metadata { get; set; }

    internal void NormalizeElementLanguages(KernelNameCollection kernelNames)
    {
        string? notebookDefaultKernelName = null;

        if (Metadata?.TryGetValue("kernelspec", out var kernelspecObj) == true)
        {
            if (kernelspecObj is IDictionary<string, object> kernelspecDict)
            {
                if (kernelspecDict.TryGetValue("language", out var languageObj) &&
                    languageObj is string defaultLanguage)
                {
                    notebookDefaultKernelName = defaultLanguage;
                }
            }
        }

        if (notebookDefaultKernelName is null)
        {
            notebookDefaultKernelName = kernelNames.DefaultKernelName;
        }

        if (notebookDefaultKernelName is not null &&
            kernelNames.TryGetByAlias(notebookDefaultKernelName, out var name))
        {
            notebookDefaultKernelName = name.Name;
        }

        foreach (var element in Elements)
        {
            if (element.Language is null)
            {
                element.Language = notebookDefaultKernelName;
            }
            else
            {
                if (kernelNames.TryGetByAlias(element.Language, out var n))
                {
                    element.Language = n.Name;
                }
            }
        }
    }
}