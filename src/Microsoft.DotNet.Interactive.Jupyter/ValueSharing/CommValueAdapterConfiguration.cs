// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.Messaging.Comms;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing;

internal class CommValueAdapterConfiguration : IJupyterKernelConfiguration
{
    private static string TargetName = "value_adapter_comm";
    private readonly IReadOnlyDictionary<string, IValueAdapterCommDefinition> _commDefinitions;
    private readonly CommsManager _commsManager;

    public CommValueAdapterConfiguration(CommsManager commsManager)
    {
        _commsManager = commsManager ?? throw new ArgumentNullException(nameof(commsManager));

        var commDefinitions = new Dictionary<string, IValueAdapterCommDefinition>();
        commDefinitions[LanguageNameValues.Python] = new PythonValueAdapterCommTarget();
        commDefinitions[LanguageNameValues.R] = new RValueAdapterCommTarget();

        _commDefinitions = commDefinitions;
    }

    public async Task<bool> ApplyAsync(JupyterKernel kernel)
    {
        var adapter = await GetValueAdapter(kernel);

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

    private async Task<CommValueAdapter> GetValueAdapter(JupyterKernel kernel)
    {
        string language = kernel.KernelInfo.LanguageName?.ToLowerInvariant();
        if (_commDefinitions.TryGetValue(language, out IValueAdapterCommDefinition definition))
        {
            var code = definition.GetTargetDefinition(TargetName);
            var initialized = await kernel.RunOnKernelAsync(code);
            if (!initialized)
            {
                return null; // don't try to create value adapter if we failed
            }

            var adapter = await CreateCommValueAdapterAsync();
            return adapter;
        }

        return null;
    }

    private async Task<CommValueAdapter> CreateCommValueAdapterAsync()
    {
        var agent = await _commsManager.OpenCommAsync(TargetName);

        if (agent is not null)
        {
            var response = await agent.Messages
                .TakeUntilMessageType(JupyterMessageContentTypes.CommMsg, JupyterMessageContentTypes.CommClose)
                .ToTask();

            if (response is CommMsg messageReceived)
            {
                var adapterMessage = ValueAdapterMessageExtensions.FromDataDictionary(messageReceived.Data);
                if (adapterMessage is InitializedEvent)
                {
                    return new CommValueAdapter(agent);
                }
            }
        }

        return null;
    }
}
