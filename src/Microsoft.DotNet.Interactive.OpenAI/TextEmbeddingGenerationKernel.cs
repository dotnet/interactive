// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.SemanticKernel;

namespace Microsoft.DotNet.Interactive.OpenAI;

public class TextEmbeddingGenerationKernel : OpenAIKernel
{

    public TextEmbeddingGenerationKernel(IKernel semanticKernel,
        string name) : base(semanticKernel, name, SubmissionHandlingType.TextEmbeddingGeneration)
    {

    }

    protected override  Task HandleSubmitCode(SubmitCode submitCode, KernelInvocationContext context)
    {
        return Task.CompletedTask;
    }
}