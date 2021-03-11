// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public class TabularDataResource
    {
        public string Profile { get; }

        public TableSchema Schema { get; }

        public IEnumerable Data { get; }

        public TabularDataResource(TableSchema schema, IEnumerable data)
        {
            Profile = "tabular-data-resource";
            Schema = schema;
            Data = data;
        }

        public TabularDataResourceJsonString ToJson()
        {
            return new(JsonSerializer.Serialize(this, TabularDataResourceFormatter.JsonSerializerOptions));
        }
    }

    public class TabularDataResourceConverter : JsonConverter<TabularDataResource>
    {
        public override TabularDataResource? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, TabularDataResource value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("profile",value.Profile);
            writer.WritePropertyName("schema");
            JsonSerializer.Serialize(writer, value.Schema, options);
            writer.WritePropertyName("data");
            JsonSerializer.Serialize(writer, value.Data, options);
            writer.WriteEndObject();
        }
    }
}