// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.SemanticKernel;

namespace Microsoft.DotNet.Interactive.OpenAI;

public class OpenAIKernelConnector
{
    public static async Task AddKernelConnector()
    {
        if (KernelInvocationContext.Current is { } context &&
            context.HandlingKernel.RootKernel is CompositeKernel root)
        {
            var config = new KernelConfig();

            root.AddKernelConnector(new ConnectOpenAICommand());
        }
    }
}

public class ConnectOpenAICommand : ConnectKernelCommand
{
    public ConnectOpenAICommand() : base("openai", "Connects a kernel that can be used to run OpenAI prompts")
    {
    }

    public Option<>

    public override async Task<Kernel> ConnectKernelAsync(
        KernelInvocationContext context,
        InvocationContext commandLineContext)
    {
        var name = commandLineContext.ParseResult.GetValueForOption(KernelNameOption);




        return new OpenAIKernel();
    }
}