// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.DotNet.Interactive.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.Server
{
    public abstract class KernelEventEnvelope : IKernelEventEnvelope
    {
        private static readonly ConcurrentDictionary<Type, Func<IKernelEvent, IKernelEventEnvelope>> _envelopeFactories =
            new ConcurrentDictionary<Type, Func<IKernelEvent, IKernelEventEnvelope>>();

        private static readonly Dictionary<string, Type> _envelopeTypesByEventTypeName;

        private static readonly Dictionary<string, Type> _eventTypesByEventTypeName;

        static KernelEventEnvelope()
        {
            _envelopeTypesByEventTypeName = new Dictionary<string, Type>
            {
                [nameof(CodeSubmissionReceived)] = typeof(KernelEventEnvelope<CodeSubmissionReceived>),
                [nameof(CommandFailed)] = typeof(KernelEventEnvelope<CommandFailed>),
                [nameof(CommandHandled)] = typeof(KernelEventEnvelope<CommandHandled>),
                [nameof(CompleteCodeSubmissionReceived)] = typeof(KernelEventEnvelope<CompleteCodeSubmissionReceived>),
                [nameof(CompletionRequestCompleted)] = typeof(KernelEventEnvelope<CompletionRequestCompleted>),
                [nameof(CompletionRequestReceived)] = typeof(KernelEventEnvelope<CompletionRequestReceived>),
                [nameof(DiagnosticLogEntryProduced)] = typeof(KernelEventEnvelope<DiagnosticLogEntryProduced>),
                [nameof(DisplayedValueProduced)] = typeof(KernelEventEnvelope<DisplayedValueProduced>),
                [nameof(DisplayedValueUpdated)] = typeof(KernelEventEnvelope<DisplayedValueUpdated>),
                [nameof(ErrorProduced)] = typeof(KernelEventEnvelope<ErrorProduced>),
                [nameof(IncompleteCodeSubmissionReceived)] = typeof(KernelEventEnvelope<IncompleteCodeSubmissionReceived>),
                [nameof(InputRequested)] = typeof(KernelEventEnvelope<InputRequested>),
                [nameof(HoverTextProduced)] = typeof(KernelEventEnvelope<HoverTextProduced>),
                [nameof(PackageAdded)] = typeof(KernelEventEnvelope<PackageAdded>),
                [nameof(PasswordRequested)] = typeof(KernelEventEnvelope<PasswordRequested>),
                [nameof(ReturnValueProduced)] = typeof(KernelEventEnvelope<ReturnValueProduced>),
                [nameof(StandardErrorValueProduced)] = typeof(KernelEventEnvelope<StandardErrorValueProduced>),
                [nameof(StandardOutputValueProduced)] = typeof(KernelEventEnvelope<StandardOutputValueProduced>),
            };

            _eventTypesByEventTypeName = _envelopeTypesByEventTypeName
                .ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.GetGenericArguments()[0]);
        }

        internal static Type EventTypeByName(string name) => _eventTypesByEventTypeName[name];

        private readonly IKernelEvent _event;

        protected KernelEventEnvelope(IKernelEvent @event)
        {
            _event = @event ?? throw new ArgumentNullException(nameof(@event));
            CommandType = @event.Command?.GetType().Name;
        }

        public string CommandType { get; }

        public abstract string EventType { get; }

        IKernelEvent IKernelEventEnvelope.Event => _event;

        public static IKernelEventEnvelope Create(IKernelEvent @event)
        {
            var factory = _envelopeFactories.GetOrAdd(
                @event.GetType(),
                eventType =>
                {
                    var genericType = _envelopeTypesByEventTypeName[@event.GetType().Name];

                    var constructor = genericType.GetConstructors().Single();

                    var eventParameter = Expression.Parameter(
                        typeof(IKernelEvent),
                        "e");

                    var newExpression = Expression.New(
                        constructor,
                        Expression.Convert(eventParameter, eventType));

                    var expression = Expression.Lambda<Func<IKernelEvent, IKernelEventEnvelope>>(
                        newExpression,
                        eventParameter);

                    return expression.Compile();
                });

            var envelope = factory(@event);

            return envelope;
        }

        public static IKernelEventEnvelope Deserialize(string json)
        {
            var jsonObject = JObject.Parse(json);

            var commandJson = jsonObject[nameof(SerializationModel.command)];

            var commandEnvelope = KernelCommandEnvelope.Deserialize(commandJson);

            var eventJson = jsonObject[nameof(SerializationModel.@event)];

            var eventTypeName = jsonObject[nameof(SerializationModel.eventType)].Value<string>();

            var eventType = EventTypeByName(eventTypeName);

            var @event = (IKernelEvent) eventJson.ToObject(eventType, Serializer.JsonSerializer);

            if (@event is KernelEventBase eventBase &&
                commandEnvelope is {})
            {
                eventBase.Command = commandEnvelope.Command;
            }

            return Create(@event);
        }

        public static string Serialize(IKernelEvent @event) => Serialize(Create(@event));

        public static string Serialize(IKernelEventEnvelope eventEnvelope)
        {
            KernelCommandEnvelope.SerializationModel commandSerializationModel = null;

            if (eventEnvelope.Event.Command != null)
            {
                var commandEnvelope = KernelCommandEnvelope.Create(eventEnvelope.Event.Command);

                commandSerializationModel = new KernelCommandEnvelope.SerializationModel
                {
                    command = commandEnvelope.Command,
                    commandType = commandEnvelope.CommandType,
                    token = eventEnvelope.Event.Command.GetToken()
                };
            }

            var serializationModel = new SerializationModel
            {
                @event = eventEnvelope.Event,
                eventType = eventEnvelope.EventType,
                command = commandSerializationModel
            };

            return JsonConvert.SerializeObject(
                serializationModel,
                Serializer.JsonSerializerSettings);
        }

        internal class SerializationModel
        {
            public object @event { get; set; }

            public string eventType { get; set; }

            public KernelCommandEnvelope.SerializationModel command { get; set; }
        }
    }
}