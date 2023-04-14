// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reactive.Disposables;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.OpenAI.Configuration;
using Microsoft.DotNet.Interactive.Tests.LanguageServices;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.OpenAI.Tests
{
    public class KernelConfigurationTests : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly string _azureOpenAIConfigFilePath;
        private readonly string _serviceId;
        private readonly string _kernelName;

        public KernelConfigurationTests()
        {
            _serviceId = $"chatgpt-{Guid.NewGuid():N}";
            _kernelName = $"AzureOpenAI({_serviceId})";

            _azureOpenAIConfigFilePath = SemanticKernelSettings.GetSettingsFilePathForKernelName(_kernelName);

            Directory.CreateDirectory(OpenAIKernelConnector.SettingsPath);

            var settings = new SemanticKernelSettings
            {
                TextCompletionServiceSettings = new()
                {
                    [_serviceId] = new()
                    {
                        ApiKey = "something-s00per-s33kr1t",
                        Endpoint = "https://my-openai-things.openai.azure.com/",
                        UseAzureOpenAI = true,
                        ModelOrDeploymentName = "text-davinci-003"
                    }
                }
            };

            var settingsJson = JsonSerializer.Serialize(settings);

            File.WriteAllText(_azureOpenAIConfigFilePath, settingsJson);

            _disposables.Add(Disposable.Create(() => File.Delete(_azureOpenAIConfigFilePath)));
        }

        [Fact]
        public void Well_known_kernel_name_loads_existing_configuration()
        {
            var success = SemanticKernelSettings.TryLoadFromFile(
                _azureOpenAIConfigFilePath,
                out var settings);

            success.Should().BeTrue();
            settings.Should().NotBeNull();

            var config = settings!.CreateKernelConfig();

            config.AllTextCompletionServiceIds.Should().Contain(_serviceId);
        }

        [Fact]
        public async Task When_there_is_no_well_known_AzureOpenAI_configuration_then_user_is_prompted_and_config_is_saved_to_a_file()
        {
            using var kernel = new CompositeKernel();
            OpenAIKernelConnector.AddKernelConnectorTo(kernel);
            kernel.RegisterCommandHandler<RequestInput>((command, context) =>
            {
                context.Publish(new InputProduced($"value-for-{command.ValueName}", command));
                return Task.CompletedTask;
            });

            kernel.SetDefaultTargetKernelNameForCommand(typeof(RequestInput), kernel.Name);

            var kernelName = $"{nameof(When_there_is_no_well_known_AzureOpenAI_configuration_then_user_is_prompted_and_config_is_saved_to_a_file)}-{Guid.NewGuid():N}";

            DeleteSettingsFileForKernelName(kernelName);

            var result = await kernel.SendAsync(new SubmitCode($"#!connect openai --use-azure-openai --kernel-name {kernelName}"));

            result.Events.Should().NotContainErrors();

            using var _ = new AssertionScope();

            SemanticKernelSettings.TryLoadFromFile(SemanticKernelSettings.GetSettingsFilePathForKernelName(kernelName), out var settings).Should().BeTrue();

            settings.TextCompletionServiceSettings[kernelName].Endpoint.Should().Be("value-for-endpoint");
            settings.TextCompletionServiceSettings[kernelName].ModelOrDeploymentName.Should().Be("value-for-deploymentName");
            settings.TextCompletionServiceSettings[kernelName].ApiKey.Should().Be("value-for-apiKey");
            settings.TextCompletionServiceSettings[kernelName].OrgId.Should().BeNull();
            settings.TextCompletionServiceSettings[kernelName].UseAzureOpenAI.Should().BeTrue();
        }

        [Fact]
        public async Task When_there_is_no_well_known_OpenAI_configuration_then_user_is_prompted_and_config_is_saved_to_a_file()
        {
            using var kernel = new CompositeKernel();
            OpenAIKernelConnector.AddKernelConnectorTo(kernel);
            kernel.RegisterCommandHandler<RequestInput>((command, context) =>
            {
                context.Publish(new InputProduced($"value-for-{command.ValueName}", command));
                return Task.CompletedTask;
            });

            kernel.SetDefaultTargetKernelNameForCommand(typeof(RequestInput), kernel.Name);

            var kernelName = $"{nameof(When_there_is_no_well_known_OpenAI_configuration_then_user_is_prompted_and_config_is_saved_to_a_file)}-{Guid.NewGuid():N}";

            DeleteSettingsFileForKernelName(kernelName);

            var result = await kernel.SendAsync(new SubmitCode($"#!connect openai --kernel-name {kernelName}"));

            result.Events.Should().NotContainErrors();

            using var _ = new AssertionScope();

            SemanticKernelSettings.TryLoadFromFile(SemanticKernelSettings.GetSettingsFilePathForKernelName(kernelName), out var settings).Should().BeTrue();

            settings.TextCompletionServiceSettings[kernelName].Endpoint.Should().BeNull();
            settings.TextCompletionServiceSettings[kernelName].ModelOrDeploymentName.Should().Be("value-for-modelName");
            settings.TextCompletionServiceSettings[kernelName].ApiKey.Should().Be("value-for-apiKey");
            settings.TextCompletionServiceSettings[kernelName].OrgId.Should().Be("value-for-orgId");
            settings.TextCompletionServiceSettings[kernelName].UseAzureOpenAI.Should().BeFalse();
        }

        private void DeleteSettingsFileForKernelName(string kernelName)
        {
            _disposables.Add(Disposable.Create(() =>
            {
                try
                {
                    File.Delete(SemanticKernelSettings.GetSettingsFilePathForKernelName(kernelName));
                }
                catch (Exception)
                {
                }
            }));
        }

        [Fact]
        public async Task connect_magic_suggest_previously_configured_kernel_names()
        {
            using var kernel = new CompositeKernel();

            OpenAIKernelConnector.AddKernelConnectorTo(kernel);

            var code = "#!connect openai --kernel-name [||]".ParseMarkupCode();

            var command = new RequestCompletions(
                code.Code,
                new LinePosition(0, code.Span.End));

            var result = await kernel.SendAsync(command);

            result.Events.Should().NotContainErrors();

            result.Events
                  .Should()
                  .ContainSingle<CompletionsProduced>()
                  .Which
                  .Completions
                  .Should()
                  .ContainSingle(c => c.DisplayText == _kernelName);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}