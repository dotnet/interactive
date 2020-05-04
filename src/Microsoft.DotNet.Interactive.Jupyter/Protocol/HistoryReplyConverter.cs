// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// using System;

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    public class HistoryReplyConverter : JsonConverter<HistoryReply> {
        public override bool CanRead { get; } = true;
        public override bool CanWrite { get; } = true;

        public override void WriteJson(JsonWriter writer, HistoryReply value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("history");
            serializer.Serialize(writer,value.History);
            writer.WriteEndObject();
        }

        public override HistoryReply ReadJson(JsonReader reader, Type objectType, HistoryReply existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            var obj = serializer.Deserialize<JObject>(reader);
            var historyProperty = obj.Property("history").Value.Values<JArray>();
            var history = new List<HistoryElement>();
            foreach (var entry in historyProperty)
            {
                if (entry.Count != 3)
                {
                    throw new FormatException("invalid history element format");
                }

                history.Add(entry[2].Type == JTokenType.Array
                    ? entry.ToObject<InputOutputHistoryElement>()
                    : entry.ToObject<InputHistoryElement>());
            }
            
            return new HistoryReply(history);
        }
    }
}