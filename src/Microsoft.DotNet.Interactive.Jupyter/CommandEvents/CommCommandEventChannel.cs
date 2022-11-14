// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.Messaging.Comms;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.CommandEvents;

internal class CommCommandEventChannel : IDisposable,
    IKernelCommandHandler<SendValue>,
    IKernelCommandHandler<RequestValue>,
    IKernelCommandHandler<RequestValueInfos>
{
    private readonly CommAgent _agent;

    public CommCommandEventChannel(CommAgent agent)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
    }

    public void Dispose()
    {
        _agent.Dispose();
    }

    private FormattedValue FormatTabularDataResource(TabularDataResource table) =>
        new FormattedValue(TabularDataResourceFormatter.MimeType,
                        JsonSerializer.Serialize(table, TabularDataResourceFormatter.JsonSerializerOptions));

    public async Task HandleAsync(SendValue command, KernelInvocationContext context)
    {
        ThrowIfAgentIsClosed();

        var variableName = command.Name;
        var variableValue = command.Value;

        if (variableValue is IEnumerable<TabularDataResource> tables)
        {
            int tableCount = tables.Count();
            int tableIndex = 0;

            var resultSetNames = Enumerable.Repeat(variableName, tableCount).Select((s, i) => $"{s}{i + 1}").ToArray();
            foreach (var resultSet in tables)
            {
                var resultSetName = tableCount > 1 ? resultSetNames[tableIndex++] : variableName;
                var resultSetSendValueCommand = new SendValue(
                                                        resultSetName,
                                                        null,
                                                        FormatTabularDataResource(resultSet)
                                                        );

                var commandResult = await SendAsync(resultSetSendValueCommand, context.CancellationToken);
                if (commandResult is CommandFailed commandFailed)
                {
                    context.Fail(command, message: commandFailed.Message);
                    return;
                }
            }

            if (tableCount > 1)
            {
                context.Display($"Multiple results in '{variableName}'. Created variables as {string.Join(", ", resultSetNames)}");
            }

            return;
        }

        var formattedValue = (variableValue is TabularDataResource table) ?
                    FormatTabularDataResource(table) : command.FormattedValue;

        var sendValueCommand = new SendValue(variableName, null, formattedValue);
        var result = await SendAsync(sendValueCommand, context.CancellationToken);
        if (result is CommandFailed failed)
        {
            context.Fail(command, message: failed.Message);
        }
    }

    public async Task HandleAsync(RequestValue command, KernelInvocationContext context)
    {
        ThrowIfAgentIsClosed();
        var result = await SendAsync(command, context.CancellationToken);
        if (result is CommandFailed failed)
        {
            context.Fail(command, message: failed.Message);
            return;
        }

        if (result is ValueProduced produced)
        {
            var jsonValue = (JsonElement)produced.Value;
            var value = produced.FormattedValue.MimeType == TabularDataResourceFormatter.MimeType ?
                    jsonValue.ToTabularDataResource() : jsonValue.ToObject();

            var valueType = value.GetType();
            var formatter = Formatter.GetPreferredFormatterFor(valueType, command.MimeType);

            using var writer = new StringWriter(CultureInfo.InvariantCulture);
            formatter.Format(value, writer);

            var formatted = new FormattedValue(command.MimeType, writer.ToString());

            context.Publish(new ValueProduced(value, command.Name, formatted, command));
            return;
        }

        context.Fail(command, message: $"Failed to get variable {command.Name}.");
        return;
    }

    public async Task HandleAsync(RequestValueInfos command, KernelInvocationContext context)
    {
        ThrowIfAgentIsClosed();
        var result = await SendAsync(command, context.CancellationToken);
        if (result is CommandFailed failed)
        {
            context.Fail(command, message: failed.Message);
            return;
        }

        if (result is ValueInfosProduced values)
        {
            context.Publish(new ValueInfosProduced(values.ValueInfos, command));
            return; 
        }

        context.Fail(command, message: "Failed to get variables.");
    }

    private async Task<KernelEvent> SendAsync(KernelCommand command, CancellationToken token)
    {
        var responseObservable = GetResponseObservable();

        var request = new CommandEventCommEnvelop(command);

        await _agent.SendAsync(request.ToDictionary());
        var response = await responseObservable.ToTask(token);

        if (response is CommMsg commMsg)
        {
            var responseEvent = CommandEventCommEnvelop.FromDataDictionary(commMsg.Data);
            return responseEvent.EventEnvelope.Event;
        }

        return new CommandFailed("Kernel didn't respond", command);
    }

    private IObservable<Protocol.Message> GetResponseObservable() =>
        _agent.Messages.TakeUntilMessageType(
            JupyterMessageContentTypes.CommMsg,
            JupyterMessageContentTypes.CommClose);

    private void ThrowIfAgentIsClosed()
    {
        if (_agent.IsClosed)
        {
            throw new Exception("CommandEvent adapter channel closed with kernel shutdown");
        }
    }
}
