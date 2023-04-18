// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.SkillDefinition;

namespace Microsoft.DotNet.Interactive.OpenAI;

public abstract class OpenAIKernel :
    Kernel,
    IKernelCommandHandler<SubmitCode>
{
    static OpenAIKernel()
    {
        Formatter.Register<FunctionView>((value, context) =>
        {
            context.Writer.Write($"params: {string.Join(", ", value.Parameters.Select(p => p.Name))}");

            return true;
        }, PlainTextSummaryFormatter.MimeType);
        
    }

    public IKernel SemanticKernel { get; }

    protected OpenAIKernel(
        IKernel semanticKernel,
        string name,
        SubmissionHandlingType submissionHandlingType) : base($"{name}({LabelFor(submissionHandlingType)})")
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
            SubmissionHandlingType.Prompt => "prompt",
            _ => throw new ArgumentOutOfRangeException(nameof(submissionHandlingType), submissionHandlingType, null)
        };

    protected abstract Task HandleSubmitCode(SubmitCode submitCode, KernelInvocationContext context);

    public async Task HandleAsync(SubmitCode submitCode, KernelInvocationContext context)
    {
        await HandleSubmitCode(submitCode, context);
    }
}