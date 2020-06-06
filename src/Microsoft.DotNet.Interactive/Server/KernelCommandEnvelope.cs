// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.DotNet.Interactive.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.Server
{
    public abstract class KernelCommandEnvelope : IKernelCommandEnvelope
    {
        private static readonly ConcurrentDictionary<Type, Func<IKernelCommand, IKernelCommandEnvelope>> _envelopeFactories =
            new ConcurrentDictionary<Type, Func<IKernelCommand, IKernelCommandEnvelope>>();

        private static Dictionary<string, Type> _envelopeTypesByCommandTypeName;

        private static Dictionary<string, Type> _commandTypesByCommandTypeName;

      
        static KernelCommandEnvelope()
        {
            ResetToDefaults();
        }

        internal static Type CommandTypeByName(string name) => _commandTypesByCommandTypeName[name];

        private readonly IKernelCommand _command;

        protected KernelCommandEnvelope(IKernelCommand command)
        {
            _command = command ?? throw new ArgumentNullException(nameof(command));
        }

        public abstract string CommandType { get; }

        public string Token => _command.GetToken();

        IKernelCommand IKernelCommandEnvelope.Command => _command;

        public static void RegisterCommandType<T>(string commandTypeName) where T : class, IKernelCommand
        {
            var commandEnvelopeType = typeof(KernelCommandEnvelope<T>);
            var commandType = typeof(T);

            _envelopeTypesByCommandTypeName.Add(commandTypeName, commandEnvelopeType);
            _commandTypesByCommandTypeName.Add(commandTypeName, commandType);
        }
        public static void ResetToDefaults()
        {
            _envelopeTypesByCommandTypeName = new Dictionary<string, Type>
            {
                [nameof(AddPackage)] = typeof(KernelCommandEnvelope<AddPackage>),
                [nameof(ChangeWorkingDirectory)] = typeof(KernelCommandEnvelope<ChangeWorkingDirectory>),
                [nameof(DisplayError)] = typeof(KernelCommandEnvelope<DisplayError>),
                [nameof(DisplayValue)] = typeof(KernelCommandEnvelope<DisplayValue>),
                [nameof(RequestCompletion)] = typeof(KernelCommandEnvelope<RequestCompletion>),
                [nameof(RequestDiagnostics)] = typeof(KernelCommandEnvelope<RequestDiagnostics>),
                [nameof(RequestHoverText)] = typeof(KernelCommandEnvelope<RequestHoverText>),
                [nameof(SubmitCode)] = typeof(KernelCommandEnvelope<SubmitCode>),
                [nameof(UpdateDisplayedValue)] = typeof(KernelCommandEnvelope<UpdateDisplayedValue>)
            };

            _commandTypesByCommandTypeName = _envelopeTypesByCommandTypeName
                .ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.GetGenericArguments()[0]);
        }

        public static IKernelCommandEnvelope Create(IKernelCommand command)
        {
            var factory = _envelopeFactories.GetOrAdd(
                command.GetType(),
                commandType =>
                {
                    var genericType = _envelopeTypesByCommandTypeName[command.GetType().Name];

                    var constructor = genericType.GetConstructors().Single();

                    var commandParameter = Expression.Parameter(
                        typeof(IKernelCommand),
                        "c");

                    var newExpression = Expression.New(
                        constructor,
                        Expression.Convert(commandParameter, commandType));

                    var expression = Expression.Lambda<Func<IKernelCommand, IKernelCommandEnvelope>>(
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
            var command = (IKernelCommand) commandJson?.ToObject(commandType, Serializer.JsonSerializer);

            var token = json["token"]?.Value<string>();

            if (token != null)
            {
                command.SetToken(token);
            }

            return Create(command);
        }

        public static string Serialize(IKernelCommand command) => Serialize(Create(command));

        public static string Serialize(IKernelCommandEnvelope envelope)
        {
            var serializationModel = new SerializationModel
            {
                command = envelope.Command,
                commandType = envelope.CommandType,
                token = envelope.Token
            };

            return JsonConvert.SerializeObject(
                serializationModel,
                Serializer.JsonSerializerSettings);
        }

        internal class SerializationModel
        {
            public string token { get; set; }

            public string commandType { get; set; }

            public object command { get; set; }
        }
    }
}