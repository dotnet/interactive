// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.AI.ImageGeneration;

namespace Microsoft.DotNet.Interactive.OpenAI;

public class OpenAIKernel :
    Kernel,
    IKernelCommandHandler<SubmitCode>
{
    private readonly IKernel _semanticKernel;
    private readonly SubmissionHandlingType _submissionHandlingType;
    private ChatHistory? _chatHistory;
    private IChatCompletion? _chatCompletionService;
    private IImageGeneration? _imageGenerationService;

    public OpenAIKernel(
        IKernel semanticKernel, 
        string name, 
        SubmissionHandlingType submissionHandlingType) : base($"{name}:{LabelFor(submissionHandlingType)}")
    {
        _semanticKernel = semanticKernel;
        _submissionHandlingType = submissionHandlingType;
        KernelInfo.LanguageName = "text";
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

    public async Task HandleAsync(SubmitCode submitCode, KernelInvocationContext context)
    {
        switch (_submissionHandlingType)
        {
            case SubmissionHandlingType.TextCompletion:
                var semanticFunction = _semanticKernel.CreateSemanticFunction("{{$INPUT}}");

                var semanticKernelResponse = await _semanticKernel.RunAsync(
                                                 submitCode.Code,
                                                 context.CancellationToken,
                                                 semanticFunction);

                var plainTextValue = new FormattedValue(PlainTextSummaryFormatter.MimeType, semanticKernelResponse.Result.ToDisplayString(PlainTextSummaryFormatter.MimeType));

                var htmlValue = new FormattedValue(HtmlFormatter.MimeType, semanticKernelResponse.ToDisplayString(HtmlFormatter.MimeType));

                var formattedValues = new[]
                {
                    plainTextValue,
                    htmlValue
                };

                context.Publish(new ReturnValueProduced(semanticKernelResponse, submitCode, formattedValues));
                break;

            case SubmissionHandlingType.ChatCompletion:

                _chatCompletionService ??= _semanticKernel.GetService<IChatCompletion>();
                _chatHistory ??= _chatCompletionService.CreateNewChat();

                _chatHistory.AddMessage("user", submitCode.Code);

                var reply = await _chatCompletionService.GenerateMessageAsync(_chatHistory, new(), context.CancellationToken);

                context.Publish(new ReturnValueProduced(reply, submitCode, FormattedValue.FromObject(reply, PlainTextFormatter.MimeType)));

                break;

            case SubmissionHandlingType.TextEmbeddingGeneration:
                break;

            case SubmissionHandlingType.ImageGeneration:

                _imageGenerationService ??= _semanticKernel.GetService<IImageGeneration>();

                var height = 256;
                var width = 256;
                var imageUrl = await _imageGenerationService.GenerateImageAsync(
                                   submitCode.Code,
                                   width, height);

                await SkiaUtils.ShowImage(imageUrl, width, height);

                break;

            case SubmissionHandlingType.Skill:
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}