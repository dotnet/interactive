// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

using BindingFlags = System.Reflection.BindingFlags;

namespace Microsoft.DotNet.Interactive.Server
{
    public abstract class KernelEventEnvelope : IKernelEventEnvelope
    {
        private static readonly ConcurrentDictionary<Type, Func<KernelEvent, IKernelEventEnvelope>> _envelopeFactories =
            new ConcurrentDictionary<Type, Func<KernelEvent, IKernelEventEnvelope>>();

        private static Dictionary<string, Type> _envelopeTypesByEventTypeName;

        private static Dictionary<string, Type> _eventTypesByEventTypeName;

        static KernelEventEnvelope()
        {
            ResetToDefaults();
        }

        internal static Type EventTypeByName(string name) => _eventTypesByEventTypeName[name];

        private readonly KernelEvent _event;

        protected KernelEventEnvelope(KernelEvent @event)
        {
            _event = @event ?? throw new ArgumentNullException(nameof(@event));
            CommandType = @event.Command?.GetType().Name;
        }

        public string CommandType { get; }

        public abstract string EventType { get; }

        KernelEvent IKernelEventEnvelope.Event => _event;

        public static void RegisterEvent<TEvent>() where TEvent : KernelEvent
        {
            _envelopeTypesByEventTypeName[typeof(TEvent).Name] = typeof(KernelEventEnvelope<TEvent>);
            _eventTypesByEventTypeName[typeof(TEvent).Name] = typeof(TEvent);
        }

        public static void ResetToDefaults()
        {
            _envelopeTypesByEventTypeName = new Dictionary<string, Type>
            {
                [nameof(CodeSubmissionReceived)] = typeof(KernelEventEnvelope<CodeSubmissionReceived>),
                [nameof(CommandFailed)] = typeof(KernelEventEnvelope<CommandFailed>),
                [nameof(CommandSucceeded)] = typeof(KernelEventEnvelope<CommandSucceeded>),
                [nameof(CompleteCodeSubmissionReceived)] = typeof(KernelEventEnvelope<CompleteCodeSubmissionReceived>),
                [nameof(CompletionsProduced)] = typeof(KernelEventEnvelope<CompletionsProduced>),
                [nameof(DiagnosticLogEntryProduced)] = typeof(KernelEventEnvelope<DiagnosticLogEntryProduced>),
                [nameof(DiagnosticsProduced)] = typeof(KernelEventEnvelope<DiagnosticsProduced>),
                [nameof(DisplayedValueProduced)] = typeof(KernelEventEnvelope<DisplayedValueProduced>),
                [nameof(DisplayedValueUpdated)] = typeof(KernelEventEnvelope<DisplayedValueUpdated>),
                [nameof(ErrorProduced)] = typeof(KernelEventEnvelope<ErrorProduced>),
                [nameof(IncompleteCodeSubmissionReceived)] = typeof(KernelEventEnvelope<IncompleteCodeSubmissionReceived>),
                [nameof(HoverTextProduced)] = typeof(KernelEventEnvelope<HoverTextProduced>),
                [nameof(KernelReady)] = typeof(KernelEventEnvelope<KernelReady>),
                [nameof(InteractiveDocumentParsed)] = typeof(KernelEventEnvelope<InteractiveDocumentParsed>),
                [nameof(InteractiveDocumentSerialized)] = typeof(KernelEventEnvelope<InteractiveDocumentSerialized>),
                [nameof(PackageAdded)] = typeof(KernelEventEnvelope<PackageAdded>),
                [nameof(ReturnValueProduced)] = typeof(KernelEventEnvelope<ReturnValueProduced>),
                [nameof(SignatureHelpProduced)] = typeof(KernelEventEnvelope<SignatureHelpProduced>),
                [nameof(StandardErrorValueProduced)] = typeof(KernelEventEnvelope<StandardErrorValueProduced>),
                [nameof(StandardOutputValueProduced)] = typeof(KernelEventEnvelope<StandardOutputValueProduced>),
                [nameof(WorkingDirectoryChanged)] = typeof(KernelEventEnvelope<WorkingDirectoryChanged>),
                [nameof(KernelExtensionLoaded)] = typeof(KernelEventEnvelope<KernelExtensionLoaded>),
                [nameof(ValueNamesProduced)] = typeof(KernelEventEnvelope<ValueNamesProduced>),
                [nameof(ValueProduced)] = typeof(KernelEventEnvelope<ValueProduced>)
            };

            _eventTypesByEventTypeName = _envelopeTypesByEventTypeName
                .ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.GetGenericArguments()[0]);
        }

        public static IKernelEventEnvelope Create(KernelEvent @event)
        {
            var factory = _envelopeFactories.GetOrAdd(
                @event.GetType(),
                eventType =>
                {
                    var genericType = _envelopeTypesByEventTypeName[@event.GetType().Name];

                    var constructor = genericType.GetConstructors().Single();

                    var eventParameter = Expression.Parameter(
                        typeof(KernelEvent),
                        "e");

                    var newExpression = Expression.New(
                        constructor,
                        Expression.Convert(eventParameter, eventType));

                    var expression = Expression.Lambda<Func<KernelEvent, IKernelEventEnvelope>>(
                        newExpression,
                        eventParameter);

                    return expression.Compile();
                });

            var envelope = factory(@event);

            return envelope;
        }

        public static IKernelEventEnvelope Deserialize(string json)
        {
            var jsonObject = JsonDocument.Parse(json).RootElement;

            return Deserialize(jsonObject);
        }

        public static IKernelEventEnvelope Deserialize(JsonElement jsonObject)
        {
            var commandJson = jsonObject.GetProperty(nameof(SerializationModel.command));

            var commandEnvelope = commandJson.ValueKind == JsonValueKind.Null ? null : KernelCommandEnvelope.Deserialize(commandJson);

            var command = commandEnvelope?.Command;

            return DeserializeWithCommand(jsonObject, command);
        }

        public static IKernelEventEnvelope DeserializeWithCommand(string json, KernelCommand command)
        {
            var jsonObject = JsonDocument.Parse(json).RootElement;

            return DeserializeWithCommand(jsonObject, command);
        }

        public static IKernelEventEnvelope DeserializeWithCommand(JsonElement jsonObject, KernelCommand command)
        {
            var eventJson = jsonObject.GetProperty(nameof(SerializationModel.@event));

            var eventTypeName = jsonObject.GetProperty(nameof(SerializationModel.eventType)).GetString();

            var eventType = EventTypeByName(eventTypeName);

            var ctor = eventType.GetConstructors(BindingFlags.IgnoreCase
                                                 | BindingFlags.Public
                                                 | BindingFlags.Instance)[0];

            var ctorParams = new List<object>();

            foreach (var parameterInfo in ctor.GetParameters())
            {
                if (typeof(KernelCommand).IsAssignableFrom(parameterInfo.ParameterType))
                {
                    ctorParams.Add(command ?? KernelCommand.None);
                }
                else
                {

                    ctorParams.Add(eventJson.TryGetProperty(parameterInfo.Name, out var property)
                        ? JsonSerializer.Deserialize(property.GetRawText(), parameterInfo.ParameterType,
                            Serializer.JsonSerializerOptions)
                        : GetDefaultValueForType(parameterInfo.ParameterType));
                }
            }

            var @event = (KernelEvent)ctor.Invoke(ctorParams.ToArray());

            return Create(@event);
        }

        private static object GetDefaultValueForType(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        public static string Serialize(KernelEvent @event) => Serialize(Create(@event));

        public static string Serialize(IKernelEventEnvelope eventEnvelope)
        {
            KernelCommandEnvelope.SerializationModel commandSerializationModel = null;

            if (eventEnvelope.Event.Command is not null && eventEnvelope.Event.Command is not NoCommand)
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

            return JsonSerializer.Serialize(
                serializationModel,
                Serializer.JsonSerializerOptions);
        }

        internal class SerializationModel
        {
            public object @event { get; set; }

            public string eventType { get; set; }

            public KernelCommandEnvelope.SerializationModel command { get; set; }
        }
    }
}