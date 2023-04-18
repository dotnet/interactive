// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SemanticKernel;

namespace Microsoft.DotNet.Interactive.OpenAI.Tests;

public static class KernelBuilder
{
    public static IKernel BuildSemanticKernel()
    {
        var config = new KernelConfig();

        config.AddTextCompletionService("mock", kernel => new MockTextCompletion());

        var semanticKernel = new SemanticKernel.KernelBuilder()
                             .WithConfiguration(config)
                             .Build();

        return semanticKernel;
    }
}