// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using Microsoft.DotNet.Interactive.Directives;

namespace Microsoft.DotNet.Interactive.SqlServer;

public class ChooseMsSqlKernelDirective : ChooseKernelDirective
{
    public ChooseMsSqlKernelDirective(Kernel kernel) : base(kernel, $"Run a T-SQL query using the \"{kernel.Name}\" connection.")
    {
        Add(NameOption);
    }

    public Option<string> NameOption { get; } = new(
        "--name",
        description: "Specify the value name to store the results.",
        getDefaultValue: () => "lastResults");

}