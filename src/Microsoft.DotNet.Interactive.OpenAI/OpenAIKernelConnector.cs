// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SemanticKernel;

namespace Microsoft.DotNet.Interactive.OpenAI;

public class OpenAIKernelConnector
{
    public static async Task AddKernelConnector()
    {
        if (KernelInvocationContext.Current is { } context &&
            context.HandlingKernel.RootKernel is CompositeKernel root)
        {
            var config = new KernelConfig();

            root.AddKernelConnector(new ConnectOpenAICommand());
        }
    }
}