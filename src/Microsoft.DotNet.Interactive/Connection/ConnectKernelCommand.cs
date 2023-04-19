// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

#nullable enable
namespace Microsoft.DotNet.Interactive.Connection;

/// <summary>
/// Defines a magic command that can be used to connect a subkernel dynamically.
/// </summary>
public abstract class ConnectKernelCommand : Command
{
    protected ConnectKernelCommand(
        string name,
        string description) :
        base(name, description)
    {
        AddOption(KernelNameOption);
    }

    public Option<string> KernelNameOption = new(
        "--kernel-name",
        "The name of the subkernel to be added")
    {
        IsRequired = true
    };

    /// <summary>
    /// Description used for the kernel connected using this command.
    /// </summary>
    public string? ConnectedKernelDescription { get; set; }

    /// <summary>
    /// Creates a kernel instance when this connection command is invoked.
    /// </summary>
    /// <param name="context">The <see cref="KernelInvocationContext"/> for the current command.</param>
    /// <returns>A new <see cref="Kernel"/> instance to be added to the <see cref="CompositeKernel"/>.</returns>
    public abstract Task<IEnumerable<Kernel>> ConnectKernelsAsync(
        KernelInvocationContext context,
        InvocationContext commandLineContext);
}