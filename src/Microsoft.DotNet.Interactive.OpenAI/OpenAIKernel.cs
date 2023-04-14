// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.OpenAI;

public class OpenAIKernel : 
    Kernel,
    IKernelCommandHandler<SubmitCode>
{
    public OpenAIKernel(string name = "openai") : base(name)
    {
        KernelInfo.LanguageName = "text";

        var configureCommand = new Command("#!configure");
        var useAzureOpenAIOption = new Option<bool>("--use-azure-openai", "Use Azure OpenAI");
        configureCommand.Add(useAzureOpenAIOption);
        configureCommand.SetHandler(async ctx =>
        {
            var parseResult = ctx.ParseResult;
            var useAzureOpenAI = parseResult.GetValueForOption(useAzureOpenAIOption);
            await Authenticate(useAzureOpenAI);
        });
        AddDirective(configureCommand);
    }

    private static async Task Authenticate(bool useAzureOpenAI)
    {
        await Settings.AskAzureEndpoint(useAzureOpenAI);
        await Settings.AskModel(useAzureOpenAI);
        await Settings.AskApiKey(useAzureOpenAI);
    }

    public Task HandleAsync(SubmitCode command, KernelInvocationContext context)
    {
        
    }
}