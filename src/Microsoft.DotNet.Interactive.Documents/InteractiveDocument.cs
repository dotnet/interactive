// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Documents.ParserServer;

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

    internal void NormalizeElementKernelNames(KernelInfoCollection kernelInfos)
    {
        var defaultKernelName = GetDefaultKernelName(kernelInfos);

        foreach (var element in Elements)
        {
            if (element.InferredTargetKernelName is not null &&
                kernelInfos.TryGetByAlias(element.InferredTargetKernelName, out var byMagic))
            {
                element.KernelName = byMagic.Name;
            }

            if (element.KernelName is null)
            {
                element.KernelName = defaultKernelName;
            }

            if (element.KernelName is not null &&
                kernelInfos.TryGetByAlias(element.KernelName, out var n))
            {
                element.KernelName = n.Name;
            }
        }
    }

    public string? GetDefaultKernelName()
    {
        if (TryGetKernelInfoFromMetadata(Metadata, out var kernelInfo))
        {
            return kernelInfo.DefaultKernelName;
        }

        return null;
    }

    internal string? GetDefaultKernelName(KernelInfoCollection kernelInfos)
    {
        string? defaultKernelName = null;

        if (Metadata is null)
        {
            return null;
        }

        if (Metadata.TryGetValue("kernelspec", out var kernelspecObj))
        {
            if (kernelspecObj is IDictionary<string, object> kernelspecDict)
            {
                if (kernelspecDict.TryGetValue("language", out var languageObj) &&
                    languageObj is string defaultLanguage)
                {
                    return defaultLanguage;
                }
            }
        }

        if (kernelInfos.DefaultKernelName is { } defaultFromKernelInfos)
        {
            if (kernelInfos.TryGetByAlias(defaultFromKernelInfos, out var info))
            {
                return info.Name;
            }
        }

        return defaultKernelName;
    }

    internal static bool TryGetKernelInfoFromMetadata(
        IDictionary<string, object>? metadata,
        [NotNullWhen(true)] out KernelInfoCollection? kernelInfo)
    {
        if (metadata?.TryGetValue("kernelInfo", out var kernelInfoObj) == true &&
            kernelInfoObj is JsonElement kernelInfoJson && kernelInfoJson.Deserialize<KernelInfoCollection>(ParserServerSerializer.JsonSerializerOptions) is
                { } kernelInfoDeserialized)
        {
            kernelInfo = kernelInfoDeserialized;
            return true;
        }

        kernelInfo = null;
        return false;
    }
}