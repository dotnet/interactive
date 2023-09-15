// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Connection;

public abstract class KernelCommandEnvelope : IKernelCommandEnvelope
{
    private static readonly ConcurrentDictionary<Type, Func<KernelCommand, IKernelCommandEnvelope>> _envelopeFactories =
        new();

    private static ConcurrentDictionary<string, Type> _envelopeTypesByCommandTypeName;

    private static ConcurrentDictionary<string, Type> _commandTypesByCommandTypeName;

    static KernelCommandEnvelope()
    {
        RegisterDefaults();
    }

    internal static Type CommandTypeByName(string name) => _commandTypesByCommandTypeName[name];

    private readonly KernelCommand _command;

    protected KernelCommandEnvelope(KernelCommand command)
    {
        _command = command ?? throw new ArgumentNullException(nameof(command));
    }

    public abstract string CommandType { get; }

    public string Token => _command.GetOrCreateToken();

    public string CommandId => _command.GetOrCreateId();

    KernelCommand IKernelCommandEnvelope.Command => _command;

    public static void RegisterCommand<T>() where T : KernelCommand
    {
        var commandType = typeof(T);
        RegisterCommand(commandType);
    }

    public static void RegisterCommand(Type commandType)
    {
        var commandTypeName = commandType.Name;
        var commandEnvelopeType = typeof(KernelCommandEnvelope<>).MakeGenericType(commandType);
        _envelopeTypesByCommandTypeName[commandTypeName] = commandEnvelopeType;
        _commandTypesByCommandTypeName[commandTypeName] = commandType;
    }

    public static void RegisterDefaults()
    {
        _envelopeTypesByCommandTypeName = new ConcurrentDictionary<string, Type>
        {
            [nameof(ChangeWorkingDirectory)] = typeof(KernelCommandEnvelope<ChangeWorkingDirectory>),
            [nameof(DisplayError)] = typeof(KernelCommandEnvelope<DisplayError>),
            [nameof(DisplayValue)] = typeof(KernelCommandEnvelope<DisplayValue>),
            [nameof(RequestCompletions)] = typeof(KernelCommandEnvelope<RequestCompletions>),
            [nameof(RequestDiagnostics)] = typeof(KernelCommandEnvelope<RequestDiagnostics>),
            [nameof(RequestHoverText)] = typeof(KernelCommandEnvelope<RequestHoverText>),
            [nameof(RequestSignatureHelp)] = typeof(KernelCommandEnvelope<RequestSignatureHelp>),
            [nameof(SendEditableCode)] = typeof(KernelCommandEnvelope<SendEditableCode>),
            [nameof(SubmitCode)] = typeof(KernelCommandEnvelope<SubmitCode>),
            [nameof(UpdateDisplayedValue)] = typeof(KernelCommandEnvelope<UpdateDisplayedValue>),
            [nameof(Quit)] = typeof(KernelCommandEnvelope<Quit>),
            [nameof(Cancel)] = typeof(KernelCommandEnvelope<Cancel>),
            [nameof(RequestInput)] = typeof(KernelCommandEnvelope<RequestInput>),
            [nameof(RequestValue)] = typeof(KernelCommandEnvelope<RequestValue>),
            [nameof(RequestValueInfos)] = typeof(KernelCommandEnvelope<RequestValueInfos>),
            [nameof(RequestKernelInfo)] = typeof(KernelCommandEnvelope<RequestKernelInfo>),
            [nameof(SendValue)] = typeof(KernelCommandEnvelope<SendValue>)
        };

        _commandTypesByCommandTypeName = new ConcurrentDictionary<string, Type>(_envelopeTypesByCommandTypeName
                                                                                    .ToDictionary(
                                                                                        pair => pair.Key,
                                                                                        pair => pair.Value.GetGenericArguments()[0]));
    }

    public static IKernelCommandEnvelope Create(KernelCommand command)
    {
        var envelopeType = _envelopeTypesByCommandTypeName.GetOrAdd(
            command.GetType().Name,
            commandTypeName =>
            {
                var commandType = command.GetType();

                var commandEnvelopeType = typeof(KernelCommandEnvelope<>).MakeGenericType(commandType);

                _commandTypesByCommandTypeName[commandTypeName] = commandType;

                return commandEnvelopeType;
            });

        var factory = _envelopeFactories.GetOrAdd(
            command.GetType(),
            commandType =>
            {
                var constructor = envelopeType.GetConstructors().Single();

                var commandParameter = Expression.Parameter(
                    typeof(KernelCommand),
                    "c");

                var newExpression = Expression.New(
                    constructor,
                    Expression.Convert(commandParameter, commandType));

                var expression = Expression.Lambda<Func<KernelCommand, IKernelCommandEnvelope>>(
                    newExpression,
                    commandParameter);

                return expression.Compile();
            });

        var envelope = factory(command);

        return envelope;
    }

    public static IKernelCommandEnvelope Deserialize(string json)
    {
        var jsonObject = JsonDocument.Parse(json);

        return Deserialize(jsonObject.RootElement);
    }

    public static IKernelCommandEnvelope Deserialize(JsonElement json)
    {
        var commandTypeJson = string.Empty;
        string commandJson;
        var commandToken = string.Empty;
        var commandId = string.Empty;

        if (json.TryGetProperty(nameof(SerializationModel.commandType), out var commandTypeProperty))
        {
            commandTypeJson = commandTypeProperty.GetString();
        }

        if (string.IsNullOrWhiteSpace(commandTypeJson))
        {
            return null;
        }

        var commandType = CommandTypeByName(commandTypeJson);
        if (json.TryGetProperty(nameof(SerializationModel.command), out var commandJsonProperty))
        {
            commandJson = commandJsonProperty.GetRawText();
        }
        else
        {
            return null;
        }

        var command = (KernelCommand)JsonSerializer.Deserialize(commandJson, commandType, Serializer.JsonSerializerOptions);

        // restore the command id
        if (json.TryGetProperty(nameof(SerializationModel.id), out var commandIdProperty))
        {
            commandId = commandIdProperty.GetString();
        }

        // restore the command token
        if (json.TryGetProperty(nameof(SerializationModel.token), out var tokenProperty))
        {
            commandToken = tokenProperty.GetString();
        }

        if (commandToken is not null)
        {
            command.SetToken(commandToken);
        }
        else
        {
        }

        if (commandId is not null)
        {
            command.SetId(commandId);
        }

        if (json.TryGetProperty(nameof(SerializationModel.routingSlip), out var routingSlipProperty))
        {
            foreach (var routingSlipItem in routingSlipProperty.EnumerateArray())
            {
                var uri = new Uri(routingSlipItem.GetString(), UriKind.Absolute);

                if (string.IsNullOrWhiteSpace(uri.Query))
                {
                    command.RoutingSlip.Stamp(uri);
                }
                else
                {
                    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    command.RoutingSlip.StampAs(uri, query["tag"]);
                }
            }
        }

        return Create(command);
    }

    public static string Serialize(KernelCommand command) => Serialize(Create(command));

    public static string Serialize(IKernelCommandEnvelope envelope)
    {
        var serializationModel = CreateSerializationModel(envelope);

        return JsonSerializer.Serialize(
            serializationModel,
            Serializer.JsonSerializerOptions);
    }

    private static SerializationModel CreateSerializationModel(IKernelCommandEnvelope envelope)
    {
        var serializationModel = new SerializationModel
        {
            command = envelope.Command,
            commandType = envelope.CommandType,
            token = envelope.Token,
            id = envelope.CommandId,
            routingSlip = envelope.Command.RoutingSlip.ToUriArray()
        };
        return serializationModel;
    }

    internal class SerializationModel
    {
        public string token { get; set; }

        public string id { get; set; }

        public string commandType { get; set; }

        public object command { get; set; }

        public string[] routingSlip { get; set; }
    }
}