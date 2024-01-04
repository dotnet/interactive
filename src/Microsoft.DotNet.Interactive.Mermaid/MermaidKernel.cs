// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Mermaid;

public class MermaidKernel : Kernel, 
                             IKernelCommandHandler<SubmitCode>
{
    private ChooseMermaidKernelDirective? _chooseKernelDirective;

    public MermaidKernel() : base("mermaid")
    {
        KernelInfo.LanguageName = "Mermaid";
        KernelInfo.Description = """
                                 This Kernel uses the Mermaid library to render diagrams. 
                                 It supports the following diagrams types:
                                  - Flowchart
                                 - Sequence Diagram
                                 - Class Diagram
                                 - State Diagram
                                 - Entity Relationship Diagram
                                 - User Journey
                                 - Gantt
                                 - Pie Chart
                                 - Quadrant Chart
                                 - Requirement Diagram
                                 - Gitgraph (Git) Diagram
                                 - C4 Diagram
                                 - Mindmap
                                 - Timeline
                                 - Zenuml
                                 - Sankey
                                 - XYChart
                                 """;
    }

    Task IKernelCommandHandler<SubmitCode>.HandleAsync(SubmitCode command, KernelInvocationContext context)
    {
        string? width = null;
        string? height = null;
        string? background = null;
        if (ChooseKernelDirective is ChooseMermaidKernelDirective chooser)
        {
            width = command.KernelChooserParseResult?.GetValueForOption(chooser.WidthOption);
            height = command.KernelChooserParseResult?.GetValueForOption(chooser.HeightOption);
            background = command.KernelChooserParseResult?.GetValueForOption(chooser.BackgroundOption);
        }

        var markdown = new MermaidMarkdown(command.Code)
        {
            Width = width ?? string.Empty,
            Height = height ?? string.Empty,
            Background = string.IsNullOrWhiteSpace(background) ? "white" : background
        };

        var formattedValues = FormattedValue.CreateManyFromObject(markdown);
        
        context.Publish(
            new DisplayedValueProduced(
                markdown,
                command,
                formattedValues));
        
        return Task.CompletedTask;
    }

    public override ChooseKernelDirective ChooseKernelDirective => _chooseKernelDirective ??= new ChooseMermaidKernelDirective(this);
}