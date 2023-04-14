// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.OpenAI;

public class ConnectOpenAICommand : ConnectKernelCommand
{
    public ConnectOpenAICommand() : base("openai", "Connects a kernel that can be used to run OpenAI prompts")
    {
    }

    public Option<bool> UseAzureOpenAIOption { get; } = new("--use-azure-openai", "Use Azure OpenAI");

    public override async Task<Kernel> ConnectKernelAsync(
        KernelInvocationContext context,
        InvocationContext commandLineContext)
    {
        var name = commandLineContext.ParseResult.GetValueForOption(KernelNameOption);

        // here we should do lookup for settings if we have them already?

        var useAzureOpenAI = commandLineContext.ParseResult.GetValueForOption(UseAzureOpenAIOption);

        var configFileName = new FileInfo($"semantic_kernel_config_{name}_{useAzureOpenAI}.config");
        string endpoint;
        string model;
        string? apiKey;
        if (!configFileName.Exists)
        {
            endpoint = await Settings.AskAzureEndpoint(useAzureOpenAI, configFileName.FullName);
            model = await Settings.AskModel(useAzureOpenAI, configFileName.FullName);
            apiKey = await Settings.AskApiKey(useAzureOpenAI, configFileName.FullName);
        }
        else
        {
            (useAzureOpenAI, model, endpoint, apiKey,_) = Settings.LoadFromFile();
        }

        var openAiKernel = new OpenAIKernel(name);

        openAiKernel.Configure(new OpenAIKernelSettings(model,endpoint,apiKey,useAzureOpenAI));

        return openAiKernel;
    }
}