// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing;

internal class CommValueAdapterConfiguration : IJupyterKernelConfiguration
{
    private static string TargetName = "value_adapter_comm";
    private IReadOnlyDictionary<string, IValueAdapterCommDefinition> _commDefinitions;

    public CommValueAdapterConfiguration()
    {
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
            var initialized = await RunOnKernelAsync(code, kernel);
            if (!initialized)
            {
                return null; // don't try to create value adapter if we failed
            }

            var adapter = await CreateValueAdapterAsync(kernel);
            return adapter;
        }

        return null;
    }

    private async Task<CommValueAdapter> CreateValueAdapterAsync(JupyterKernel kernel)
    {
        var agent = await kernel.Comms.OpenCommAsync(TargetName);

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

    private async Task<bool> RunOnKernelAsync(string code, Kernel kernel)
    {
        var results = await kernel.SendAsync(new SubmitCode(code));
        var success = await results.KernelEvents
                                         .OfType<CommandSucceeded>()
                                         .FirstOrDefaultAsync();

        return success is { };
    }
}
