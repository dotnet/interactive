// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.SemanticKernel;

namespace Microsoft.DotNet.Interactive.OpenAI;

public abstract class OpenAIKernel :
    Kernel,
    IKernelCommandHandler<SubmitCode>
{
    public IKernel SemanticKernel { get; }

    protected OpenAIKernel(
        IKernel semanticKernel, 
        string name, 
        SubmissionHandlingType submissionHandlingType) : base($"{name}:{LabelFor(submissionHandlingType)}")
    {
        SemanticKernel = semanticKernel;
        KernelInfo.LanguageName = "text";
    }

    private static string LabelFor(SubmissionHandlingType submissionHandlingType) =>
        submissionHandlingType switch
        {
            SubmissionHandlingType.TextCompletion => "text",
            SubmissionHandlingType.ChatCompletion => "chat",
            SubmissionHandlingType.TextEmbeddingGeneration => "embedding",
            SubmissionHandlingType.ImageGeneration => "image",
            SubmissionHandlingType.Prompt => "Prompt",
            _ => throw new ArgumentOutOfRangeException(nameof(submissionHandlingType), submissionHandlingType, null)
        };

    protected abstract Task HandleSubmitCode(SubmitCode submitCode, KernelInvocationContext context);

    public async Task HandleAsync(SubmitCode submitCode, KernelInvocationContext context)
    {
        await HandleSubmitCode(submitCode, context);
    }
}