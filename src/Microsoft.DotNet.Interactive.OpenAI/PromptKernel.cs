// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.ValueSharing;
using Microsoft.SemanticKernel;

namespace Microsoft.DotNet.Interactive.OpenAI;

public class PromptKernel :
    OpenAIKernel,
    IKernelCommandHandler<RequestValueInfos>
{
    private readonly Command _functionCommand = new("#!function", "Indicates that the cell contents should be used to define a semantic function.");

    private readonly Option<string> _skillNameOption = new("--skill", "The name of the skill to which the function should be added.");

    private readonly Argument<string> _functionNameArgument = new("functionName", "The name of the function to be defined.");
    private string? _currentSkillName;
    private string _currentFunctionName;

    public PromptKernel(
        IKernel semanticKernel,
        string name) : base(semanticKernel, name, SubmissionHandlingType.Prompt)
    {
        _functionCommand.Add(_skillNameOption);
        _functionCommand.Add(_functionNameArgument);

        _functionCommand.SetHandler(context =>
        {
            _currentSkillName = context.ParseResult.GetValueForOption(_skillNameOption);
            _currentFunctionName = context.ParseResult.GetValueForArgument(_functionNameArgument);
        });

        AddDirective(_functionCommand);
    }

    protected override Task HandleSubmitCode(SubmitCode submitCode, KernelInvocationContext context)
    {
        try
        {
            SemanticKernel.CreateSemanticFunction(
                submitCode.Code,
                _currentFunctionName,
                _currentSkillName);
        }
        finally
        {
            _currentSkillName = null;
            _currentFunctionName = null;
        }

        return Task.CompletedTask;
    }

    public Task HandleAsync(RequestValueInfos command, KernelInvocationContext context)
    {
        var valueInfos = new List<KernelValueInfo>();

        var functionsView = SemanticKernel.Skills.GetFunctionsView();

        foreach (var (key, value) in functionsView.SemanticFunctions.Concat(functionsView.NativeFunctions))
        {
            foreach (var functionView in value)
            {
                valueInfos.Add(new KernelValueInfo(
                                   $"function.{key}.{functionView.Name}",
                                   FormattedValue.FromObject(functionView, PlainTextSummaryFormatter.MimeType)
                                                 .FirstOrDefault()));
            }
        }

        context.Publish(new ValueInfosProduced(valueInfos, command));

        return Task.CompletedTask;
    }
}

internal static class SemanticKernelExtensions
{
    public static IEnumerable<string> GetFunctionNames(this IKernel kernel)
    {
        var functionsView = kernel.Skills.GetFunctionsView();

        foreach (var functionView in functionsView
                                     .SemanticFunctions.Concat(functionsView.NativeFunctions)
                                     .SelectMany(p => p.Value))
        {
            yield return $"function.{functionView.SkillName}.{functionView.Name}";
        }
    }
}