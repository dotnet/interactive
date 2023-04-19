// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.CoreSkills;
using Microsoft.SemanticKernel.Orchestration;

namespace Microsoft.DotNet.Interactive.OpenAI;

public class TextCompletionKernel :
    Kernel,
    IKernelCommandHandler<SubmitCode>,
    IKernelCommandHandler<SendValue>
{
    private readonly Command _promptCommand = new("#!prompt");
    private readonly Dictionary<string, string> _values = new();
    private readonly Argument<string[]> _functionPipelineArgument = new("pipeline");
    private readonly Option<bool> _usePlannerOption = new("--use-planner");
    private string[] _functionNamesForPipeline;
    private bool _usePlanner;

    public TextCompletionKernel(
        IKernel semanticKernel,
        string name,
        string modelName) : base($"{name}(text)")
    {
        SemanticKernel = semanticKernel;
        KernelInfo.LanguageName = "text";
        KernelInfo.DisplayName = $"{Name} - {modelName}";

        _functionPipelineArgument.AddCompletions(context => SemanticKernel.GetFunctionNames());

        _promptCommand.Add(_functionPipelineArgument);
        _promptCommand.Add(_usePlannerOption);

        _promptCommand.SetHandler(context =>
        {
            _functionNamesForPipeline = context.ParseResult.GetValueForArgument(_functionPipelineArgument);
            _usePlanner = context.ParseResult.GetValueForOption(_usePlannerOption);
        });

        AddDirective(_promptCommand);

        this.UseValueSharing();
    }

    public IKernel SemanticKernel { get; }

    async Task IKernelCommandHandler<SubmitCode>.HandleAsync(SubmitCode submitCode, KernelInvocationContext context)
    {
        try
        {
            var pipeline = new List<ISKFunction>();
            var skContext = SemanticKernel.CreateNewContext();
            // var contextVariables = new ContextVariables();
            skContext.Variables.Set("INPUT", submitCode.Code);
            skContext.Variables.Set(TextMemorySkill.CollectionParam, TextEmbeddingGenerationKernel.DefaultMemoryCollectionName);

            if (_functionNamesForPipeline is null || _functionNamesForPipeline.Length == 0)
            {
                var semanticFunction = SemanticKernel.CreateSemanticFunction("{{$INPUT}}");
                pipeline.Add(semanticFunction);
            }
            else
            {
                foreach (var (key, value) in _values)
                {
                    skContext.Variables.Set(key, value);
                }

                foreach (var functionName in _functionNamesForPipeline)
                {
                    var parts = functionName.Split(".");

                    var skill = parts[1];
                    var function = parts[2];

                    pipeline.Add(SemanticKernel.Skills.GetFunction(skill, function));
                }
            }

            if (_usePlanner)
            {
                await ExecutePlan(submitCode, context);
            }
            else
            {
                await ExecutePrompt(submitCode, context, skContext.Variables, pipeline);
            }
        }
        finally
        {
            _functionNamesForPipeline = null;
        }
    }

    private async Task ExecutePrompt(
        SubmitCode submitCode,
        KernelInvocationContext context,
        ContextVariables contextVariables,
        List<ISKFunction> pipeline)
    {
        var semanticKernelResponse = await SemanticKernel.RunAsync(
                                         contextVariables,
                                         context.CancellationToken,
                                         pipeline.ToArray());

        var plainTextValue = new FormattedValue(PlainTextFormatter.MimeType,
                                                semanticKernelResponse.Result.ToDisplayString(PlainTextFormatter.MimeType));

        var htmlValue = new FormattedValue(HtmlFormatter.MimeType,
                                           semanticKernelResponse.ToDisplayString(HtmlFormatter.MimeType));

        var formattedValues = new[]
        {
            plainTextValue,
            htmlValue
        };

        context.Publish(new ReturnValueProduced(semanticKernelResponse, submitCode, formattedValues));
    }

    private async Task ExecutePlan(SubmitCode submitCode, KernelInvocationContext context)
    {
        var planner = SemanticKernel.ImportSkill(new PlannerSkill(SemanticKernel));
        var plan = await SemanticKernel.RunAsync(submitCode.Code, planner["CreatePlan"]);
        var currentPlanStepContext = plan;
        var step = 1;
        var maxSteps = 10;
        while (!currentPlanStepContext.Variables.ToPlan().IsComplete && step < maxSteps)
        {
            var results = await SemanticKernel.RunAsync(currentPlanStepContext.Variables, planner["ExecutePlan"]);
            var currentStepPlan = results.Variables.ToPlan();
            if (currentStepPlan.IsSuccessful)
            {
                if (currentStepPlan.IsComplete)
                {
                    var plainTextValue = new FormattedValue(PlainTextFormatter.MimeType,
                                                            currentStepPlan.Result.ToDisplayString(PlainTextFormatter.MimeType));

                    var htmlValue = new FormattedValue(HtmlFormatter.MimeType,
                                                       currentStepPlan.ToDisplayString(HtmlFormatter.MimeType));

                    var formattedValues = new[]
                    {
                        plainTextValue,
                        htmlValue
                    };

                    context.Publish(new ReturnValueProduced(currentStepPlan, submitCode, formattedValues));
                    break;
                }
            }
            else
            {
                context.Fail(submitCode, message: $"Step {step} - Execution failed: {currentStepPlan.Result}");
                break;
            }

            currentPlanStepContext = results;
            step++;
        }
    }

    public Task HandleAsync(SendValue command, KernelInvocationContext context)
    {
        _values[command.Name] = command.FormattedValue?.Value ?? command.Value?.ToString();

        return Task.CompletedTask;
    }
}