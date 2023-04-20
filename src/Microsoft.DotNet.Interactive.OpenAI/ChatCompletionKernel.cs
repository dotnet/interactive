// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.CoreSkills;

namespace Microsoft.DotNet.Interactive.OpenAI;

public class ChatCompletionKernel :
    Kernel,
    IKernelCommandHandler<SubmitCode>,
    IKernelCommandHandler<SendValue>
{
    private ChatHistory? _chatHistory;
    private IChatCompletion? _chatCompletionService;
    private readonly Dictionary<string, string> _values = new();

    public ChatCompletionKernel(
        IKernel semanticKernel,
        string name,
        string modelName) : base($"{name}(chat)")
    {
        SemanticKernel = semanticKernel;
        KernelInfo.LanguageName = "text";
        KernelInfo.DisplayName = $"{Name} - {modelName}";
    }

    public IKernel SemanticKernel { get; }

    async Task IKernelCommandHandler<SubmitCode>.HandleAsync(SubmitCode submitCode, KernelInvocationContext context)
    {
        _chatCompletionService ??= SemanticKernel.GetService<IChatCompletion>();
        _chatHistory ??= _chatCompletionService.CreateNewChat();
        
        var skContext = SemanticKernel.CreateNewContext();
        skContext.Variables.Set(TextMemorySkill.CollectionParam, TextEmbeddingGenerationKernel.DefaultMemoryCollectionName);

        foreach (var (key, value) in _values)
        {
            skContext.Variables.Set(key, value);
        }

        _chatHistory.AddMessage(ChatHistory.AuthorRoles.User, submitCode.Code);

        var reply = await _chatCompletionService.GenerateMessageAsync(_chatHistory, new(), context.CancellationToken);

        context.Publish(new ReturnValueProduced(reply, submitCode, FormattedValue.CreateManyFromObject(reply, PlainTextFormatter.MimeType)));
    }

    public Task HandleAsync(SendValue command, KernelInvocationContext context)
    {
        var value = command.FormattedValue?.Value ?? command.Value?.ToString();
        if (value is { })
        {
            _values[command.Name] = value;
        }

        return Task.CompletedTask;
    }
}