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
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Threading.Tasks;
using Stream = System.IO.Stream;

namespace Microsoft.DotNet.Interactive.Jupyter.CommandEvents;

internal class CommCommandEventAdapterConfiguration : IJupyterKernelConfiguration
{
    private static string TargetName = "dotnet_coe_adapter_comm";
    private static IReadOnlyDictionary<string, string> _commDefinitions = GetCommDefinitions();
    private readonly CommsManager _commsManager;

    public CommCommandEventAdapterConfiguration(CommsManager commsManager)
    {
        _commsManager = commsManager ?? throw new ArgumentNullException(nameof(commsManager));
    }

    public async Task<bool> ApplyAsync(JupyterKernel kernel)
    {
        var adapter = await GetCommandEventAdapter(kernel);

        if (adapter is not null)
        {
            kernel.RegisterCommandHandler<RequestValue>(adapter.HandleAsync);
            kernel.RegisterCommandHandler<RequestValueInfos>(adapter.HandleAsync);
            kernel.RegisterCommandHandler<SendValue>(adapter.HandleAsync);
            kernel.UseValueSharing();
            kernel.UseWho();

            kernel.RegisterForDisposal(adapter);

            return true;
        }

        return false;
    }

    private async Task<CommCommandEventAdapter> GetCommandEventAdapter(JupyterKernel kernel)
    {
        string language = kernel.KernelInfo.LanguageName?.ToLowerInvariant();
        if (_commDefinitions.TryGetValue(language, out string definition))
        {
            var initialized = await kernel.RunOnKernelAsync(definition);
            if (!initialized)
            {
                return null; // don't try to create value adapter if we failed
            }

            var adapter = await CreateCommandEventAdapterAsync(kernel);
            return adapter;
        }

        return null;
    }

    private async Task<CommCommandEventAdapter> CreateCommandEventAdapterAsync(JupyterKernel kernel)
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
                    return new CommCommandEventAdapter(agent);
                }
            }
        }

        return null;
    }

    private static IReadOnlyDictionary<string, string> GetCommDefinitions()
    {
        var assembly = Assembly.GetAssembly(typeof(CommCommandEventAdapterConfiguration));
        string resourceNamePrefix = $"{assembly.GetName().Name}.CommandEvents.KernelAdapters.";
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
