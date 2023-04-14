// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.SemanticKernel;

namespace Microsoft.DotNet.Interactive.OpenAI;

public class OpenAIKernel :
    Kernel,
    IKernelCommandHandler<SubmitCode>
{
    private readonly IKernel _semanticKernel;

    public OpenAIKernel(IKernel semanticKernel, string name, SubmissionHandlingType submissionHandlingType) : this($"{name}:{LabelFor(submissionHandlingType)}")
    {
        _semanticKernel = semanticKernel;
    }

    private static string LabelFor(SubmissionHandlingType submissionHandlingType) =>
        submissionHandlingType switch
        {
            SubmissionHandlingType.TextCompletion => "text",
            SubmissionHandlingType.ChatCompletion => "chat",
            SubmissionHandlingType.TextEmbeddingGeneration => "embedding",
            SubmissionHandlingType.ImageGeneration => "image",
            SubmissionHandlingType.Skill => "skill",
            _ => throw new ArgumentOutOfRangeException(nameof(submissionHandlingType), submissionHandlingType, null)
        };

    private OpenAIKernel(string name) : base(name)
    {
        KernelInfo.LanguageName = "text";
    }

    public async Task HandleAsync(SubmitCode command, KernelInvocationContext context)
    {
        var semanticFunction = _semanticKernel.CreateSemanticFunction("{{$INPUT}}");

        var semanticKernelResponse = await _semanticKernel.RunAsync(
                         command.Code,
                         context.CancellationToken,
                         semanticFunction);

        var plainTextValue = new FormattedValue("text/plain", semanticKernelResponse.Result.ToDisplayString("text/plain"));
        var htmlValue = new FormattedValue("text/html", semanticKernelResponse.ToDisplayString("text/html"));

        var formattedValues = new[]
        {
            plainTextValue,
            htmlValue
        };

        context.Publish(new ReturnValueProduced(semanticKernelResponse, command, formattedValues));
    }
}

public enum SubmissionHandlingType
{
    TextEmbeddingGeneration,
    ChatCompletion,
    TextCompletion,
    ImageGeneration,
    Skill
}