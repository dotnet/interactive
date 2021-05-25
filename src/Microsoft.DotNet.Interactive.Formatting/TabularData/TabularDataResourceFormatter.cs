// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Formatting.TabularData
{
    public static class TabularDataResourceFormatter
    {
        // https://specs.frictionlessdata.io/table-schema/#language
        public const string MimeType = "application/table-schema+json";

        static TabularDataResourceFormatter()
        {
            JsonSerializerOptions = new JsonSerializerOptions(JsonFormatter.SerializerOptions)
            {
                WriteIndented = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                ReferenceHandler = null,
                Converters =
                {
                    new TableSchemaFieldTypeConverter(),
                    new TabularDataResourceConverter(),
                    new DataDictionaryConverter()
                }
            };
        }

        public static JsonSerializerOptions JsonSerializerOptions { get; }

        internal static ITypeFormatter[] DefaultFormatters { get; } = DefaultTabularDataFormatterSet.DefaultFormatters;

        public static TabularDataResource ToTabularDataResource<T>(this IEnumerable<T> source)
        {
            var (schema, data) = Generate(source);

            return new TabularDataResource(schema, data);
        }

        private static (TableSchema schema, IEnumerable<IDictionary<string, object>> data) Generate<T>(IEnumerable<T> source)
        {
            var schema = new TableSchema();
            var fields = new HashSet<string>();
            var data = new List<IDictionary<string, object>>();
            IDestructurer destructurer = default;

            foreach (var item in source)
            {
                switch (item)
                {
                    case IEnumerable<(string name, object value)> valueTuples:
                    {
                        var tuples = valueTuples.ToArray();

                        EnsureFieldsAreInitializedFromValueTuples(tuples);

                        var obj = new Dictionary<string, object>();
                        foreach (var (name, value) in tuples)
                        {
                            obj.Add(name, value);
                        }

                        data.Add(obj);
                    }
                        break;

                    case IEnumerable<KeyValuePair<string, object>> keyValuePairs:
                    {
                        var pairs = keyValuePairs.ToArray();

                        EnsureFieldsAreInitializedFromKeyValuePairs(pairs);

                        var obj = new Dictionary<string, object>();
                        foreach (var pair in pairs)
                        {
                            obj.Add(pair.Key, pair.Value);
                        }

                        data.Add(obj);
                    }
                        break;

                    default:
                    {
                        destructurer ??= Destructurer.GetOrCreate(item.GetType());

                        var dict = destructurer.Destructure(item);

                        EnsureFieldsAreInitializedFromDictionary(dict);

                        data.Add(dict);
                    }
                        break;
                }
            }

            return (schema, data);

            // FIX: (Generate) these have a bug if the first item for a given field name is null

            void EnsureFieldsAreInitializedFromDictionary(IDictionary<string, object> dictionary)
            {
                foreach (var key in dictionary.Keys)
                {
                    if (fields.Add(key))
                    {
                        var type = dictionary[key]?.GetType();
                        schema.Fields.Add(new TableSchemaFieldDescriptor(key, type?.ToTableSchemaFieldType()));
                    }
                }
            }

            void EnsureFieldsAreInitializedFromValueTuples(IEnumerable<(string name, object value)> valueTuples)
            {
                foreach (var (name, value) in valueTuples)
                {
                    if (fields.Add(name))
                    {
                        schema.Fields.Add(new TableSchemaFieldDescriptor(name, value?.GetType().ToTableSchemaFieldType()));
                    }
                }
            }

            void EnsureFieldsAreInitializedFromKeyValuePairs(IEnumerable<KeyValuePair<string, object>> keyValuePairs)
            {
                foreach (var keyValuePair in keyValuePairs)
                {
                    if (fields.Add(keyValuePair.Key))
                    {
                        var fieldDescriptor = new TableSchemaFieldDescriptor(
                            keyValuePair.Key,
                            GetTableSchemaFieldType(keyValuePair.Value));

                        schema.Fields.Add(fieldDescriptor);
                    }
                }
            }
        }

        private static TableSchemaFieldType GetTableSchemaFieldType(object value)
        {
            return value switch
            {
                JsonElement j => j.ValueKind.ToTableSchemaFieldType(),
                null => TableSchemaFieldType.Null,
                _ => value.GetType().ToTableSchemaFieldType()
            };
        }

        public static TableSchemaFieldType ToTableSchemaFieldType(this JsonValueKind kind) =>
            kind switch
            {
                JsonValueKind.Undefined => TableSchemaFieldType.Any,
                JsonValueKind.Object => TableSchemaFieldType.Object,
                JsonValueKind.Array => TableSchemaFieldType.Array,
                JsonValueKind.String => TableSchemaFieldType.String,
                JsonValueKind.Number => TableSchemaFieldType.Number,
                JsonValueKind.True => TableSchemaFieldType.Boolean,
                JsonValueKind.False => TableSchemaFieldType.Boolean,
                JsonValueKind.Null => TableSchemaFieldType.Null,
                _ => TableSchemaFieldType.Any
            };

        public static TableSchemaFieldType ToTableSchemaFieldType(this Type type) =>
            type switch
            {
                { } t when t == typeof(bool) => TableSchemaFieldType.Boolean,
                { } t when t == typeof(DateTime) => TableSchemaFieldType.DateTime,
                { } t when t == typeof(int) => TableSchemaFieldType.Integer,
                { } t when t == typeof(ushort) => TableSchemaFieldType.Integer,
                { } t when t == typeof(uint) => TableSchemaFieldType.Integer,
                { } t when t == typeof(ulong) => TableSchemaFieldType.Integer,
                { } t when t == typeof(long) => TableSchemaFieldType.Integer,
                { } t when t == typeof(float) => TableSchemaFieldType.Number,
                { } t when t == typeof(double) => TableSchemaFieldType.Number,
                { } t when t == typeof(decimal) => TableSchemaFieldType.Number,
                { } t when t == typeof(string) => TableSchemaFieldType.String,
                { } t when t == typeof(ReadOnlyMemory<char>) => TableSchemaFieldType.String,
                _ => TableSchemaFieldType.Any
            };
    }
}