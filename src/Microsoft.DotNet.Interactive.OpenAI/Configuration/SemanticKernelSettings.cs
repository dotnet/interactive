// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.SemanticKernel;

namespace Microsoft.DotNet.Interactive.OpenAI.Configuration;

public class SemanticKernelSettings
{
    public Dictionary<string /* serviceId */, TextCompletionServiceSettings> TextCompletionServiceSettings { get; set; } = new();
    public Dictionary<string /* serviceId */, TextEmbeddingGenerationServiceSettings> TextEmbeddingGenerationServiceSettings { get; set; } = new();
    public Dictionary<string /* serviceId */, ChatCompletionServiceSettings> ChatCompletionServiceSettings { get; set; } = new();
    public Dictionary<string /* serviceId */, ImageGenerationServiceSettings> ImageGenerationServiceSettings { get; set; } = new();

    public static bool TryLoadFromFile(
        string configFilePath,
        [NotNullWhen(returnValue: true)] out SemanticKernelSettings? settings)
    {
        if (!File.Exists(configFilePath))
        {
            settings = null;
            return false;
        }

        settings = JsonDocument.Parse(File.ReadAllText(configFilePath)).Deserialize<SemanticKernelSettings>()!;

        return true;
    }

    public KernelConfig CreateKernelConfig()
    {
        var config = new KernelConfig();

        foreach (var settings in TextCompletionServiceSettings)
        {
            if (settings.Value.UseAzureOpenAI)
            {
                config.AddAzureOpenAITextCompletionService(
                    settings.Key,
                    settings.Value.ModelOrDeploymentName,
                    settings.Value.Endpoint,
                    settings.Value.ApiKey);
            }
            else
            {
                config.AddOpenAITextCompletionService(
                    settings.Key,
                    settings.Value.ModelOrDeploymentName,
                    settings.Value.ApiKey,
                    settings.Value.OrgId);
            }
        }

        foreach (var settings in TextEmbeddingGenerationServiceSettings)
        {
            if (settings.Value.UseAzureOpenAI)
            {
                config.AddAzureOpenAIEmbeddingGenerationService(
                    settings.Key,
                    settings.Value.ModelOrDeploymentName,
                    settings.Value.Endpoint,
                    settings.Value.ApiKey);
            }
            else
            {
                config.AddOpenAIEmbeddingGenerationService(
                    settings.Key,
                    settings.Value.ModelOrDeploymentName,
                    settings.Value.ApiKey,
                    settings.Value.OrgId);
            }
        }

        foreach (var settings in ChatCompletionServiceSettings)
        {
            config.AddOpenAIChatCompletionService(
                settings.Key,
                settings.Value.ModelOrDeploymentName,
                settings.Value.ApiKey,
                settings.Value.OrgId);
        }

        foreach (var settings in ImageGenerationServiceSettings)
        {
            config.AddOpenAIImageGenerationService(
                settings.Key,
                settings.Value.ApiKey,
                settings.Value.OrgId);
        }

        return config;
    }

    public static string GetSettingsFilePathForKernelName(string kernelName)
    {
        return Path.Combine(OpenAIKernelConnector.SettingsPath, kernelName + ".json");
    }

    public static void WriteSettingsFile(SemanticKernelSettings settings, string getSettingsFilePathForKernelName)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(getSettingsFilePathForKernelName, json);
    }
}