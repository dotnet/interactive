// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SemanticKernel;

namespace Microsoft.DotNet.Interactive.OpenAI;

public class TextEmbeddingGenerationKernel :
    KeyValueStoreKernel
{
    public TextEmbeddingGenerationKernel(
        IKernel semanticKernel,
        string name,
        string modelName) : base($"{name}(embedding)")
    {
        SemanticKernel = semanticKernel;
        KernelInfo.LanguageName = "text";
        KernelInfo.DisplayName = $"{Name} - {modelName}";
    }

    public IKernel SemanticKernel { get; }

    protected override async Task StoreValueAsync(
        string key,
        string value,
        string mimeType,
        bool shouldDisplayValue,
        KernelInvocationContext context)
    {
        await SemanticKernel.Memory.SaveInformationAsync(
            DefaultMemoryCollectionName,
            value, 
            key);
    }

    internal const string DefaultMemoryCollectionName = "default-memory-collection";
}