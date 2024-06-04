// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;

#nullable enable

namespace Microsoft.DotNet.Interactive.Directives;

public class ChooseKernelDirective : Command
{
    public ChooseKernelDirective(Kernel kernel, string? description = null) :
        base($"#!{kernel.Name}",
            description ?? $"Run the code that follows using the {kernel.Name} kernel.")
    {
        Kernel = kernel;
        Handler = CommandHandler.Create((InvocationContext ctx) => Handle(ctx.GetService<KernelInvocationContext>(), ctx));
    }

    public Kernel Kernel { get; }

    protected virtual Task Handle(KernelInvocationContext kernelInvocationContext, InvocationContext commandLineInvocationContext)
    {
        return Task.CompletedTask;
    }
}