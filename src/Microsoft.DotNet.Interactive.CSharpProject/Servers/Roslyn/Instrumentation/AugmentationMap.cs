// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn.Instrumentation;

public class AugmentationMap 
{
    public Dictionary<SyntaxNode, Augmentation> Data { get; }

    public AugmentationMap(Dictionary<SyntaxNode, Augmentation> data = null)
    {
        Data = data ?? new Dictionary<SyntaxNode, Augmentation>();
    }

    public AugmentationMap(params Augmentation[] augmentations)
    {
        Data = new Dictionary<SyntaxNode, Augmentation>();
        foreach (var augmentation in augmentations)
        {
            Data[augmentation.AssociatedStatement] = augmentation;
        }
    }
}