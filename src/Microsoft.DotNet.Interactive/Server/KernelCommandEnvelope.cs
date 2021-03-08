// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Server
{
    public abstract class KernelCommandEnvelope : IKernelCommandEnvelope
    {
        private static readonly ConcurrentDictionary<Type, Func<KernelCommand, IKernelCommandEnvelope>> _envelopeFactories =
            new ConcurrentDictionary<Type, Func<KernelCommand, IKernelCommandEnvelope>>();

        private static ConcurrentDictionary<string, Type> _envelopeTypesByCommandTypeName;

        private static ConcurrentDictionary<string, Type> _commandTypesByCommandTypeName;


        static KernelCommandEnvelope()
        {
            ResetToDefaults();
        }

        internal static Type CommandTypeByName(string name) => _commandTypesByCommandTypeName[name];

        private readonly KernelCommand _command;

        protected KernelCommandEnvelope(KernelCommand command)
        {
            _command = command ?? throw new ArgumentNullException(nameof(command));
        }

        public abstract string CommandType { get; }

        public string Token => _command.GetToken();

        KernelCommand IKernelCommandEnvelope.Command => _command;

        public static void RegisterCommandType<T>(string commandTypeName) where T : KernelCommand
        {
            var commandEnvelopeType = typeof(KernelCommandEnvelope<T>);
            var commandType = typeof(T);

            _envelopeTypesByCommandTypeName.TryAdd(commandTypeName, commandEnvelopeType);
            _commandTypesByCommandTypeName.TryAdd(commandTypeName, commandType);
        }

        internal static void RegisterCommandTypeReplacingIfNecessary<T>() where T : KernelCommand
        {
            var commandEnvelopeType = typeof(KernelCommandEnvelope<T>);
            var commandType = typeof(T);
            var commandTypeName = commandType.Name;

            _envelopeTypesByCommandTypeName[commandTypeName] = commandEnvelopeType;
            _commandTypesByCommandTypeName[commandTypeName] = commandType;
        }

        public static void ResetToDefaults()
        {
            _envelopeTypesByCommandTypeName = new ConcurrentDictionary<string, Type>
            {
                [nameof(AddPackage)] = typeof(KernelCommandEnvelope<AddPackage>),
                [nameof(ChangeWorkingDirectory)] = typeof(KernelCommandEnvelope<ChangeWorkingDirectory>),
                [nameof(DisplayError)] = typeof(KernelCommandEnvelope<DisplayError>),
                [nameof(DisplayValue)] = typeof(KernelCommandEnvelope<DisplayValue>),
                [nameof(ParseNotebook)] = typeof(KernelCommandEnvelope<ParseNotebook>),
                [nameof(RequestCompletions)] = typeof(KernelCommandEnvelope<RequestCompletions>),
                [nameof(RequestDiagnostics)] = typeof(KernelCommandEnvelope<RequestDiagnostics>),
                [nameof(RequestHoverText)] = typeof(KernelCommandEnvelope<RequestHoverText>),
                [nameof(RequestSignatureHelp)] = typeof(KernelCommandEnvelope<RequestSignatureHelp>),
                [nameof(SerializeNotebook)] = typeof(KernelCommandEnvelope<SerializeNotebook>),
                [nameof(SubmitCode)] = typeof(KernelCommandEnvelope<SubmitCode>),
                [nameof(UpdateDisplayedValue)] = typeof(KernelCommandEnvelope<UpdateDisplayedValue>),
                [nameof(Quit)] = typeof(KernelCommandEnvelope<Quit>),
                [nameof(Cancel)] = typeof(KernelCommandEnvelope<Cancel>)
            };

            _commandTypesByCommandTypeName = new ConcurrentDictionary<string, Type>(_envelopeTypesByCommandTypeName
                .ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.GetGenericArguments()[0]));
        }

        public static IKernelCommandEnvelope Create(KernelCommand command)
        {
            var factory = _envelopeFactories.GetOrAdd(
                command.GetType(),
                commandType =>
                {
                    var genericType = _envelopeTypesByCommandTypeName[command.GetType().Name];

                    var constructor = genericType.GetConstructors().Single();

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

        internal static IKernelCommandEnvelope Deserialize(JsonElement json)
        {
            var commandTypeJson = string.Empty;
            var commandJson = string.Empty;
            var token = string.Empty;
            
            if (json.TryGetProperty("commandType", out var commandTypeProperty))
            {
                commandTypeJson = commandTypeProperty.GetString();
            }

            if (string.IsNullOrWhiteSpace(commandTypeJson))
            {
                return null;
            }

            var commandType = CommandTypeByName(commandTypeJson);
            if (json.TryGetProperty("command", out var commandJsonProperty))
            {
                commandJson = commandJsonProperty.GetRawText();
            }
            else
            {
                return null;
            }

       
            var command = (KernelCommand) JsonSerializer.Deserialize( commandJson,commandType, Serializer.JsonSerializerOptions);

           
            if (json.TryGetProperty("token", out var tokenProperty))
            {
                token = tokenProperty.GetString();
            }
            if (token != null)
            {
                command.SetToken(token);
            }

            return Create(command);
        }

        public static string Serialize(KernelCommand command) => Serialize(Create(command));

        public static string Serialize(IKernelCommandEnvelope envelope)
        {
            var serializationModel = new SerializationModel
            {
                command = envelope.Command,
                commandType = envelope.CommandType,
                token = envelope.Token
            };

            return JsonSerializer.Serialize(
                serializationModel,
                Serializer.JsonSerializerOptions);
        }

        internal class SerializationModel
        {
            public string token { get; set; }

            public string commandType { get; set; }

            public object command { get; set; }
        }
    }
}