// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.Messaging.Comms;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.ValueSharing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing;

internal class CommValueAdapter : IDisposable,
    IKernelCommandHandler<SendValue>,
    IKernelCommandHandler<RequestValue>,
    IKernelCommandHandler<RequestValueInfos>
{
    private readonly CommAgent _agent;

    public CommValueAdapter(CommAgent agent)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
    }

    public void Dispose()
    {
        _agent.Dispose();
    }

    public async Task HandleAsync(SendValue command, KernelInvocationContext context)
    {
        if (FailIfAgentIsClosed(command, context))
        {
            return;
        }

        var variableName = command.Name;
        var variableValue = command.Value;

        int seq = 0;
        bool success = false;

        try
        {
            if (variableValue is IEnumerable<TabularDataResource> tables)
            {
                int tableCount = tables.Count();
                int tableIndex = 0;

                var variablePartNames = Enumerable.Repeat(variableName, tableCount).Select((s, i) => $"{s}{i + 1}").ToArray();
                foreach (var table in tables)
                {
                    var partName = tableCount > 1 ? variablePartNames[tableIndex++] : variableName;
                    var response = await SetVariableAsync(
                        seq,
                        partName,
                        JsonSerializer.Serialize(table, TabularDataResourceFormatter.JsonSerializerOptions),
                        TabularDataResourceFormatter.MimeType,
                        context.CancellationToken);
                    success = response is not null && response.Success;
                    if (!success)
                    {
                        context.Display($"Failed to create part of results in '{variableName}' as '{partName}'. {response.Message}");
                        context.Fail(command);
                        return;
                    }
                }

                if (success && tableCount > 1)
                {
                    context.Display($"Multiple results in '{variableName}'. Created variables as {string.Join(", ", variablePartNames)}");
                }
            }
            else
            {
                var formattedValue = (variableValue is TabularDataResource table) ?
                    new FormattedValue(
                        TabularDataResourceFormatter.MimeType,
                        JsonSerializer.Serialize(table, TabularDataResourceFormatter.JsonSerializerOptions))
                    : command.FormattedValue;

                var response = await SetVariableAsync(seq, variableName, formattedValue.Value, formattedValue.MimeType, context.CancellationToken);
                success = response is not null && response.Success;
                if (!success)
                {
                    context.Display($"Failed to create variable '{variableName}'. {response.Message}");
                    context.Fail(command);
                    return;
                }
            }
        }
        catch (Exception e)
        {
            context.Fail(command, e);
            return;
        }
    }

    private async Task<SetVariableResponse> SetVariableAsync(int seq, string name, object value, string formattedType, CancellationToken token)
    {
        var arguments = new SetVariableArguments(seq, name, value, formattedType);
        var request = new SetVariableRequest(arguments);

        var response = await SendRequestAsync(request, token);

        if (response is SetVariableResponse setVariableResponse)
        {
            return setVariableResponse;
        }

        return null;
    }

    public async Task HandleAsync(RequestValue command, KernelInvocationContext context)
    {
        if (FailIfAgentIsClosed(command, context))
        {
            return;
        }

        var arguments = new GetVariableArguments(command.Name, command.MimeType);
        var request = new GetVariableRequest(arguments);

        var response = await SendRequestAsync(request, context.CancellationToken);

        if (response is GetVariableResponse getVariableResponse && getVariableResponse.Success)
        {
            var jsonValue = (JsonElement)getVariableResponse.Body.Value;
            var value = getVariableResponse.Body.Type == TabularDataResourceFormatter.MimeType ?
                    jsonValue.ToTabularDataResource() : jsonValue.ToObject();

            var valueType = value.GetType();
            var formatter = Formatter.GetPreferredFormatterFor(valueType, command.MimeType);

            using var writer = new StringWriter(CultureInfo.InvariantCulture);
            formatter.Format(value, writer);

            var formatted = new FormattedValue(command.MimeType, writer.ToString());

            context.Publish(new ValueProduced(value, command.Name, formatted, command));
            return;
        }

        context.Display($"Failed to get variable {command.Name}.");
        context.Fail(command);
    }

    public async Task HandleAsync(RequestValueInfos command, KernelInvocationContext context)
    {
        if (FailIfAgentIsClosed(command, context))
        {
            return;
        }

        var request = new VariablesRequest();

        var response = await SendRequestAsync(request, context.CancellationToken);

        if (response is VariablesResponse variablesResponse && variablesResponse.Success)
        {
            var kernelValueInfos = variablesResponse.Body.Variables
                .Select(v => new KernelValueInfo(v.Name)).ToList();

            context.Publish(new ValueInfosProduced(kernelValueInfos, command));
            return;
        }

        context.Display($"Failed to get variables");
        context.Fail(command); 
    }

    private async Task<ValueAdapterMessage> SendRequestAsync(ValueAdapterMessage request, CancellationToken token)
    {
        var responseObservable = GetResponseObservable();

        await _agent.SendAsync(request.ToDictionary());
        var response = await responseObservable.ToTask(token);

        if (response is CommMsg commMsg)
        {
            var adapterResponse = ValueAdapterMessageExtensions.FromDataDictionary(commMsg.Data);
            return adapterResponse;
        }

        return null;
    }

    private IObservable<Protocol.Message> GetResponseObservable()
    {
        return _agent.Messages.TakeUntilMessageType(
            JupyterMessageContentTypes.CommMsg,
            JupyterMessageContentTypes.CommClose);
    }

    private bool FailIfAgentIsClosed(KernelCommand command, KernelInvocationContext context)
    {
        if (_agent.IsClosed)
        {
            context.Fail(command, null, "Value adapter channel with kernel shutdown");
            return true;
        }

        return false;
    }
}
