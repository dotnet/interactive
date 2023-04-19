// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.ValueSharing;
using Microsoft.SemanticKernel;

namespace Microsoft.DotNet.Interactive.OpenAI;

public class SkillKernel :
    Kernel,
    IKernelCommandHandler<RequestValueInfos>,
    IKernelCommandHandler<SubmitCode>
{
    private readonly Command _functionCommand = new("#!function", "Indicates that the cell contents should be used to define a semantic function.");
    private readonly Option<string> _skillNameOption = new("--skill", "The name of the skill to which the function should be added.");
    private readonly Argument<string> _functionNameArgument = new("functionName", "The name of the function to be defined.");

    private string? _currentSkillName;
    private string _currentFunctionName;

    public SkillKernel(
        IKernel semanticKernel,
        string name) : base($"{name}(skill)")
    {
        SemanticKernel = semanticKernel;
        KernelInfo.LanguageName = "text";
        KernelInfo.DisplayName = $"{Name} - Define skills";

        _functionCommand.Add(_skillNameOption);
        _functionCommand.Add(_functionNameArgument);

        _functionCommand.SetHandler(context =>
        {
            _currentSkillName = context.ParseResult.GetValueForOption(_skillNameOption);
            _currentFunctionName = context.ParseResult.GetValueForArgument(_functionNameArgument);
        });

        AddDirective(_functionCommand);
    }

    public IKernel SemanticKernel { get; }

    Task IKernelCommandHandler<SubmitCode>.HandleAsync(SubmitCode submitCode, KernelInvocationContext context)
    {
        if (_currentFunctionName is null)
        {
            context.DisplayAs(
                $"Use the `{Name}` kernel to define a semantic function. You must give the function a name by calling the `{_functionCommand.Name}` magic command at the top of the cell.",
                "text/markdown");
            context.Fail(context.Command);
            return Task.CompletedTask;
        }

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