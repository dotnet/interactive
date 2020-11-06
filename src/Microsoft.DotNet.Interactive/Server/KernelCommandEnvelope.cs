// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;

using Microsoft.DotNet.Interactive.Commands;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
                [nameof(SerializeNotebook)] = typeof(KernelCommandEnvelope<SerializeNotebook>),
                [nameof(SubmitCode)] = typeof(KernelCommandEnvelope<SubmitCode>),
                [nameof(UpdateDisplayedValue)] = typeof(KernelCommandEnvelope<UpdateDisplayedValue>),
                [nameof(SendMessage)] = typeof(KernelCommandEnvelope<SendMessage>),
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
            var jsonObject = JObject.Parse(json);

            return Deserialize(jsonObject);
        }

        internal static IKernelCommandEnvelope Deserialize(JToken json)
        {
            if (json is JValue)
            {
                return null;
            }

            var commandTypeJson = json["commandType"];

            if (commandTypeJson == null)
            {
                return null;
            }

            var commandType = CommandTypeByName(commandTypeJson.Value<string>());
            var commandJson = json["command"];
            var command = (KernelCommand)commandJson?.ToObject(commandType, Serializer.JsonSerializer);

            var token = json["token"]?.Value<string>();

            if (token != null)
            {
                command.SetToken(token);
            }

            return Create(command);
        }

        public static string Serialize(KernelCommand command) => Serialize(Create(command));
        internal static SerializationModel SerializeToModel(KernelCommand command) => SerializeToModel(Create(command));

        public static string Serialize(IKernelCommandEnvelope envelope)
        {
            SerializationModel serializationModel = SerializeToModel(envelope);

            return JsonConvert.SerializeObject(
                serializationModel,
                Serializer.JsonSerializerSettings);
        }

        internal static SerializationModel SerializeToModel(IKernelCommandEnvelope envelope)
        {
            return new SerializationModel
            {
                command = envelope.Command,
                commandType = envelope.CommandType,
                token = envelope.Token
            };
        }

        internal class SerializationModel
        {
            public string token { get; set; }

            public string commandType { get; set; }

            public object command { get; set; }
        }
    }
}