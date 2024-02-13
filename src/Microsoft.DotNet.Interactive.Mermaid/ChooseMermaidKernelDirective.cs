// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using Microsoft.DotNet.Interactive.Directives;

namespace Microsoft.DotNet.Interactive.Mermaid;

internal class ChooseMermaidKernelDirective : ChooseKernelDirective
{
    public ChooseMermaidKernelDirective(MermaidKernel kernel) : base(kernel, "Render mermaid markdown.")
    {
        Add(HeightOption);
        Add(WidthOption);
        Add(BackgroundOption);
    }

    public Option<string> WidthOption { get; } = new(
        "--display-width",
        description: "Specify width for the display.",
        getDefaultValue: () => "");

    public Option<string> HeightOption { get; } = new(
        "--display-height",
        description: "Specify height for the display.",
        getDefaultValue: () => "");

    public Option<string> BackgroundOption { get; } = new(
        "--display-background-color",
        description: "Specify background color for the display.",
        getDefaultValue: () => "white");

}