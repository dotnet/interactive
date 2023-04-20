// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.Text;
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
    private readonly Command _conversationContextCommand = new("#!conversation-context");
    private readonly Argument<string[]> _conversationContextArgument = new("arguments");

    private readonly Command _useSkillsCommand = new("#!use-skills");
    private readonly Argument<string[]> _skillsArgument = new("pipeline");
    private readonly Option<bool> _usePlannerOption = new("--use-planner");

    private readonly TextEmbeddingGenerationKernel? _embeddingsKernel;
    private readonly Dictionary<string, string> _values = new();
    private string[]? _functionNamesForPipeline;
    private bool _usePlanner;
    private string[]? _contextForMemory;

    public TextCompletionKernel(
        IKernel semanticKernel,
        string name,
        string modelName,
        TextEmbeddingGenerationKernel? embeddingsKernel = null) : base($"{name}(text)")
    {
        _embeddingsKernel = embeddingsKernel;
        SemanticKernel = semanticKernel;
        KernelInfo.LanguageName = "text";
        KernelInfo.DisplayName = $"{Name} - {modelName}";

        _skillsArgument.AddCompletions(context => SemanticKernel.GetFunctionNames());

        _useSkillsCommand.Add(_skillsArgument);
        _useSkillsCommand.Add(_usePlannerOption);

        _useSkillsCommand.SetHandler(context =>
        {
            _functionNamesForPipeline = context.ParseResult.GetValueForArgument(_skillsArgument);
            _usePlanner = context.ParseResult.GetValueForOption(_usePlannerOption);
        });

        _conversationContextCommand.Add(_conversationContextArgument);

        _conversationContextCommand.SetHandler(context =>
        {
            _contextForMemory = context.ParseResult.GetValueForArgument(_conversationContextArgument);
        });

        AddDirective(_useSkillsCommand);

        AddDirective(_conversationContextCommand);

        this.UseValueSharing();

    }

    public IKernel SemanticKernel { get; }

    async Task IKernelCommandHandler<SubmitCode>.HandleAsync(
        SubmitCode submitCode,
        KernelInvocationContext context)
    {
        try
        {
            var skContext = SemanticKernel.CreateNewContext();

            skContext.Variables.Set(TextMemorySkill.CollectionParam, TextEmbeddingGenerationKernel.DefaultMemoryCollectionName);

            foreach (var (key, value) in _values)
            {
                skContext.Variables.Set(key, value);
            }

            skContext.Variables.Set("INPUT", submitCode.Code);

            var pipeline = new List<ISKFunction>();
            var hasFacts = false;
            if (_embeddingsKernel is not null && _contextForMemory?.Length > 1)
            {
                var sb = new StringBuilder();
                var uniqueFacts = new HashSet<string>();
                var factCount = 0;
                foreach (var memoryContext in _contextForMemory)
                {
                    var memories = 
                        SemanticKernel.Memory.SearchAsync(TextEmbeddingGenerationKernel.DefaultMemoryCollectionName,
                            memoryContext, 5);

                    await foreach (var memory in memories)
                    {
                        var fact = memory.Metadata.Text;
                        if (uniqueFacts.Add(fact))
                        {
                            if (!hasFacts)
                            {
                                hasFacts = true;
                                sb.AppendLine("""
                                    Given the following facts:

                                    """);
                            }
                            sb.AppendLine($"""
                            -{++factCount} {fact}

                            """);
                        }
                    }
                }

                if (hasFacts)
                {
                    sb.AppendLine("""
                                    then:

                                    """);
                }
                sb.AppendLine("{{$INPUT}}");
                var code = sb.ToString();
                var semanticFunction = SemanticKernel.CreateSemanticFunction(
                    code,
                    skillName: FixUpCharacters(Name),
                    functionName: "loadmemory");
                
                pipeline.Add(semanticFunction);

                string FixUpCharacters(string name)
                {
                    return name.Replace("(", "_").Replace(")", "_");
                }
            }

            else if (_functionNamesForPipeline is null || _functionNamesForPipeline.Length == 0)
            {
                var semanticFunction = SemanticKernel.CreateSemanticFunction("""
                    {{$INPUT}}
                    """);
                pipeline.Add(semanticFunction);
            }
            else
            {
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
        var maxSteps = 100;
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
        var value = command.FormattedValue?.Value ?? command.Value?.ToString();
        if (value is { })
        {
            _values[command.Name] = value;
        }

        return Task.CompletedTask;
    }
}