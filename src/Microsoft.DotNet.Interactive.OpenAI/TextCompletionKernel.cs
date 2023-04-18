// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;

namespace Microsoft.DotNet.Interactive.OpenAI;

public class TextCompletionKernel :
    OpenAIKernel,
    IKernelCommandHandler<SendValue>
{
    private readonly Command _promptCommand = new("#!prompt");
    private readonly Dictionary<string, string> _values = new();
    private readonly Argument<string[]> _functionPipelineArgument = new("pipeline");
    private string[] _functionNamesForPipeline;

    public TextCompletionKernel(
        IKernel semanticKernel,
        string name) : base(semanticKernel, name, SubmissionHandlingType.TextCompletion)
    {
        _functionPipelineArgument.AddCompletions(context => SemanticKernel.GetFunctionNames());

        _promptCommand.Add(_functionPipelineArgument);

        _promptCommand.SetHandler(context => { _functionNamesForPipeline = context.ParseResult.GetValueForArgument(_functionPipelineArgument); });

        AddDirective(_promptCommand);
    }

    protected override async Task HandleSubmitCode(SubmitCode submitCode, KernelInvocationContext context)
    {
        try
        {
            var pipeline = new List<ISKFunction>();
            var contextVariables = new ContextVariables();
            contextVariables.Set("INPUT", submitCode.Code);

            if (_functionNamesForPipeline is null || _functionNamesForPipeline.Length == 0)
            {
                var semanticFunction = SemanticKernel.CreateSemanticFunction("{{$INPUT}}");
                pipeline.Add(semanticFunction);
            }
            else
            {
                foreach (var (key, value) in _values)
                {
                    contextVariables.Set(key, value);
                }

                foreach (var functionName in _functionNamesForPipeline)
                {
                    var parts = functionName.Split(".");

                    var skill = parts[1];
                    var function = parts[2];

                    pipeline.Add(SemanticKernel.Skills.GetFunction(skill, function));
                }
            }

            var semanticKernelResponse = await SemanticKernel.RunAsync(
                                             contextVariables,
                                             context.CancellationToken,
                                             pipeline.ToArray());

            var plainTextValue = new FormattedValue(PlainTextFormatter.MimeType, semanticKernelResponse.Result.ToDisplayString(PlainTextFormatter.MimeType));

            var htmlValue = new FormattedValue(HtmlFormatter.MimeType, semanticKernelResponse.ToDisplayString(HtmlFormatter.MimeType));

            var formattedValues = new[]
            {
                plainTextValue,
                htmlValue
            };

            context.Publish(new ReturnValueProduced(semanticKernelResponse, submitCode, formattedValues));
        }
        finally
        {
            _functionNamesForPipeline = null;
        }
    }

    public Task HandleAsync(SendValue command, KernelInvocationContext context)
    {
        _values[command.Name] = command.FormattedValue?.Value ?? command.Value?.ToString();

        return Task.CompletedTask;
    }
}