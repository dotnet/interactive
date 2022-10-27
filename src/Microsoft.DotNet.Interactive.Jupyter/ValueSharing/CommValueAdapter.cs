// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Microsoft.DotNet.Interactive.Jupyter.Connection;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.Messaging.Comms;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.ValueSharing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Threading.Tasks;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing;

internal class CommValueAdapter : IValueAdapter
{
    private readonly CommAgent _agent;
    private readonly CompositeDisposable _disposables;

    public CommValueAdapter(CommAgent agent)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _disposables = new CompositeDisposable
        {
            _agent
        };
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    public void Display(ICommandExecutionContext context, KernelCommand command, object value, params string[] mimeTypes)
    {
        var displayId = Guid.NewGuid().ToString();

        var formattedValues = FormattedValue.FromObject(value, mimeTypes);

        context.Publish(
            new DisplayedValueProduced(
                value,
                command,
                formattedValues,
                displayId));
    }

    public async Task HandleCommandAsync(SendValue command, ICommandExecutionContext context, CancellationToken token)
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
                        token);
                    success = response is not null && response.Success;
                    if (!success)
                    {
                        context.Publish(new CommandFailed($"Failed to create part of results in '{variableName}' as '{partName}'. {response.Message}", command));
                        break;
                    }
                }

                if (success && tableCount > 1)
                {
                    Display(context, command, $"Multiple results in '{variableName}'. Created variables as {string.Join(", ", variablePartNames)}");
                }
            }
            else
            {
                var formattedValue = (variableValue is TabularDataResource table) ?
                    new FormattedValue(
                        TabularDataResourceFormatter.MimeType,
                        JsonSerializer.Serialize(table, TabularDataResourceFormatter.JsonSerializerOptions))
                    : command.FormattedValue;

                var response = await SetVariableAsync(seq, variableName, formattedValue.Value, formattedValue.MimeType, token);
                success = response is not null && response.Success;
                if (!success)
                {
                    context.Publish(new CommandFailed($"Failed to create variable '{variableName}'. {response.Message}", command));
                }
            }
        }
        catch (Exception e)
        {
            context.Publish(new CommandFailed(e, command));
            return;
        }

        if (success)
        {
            context.Publish(new CommandSucceeded(command));
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

    public async Task HandleCommandAsync(RequestValue command, ICommandExecutionContext context, CancellationToken token)
    {
        if (FailIfAgentIsClosed(command, context))
        {
            return;
        }

        var arguments = new GetVariableArguments(command.Name, command.MimeType);
        var request = new GetVariableRequest(arguments);

        var response = await SendRequestAsync(request, token);

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
            context.Publish(new CommandSucceeded(command));
            return;
        }

        context.Publish(new CommandFailed($"Failed to get variable {command.Name}.", command));
    }

    public async Task HandleCommandAsync(RequestValueInfos command, ICommandExecutionContext context, CancellationToken token)
    {
        if (FailIfAgentIsClosed(command, context))
        {
            return;
        }

        var request = new VariablesRequest();

        var response = await SendRequestAsync(request, token);

        if (response is VariablesResponse variablesResponse && variablesResponse.Success)
        {
            var kernelValueInfos = variablesResponse.Body.Variables
                .Select(v => new KernelValueInfo(v.Name)).ToList();

            context.Publish(new ValueInfosProduced(kernelValueInfos, command));
            context.Publish(new CommandSucceeded(command));
            return;
        }

        context.Publish(new CommandFailed($"Failed to get variables", command));
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

    private bool FailIfAgentIsClosed(KernelCommand command, ICommandExecutionContext context)
    {
        if (_agent.IsClosed)
        {
            context.Publish(new CommandFailed("Value adapter channel with kernel shutdown", command));
            return true;
        }

        return false;
    }
}
