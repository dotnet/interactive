// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Directives;

namespace Microsoft.DotNet.Interactive.Connection;

/// <summary>
/// Defines a magic command that can be used to connect a subkernel dynamically.
/// </summary>
public abstract class ConnectKernelDirective<TCommand> : KernelActionDirective
    where TCommand : ConnectKernelCommand
{
    protected ConnectKernelDirective(
        string name,
        string description) :
        base(name)
    {
        Description = description;
        Parameters.Add(KernelNameParameter);
    }

    protected KernelDirectiveParameter KernelNameParameter = new("--kernel-name")
    {
        Description = "The name of the subkernel to be added",
        Required = true
    };

    protected void AddOption(KernelDirectiveParameter parameter)
    {
        // FIX: (AddOption) inline and remove this method
        Parameters.Add(parameter);
    }

    /// <summary>
    /// Description used for the kernel connected using this command.
    /// </summary>
    public string? ConnectedKernelDescription { get; set; }

    /// <summary>
    /// Creates a kernel instance when this connection command is invoked.
    /// </summary>
    /// <param name="connectCommand">A <see cref="KernelCommand"/> sent to connect one or more new kernels.</param>
    /// <param name="context">The <see cref="KernelInvocationContext"/> for the current command.</param>
    /// <returns>A new <see cref="Kernel"/> instance to be added to the <see cref="CompositeKernel"/>.</returns>
    public abstract Task<IEnumerable<Kernel>> ConnectKernelsAsync(
        TCommand connectCommand,
        KernelInvocationContext context);
}