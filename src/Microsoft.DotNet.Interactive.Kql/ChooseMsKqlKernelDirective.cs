// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using Microsoft.DotNet.Interactive.Directives;

namespace Microsoft.DotNet.Interactive.Kql;

public class ChooseMsKqlKernelDirective : ChooseKernelDirective
{
    public ChooseMsKqlKernelDirective(Kernel kernel) : base(kernel, $"Run a Kusto query using the \"{kernel.Name}\" connection.")
    {
        Add(NameOption);
    }

    public Option<string> NameOption { get; } = new(
        "--name",
        description: "Specify the value name to store the results.",
        getDefaultValue: () => "lastResults");

}