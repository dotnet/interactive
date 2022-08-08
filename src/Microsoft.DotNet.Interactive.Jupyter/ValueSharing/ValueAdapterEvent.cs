using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing
{
    public abstract class ValueAdapterEvent: ValueAdapterMessage
    {
        private static readonly IReadOnlyDictionary<string, Type> _eventToClrType;
        private static readonly IReadOnlyDictionary<Type, string> _clrTypeToEvent;

        private string _event;

        static ValueAdapterEvent()
        {
            var eventImplementations = typeof(ValueAdapterEvent).Assembly.GetExportedTypes().Where(t =>
                t.IsAbstract == false && typeof(ValueAdapterEvent).IsAssignableFrom(t)).ToList();

            var eventToClrType = new Dictionary<string, Type>();
            var clrTypeToEvent = new Dictionary<Type, string>();
            foreach (var eventImpl in eventImplementations)
            {
                var @event = eventImpl.GetCustomAttribute<ValueAdapterEventAttribute>(true);
                if (@event is not null)
                {
                    eventToClrType[@event.Name] = eventImpl;
                    clrTypeToEvent[eventImpl] = @event.Name;
                }
            }

            _eventToClrType = eventToClrType;
            _clrTypeToEvent = clrTypeToEvent;
        }

        [JsonPropertyName("event")]
        public string Event => _event ?? (_event = _clrTypeToEvent[GetType()]);

        public ValueAdapterEvent(): base(ValueAdapterMessageType.Event)
        {
        }

        public static ValueAdapterEvent FromDataDictionary(IReadOnlyDictionary<string, object> data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (!data.TryGetValue("event", out object @event))
            {
                throw new ArgumentException($"dictionary does not contain the event key.");
            }

            if (_eventToClrType.TryGetValue(@event?.ToString(), out var supportedType))
            {
                var jsonString = JsonSerializer.Serialize(data);
                return JsonSerializer.Deserialize(jsonString, supportedType) as ValueAdapterEvent;
            }

            return null;
        }
    }
}
