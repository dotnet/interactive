// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Documents;

public class InteractiveDocument  : IEnumerable
{
    private IDictionary<string, object>? _metadata;

    public InteractiveDocument(IList<InteractiveDocumentElement>? elements = null)
    {
        Elements = elements ?? new List<InteractiveDocumentElement>();
    }

    public IList<InteractiveDocumentElement> Elements { get; set; }

    public IDictionary<string, object> Metadata => 
        _metadata ??= new Dictionary<string, object>();

    public IEnumerator GetEnumerator() => Elements.GetEnumerator();

    public void Add(InteractiveDocumentElement element) => Elements.Add(element);

    internal void NormalizeElementLanguages(KernelInfoCollection kernelInfos)
    {
        var notebookDefaultKernelName = GetNotebookDefaultKernelName(kernelInfos);

        foreach (var element in Elements)
        {
            if (element.InferredTargetKernelName is not null &&
                kernelInfos.TryGetByAlias(element.InferredTargetKernelName, out var byMagic))
            {
                element.Language = byMagic.Name;
            }

            if (element.Language is null)
            {
                element.Language = notebookDefaultKernelName;
            }

            if (element.Language is not null &&
                kernelInfos.TryGetByAlias(element.Language, out var n))
            {
                element.Language = n.Name;
            }
        }
    }

    internal string? GetNotebookDefaultKernelName(KernelInfoCollection kernelInfos)
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
            notebookDefaultKernelName = kernelInfos.DefaultKernelName;
        }

        if (notebookDefaultKernelName is not null &&
            kernelInfos.TryGetByAlias(notebookDefaultKernelName, out var name))
        {
            notebookDefaultKernelName = name.Name;
        }

        return notebookDefaultKernelName;
    }
}