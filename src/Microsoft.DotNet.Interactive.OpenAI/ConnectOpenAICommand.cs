// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.OpenAI.Configuration;
using Microsoft.Extensions.Logging;
using Pocket.For.MicrosoftExtensionsLogging;
using static Microsoft.DotNet.Interactive.OpenAI.Configuration.SemanticKernelSettings;

namespace Microsoft.DotNet.Interactive.OpenAI;

public class ConnectOpenAICommand : ConnectKernelCommand
{
    public ConnectOpenAICommand() : base("openai", "Connects a kernel that can be used to run OpenAI prompts")
    {
        KernelNameOption.AddCompletions(_ => Directory.GetFiles(OpenAIKernelConnector.SettingsPath).Select(Path.GetFileNameWithoutExtension)!);

        Add(UseAzureOpenAIOption);
    }

    public Option<bool> UseAzureOpenAIOption { get; } = new("--use-azure-openai", "Use Azure OpenAI");

    public override async Task<Kernel> ConnectKernelAsync(
        KernelInvocationContext context,
        InvocationContext commandLineContext)
    {
        if (context.HandlingKernel.RootKernel is not CompositeKernel rootKernel)
        {
            throw new InvalidOperationException("The root kernel must be a CompositeKernel");
        }

        var kernelName = commandLineContext.ParseResult.GetValueForOption(KernelNameOption)!;

        var settingsFile = GetSettingsFilePathForKernelName(kernelName);

        if (!TryLoadFromFile(settingsFile, out var settings))
        {
            var useAzureOpenAI = commandLineContext.ParseResult.GetValueForOption(UseAzureOpenAIOption);

            settings = new SemanticKernelSettings();

            settings.TextCompletionServiceSettings = new()
            {
                [kernelName] = new TextCompletionServiceSettings
                {
                    Endpoint = useAzureOpenAI
                                   ? await Kernel.GetInputAsync(
                                         "Please enter your Azure OpenAI endpoint",
                                         valueName: "endpoint")
                                   : null,
                    UseAzureOpenAI = useAzureOpenAI,
                    ModelOrDeploymentName =
                        useAzureOpenAI
                            ? await Kernel.GetInputAsync(
                                  "Please enter your Azure OpenAI deployment name",
                                  valueName: "deploymentName")
                            : await Kernel.GetInputAsync(
                                  "Please enter the OpenAI model name",
                                  valueName: "modelName"),

                    ApiKey =
                        useAzureOpenAI
                            ? await Kernel.GetPasswordAsync(
                                  "Please enter your Azure OpenAI API key",
                                  valueName: "apiKey")
                            : await Kernel.GetPasswordAsync(
                                  "Please enter your OpenAI API key",
                                  valueName: "apiKey"),
                    OrgId = useAzureOpenAI
                                ? null
                                : await Kernel.GetInputAsync(
                                      "Please enter your OpenAI organization ID",
                                      valueName: "orgId"),
                }
            };

            WriteSettingsFile(settings, settingsFile);

            context.Display($"""
        A settings file was created: {settingsFile}

        You can edit this file to add additional OpenAI services.
        """);
        }

        var config = settings.CreateKernelConfig();

        var semanticKernel = SemanticKernel.Kernel
                                           .Builder
                                           .WithConfiguration(config)
                                           .WithLogger(new LoggerFactory().AddPocketLogger().CreateLogger<OpenAIKernel>())
                                           .Build();

        if (config.AllChatCompletionServiceIds.Any())
        {
            rootKernel.Add(new OpenAIKernel(semanticKernel, kernelName, SubmissionHandlingType.ChatCompletion));
        }

        if (config.AllTextCompletionServiceIds.Any())
        {
            rootKernel.Add(new OpenAIKernel(semanticKernel, kernelName, SubmissionHandlingType.TextCompletion));
        }

        if (config.AllTextEmbeddingGenerationServiceIds.Any())
        {
            rootKernel.Add(new OpenAIKernel(semanticKernel, kernelName, SubmissionHandlingType.TextEmbeddingGeneration));
        }

        if (config.ImageGenerationServices.Any())
        {
            rootKernel.Add(new OpenAIKernel(semanticKernel, kernelName, SubmissionHandlingType.ImageGeneration));
        }

        return new OpenAIKernel(semanticKernel, kernelName, SubmissionHandlingType.Skill);
    }
}