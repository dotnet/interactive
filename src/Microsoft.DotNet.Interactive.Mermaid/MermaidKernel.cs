// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Mermaid;

public class MermaidKernel : Kernel,
                             IKernelCommandHandler<SubmitCode>
{
    public MermaidKernel() : base("mermaid")
    {
        KernelInfo.LanguageName = "Mermaid";
        KernelInfo.Description = """
                                 Render diagrams using the Mermaid language (https://mermaid.js.org/intro)
                                 """;
    }

    Task IKernelCommandHandler<SubmitCode>.HandleAsync(
        SubmitCode command,
        KernelInvocationContext context)
    {
        command.Parameters.TryGetValue("--display-width", out var width);
        command.Parameters.TryGetValue("--display-height", out var height);
        command.Parameters.TryGetValue("--display-background-color", out var background);

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

    public override KernelSpecifierDirective KernelSpecifierDirective
    {
        get
        {
            var directive = base.KernelSpecifierDirective;

            directive.Parameters.Add(new(
                                         "--display-width",
                                         description: "Specify width for the display."));

            directive.Parameters.Add(new(
                                         "--display-height",
                                         description: "Specify height for the display."));

            directive.Parameters.Add(new(
                                         "--display-background-color",
                                         description: "Specify background color for the display."));

            return directive;
        }
    }
}