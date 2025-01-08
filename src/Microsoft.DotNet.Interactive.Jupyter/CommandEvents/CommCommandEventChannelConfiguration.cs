// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.Messaging.Comms;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Threading.Tasks;
using Stream = System.IO.Stream;

namespace Microsoft.DotNet.Interactive.Jupyter.CommandEvents;

internal class CommCommandEventChannelConfiguration : IJupyterKernelConfiguration
{
    private static string TargetName = "dotnet_coe_handler_comm";
    private static IReadOnlyDictionary<string, string> _commDefinitions = GetCommDefinitions();
    private readonly CommsManager _commsManager;

    public CommCommandEventChannelConfiguration(CommsManager commsManager)
    {
        _commsManager = commsManager ?? throw new ArgumentNullException(nameof(commsManager));
    }

    public async Task<bool> ApplyAsync(JupyterKernel kernel)
    {
        var channel = await GetCommandEventChannelAsync(kernel);

        if (channel is not null)
        {
            kernel.RegisterCommandHandler<RequestValue>(channel.HandleAsync);
            kernel.RegisterCommandHandler<RequestValueInfos>(channel.HandleAsync);
            kernel.RegisterCommandHandler<SendValue>(channel.HandleAsync);
            kernel.UseValueSharing();
            kernel.UseWho();

            kernel.RegisterForDisposal(channel);

            return true;
        }

        return false;
    }

    private async Task<CommCommandEventChannel> GetCommandEventChannelAsync(JupyterKernel kernel)
    {
        string language = kernel.KernelInfo.LanguageName?.ToLowerInvariant();
        if (_commDefinitions.TryGetValue(language, out string definition))
        {
            await kernel.RunOnKernelAsync(definition);

            return await CreateCommandEventChannelAsync();
        }

        return null;
    }

    private async Task<CommCommandEventChannel> CreateCommandEventChannelAsync()
    {
        var agent = await _commsManager.OpenCommAsync(TargetName);

        if (agent is not null)
        {
            var response = await agent.Messages
                .TakeUntilMessageType(JupyterMessageContentTypes.CommMsg, JupyterMessageContentTypes.CommClose)
                .ToTask();

            if (response is CommMsg messageReceived)
            {
                var payload = CommandEventCommEnvelop.FromDataDictionary(messageReceived.Data);
                if (payload.EventEnvelope.Event is KernelReady)
                {
                    return new CommCommandEventChannel(agent);
                }
            }
        }

        return null;
    }

    private static IReadOnlyDictionary<string, string> GetCommDefinitions()
    {
        var assembly = Assembly.GetAssembly(typeof(CommCommandEventChannelConfiguration));
        string resourceNamePrefix = $"{assembly.GetName().Name}.CommandEvents.LanguageHandlers.";
        var resourcePaths = assembly.GetManifestResourceNames()
            .Where(str => str.StartsWith(resourceNamePrefix));

        var commDefinitions = new Dictionary<string, string>();

        foreach (var commDefinitionPath in resourcePaths)
        {
            // resource path needs to be {resourceNamePrefix}.{languageName}.{resourceFileWithCommDefinition}
            var sections = commDefinitionPath.Remove(0, resourceNamePrefix.Length).Split(".");

            using (Stream stream = assembly.GetManifestResourceStream(commDefinitionPath))
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();
                commDefinitions.Add(sections[0], result);
            }
        }

        return commDefinitions;
    }
}
