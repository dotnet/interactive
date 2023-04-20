// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.OpenAI.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.CoreSkills;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.SkillDefinition;
using Pocket.For.MicrosoftExtensionsLogging;

namespace Microsoft.DotNet.Interactive.OpenAI;

public class OpenAIKernelConnector
{
    static OpenAIKernelConnector()
    {
        Formatter.Register<FunctionView>((value, context) =>
        {
            context.Writer.Write($"params: {string.Join(", ", value.Parameters.Select(p => p.Name))}");

            return true;
        }, PlainTextSummaryFormatter.MimeType);
    }

    public static void AddKernelConnectorToCurrentRootKernel()
    {
        if (KernelInvocationContext.Current is { } context &&
            context.HandlingKernel.RootKernel is CompositeKernel root)
        {
            AddKernelConnectorTo(root);

            context.DisplayAs("Added magic command `#!connect openai`.", "text/markdown");
        }
    }

    public static void AddKernelConnectorTo(CompositeKernel kernel)
    {
        kernel.AddKernelConnector(new ConnectOpenAICommand());
    }

    public static readonly string SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".net-interactive", "OpenAI");

    public static IEnumerable<Kernel> CreateKernels(
        SemanticKernelSettings settings,
        string kernelGroupName)
    {
        var config = settings.CreateKernelConfig();

        var kernelBuilder = SemanticKernel.Kernel
                                          .Builder
                                          .WithConfiguration(config)
                                          .WithLogger(new LoggerFactory().AddPocketLogger().CreateLogger<ConnectOpenAICommand>());

        if (config.AllTextEmbeddingGenerationServiceIds.Any())
        {
            kernelBuilder.WithMemoryStorage(new VolatileMemoryStore());
        }

        var semanticKernel = kernelBuilder.Build();

        var kernels = new List<Kernel>();

        if (config.AllChatCompletionServiceIds.Any())
        {
            kernels.Add(new ChatCompletionKernel(
                            semanticKernel,
                            kernelGroupName,
                            settings.ChatCompletionServiceSettings[kernelGroupName].ModelOrDeploymentName!));
        }

        TextEmbeddingGenerationKernel? embeddingsKernel = null;

        if (config.AllTextEmbeddingGenerationServiceIds.Any())
        {
            embeddingsKernel = new TextEmbeddingGenerationKernel(
                semanticKernel,
                kernelGroupName,
                settings.TextEmbeddingGenerationServiceSettings[kernelGroupName].ModelOrDeploymentName!);

            kernels.Add(embeddingsKernel);
        }

        if (config.AllTextCompletionServiceIds.Any())
        {
            kernels.Add(
                new TextCompletionKernel(
                    semanticKernel,
                    kernelGroupName,
                    settings.TextCompletionServiceSettings[kernelGroupName].ModelOrDeploymentName!,
                    embeddingsKernel));
        }

        if (config.ImageGenerationServices.Any())
        {
            kernels.Add(new ImageGenerationKernel(semanticKernel, kernelGroupName));
        }

        kernels.Add(new SkillKernel(semanticKernel, kernelGroupName));

        return kernels;
    }
}