// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Formatting.TabularData;

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
            Converters = { new DataDictionaryConverter() }
        };
    }

    public static JsonSerializerOptions JsonSerializerOptions { get; }

    internal static ITypeFormatter[] DefaultFormatters { get; } = DefaultTabularDataFormatterSet.DefaultFormatters;

    public static TabularDataResource ToTabularDataResource<T>(this IEnumerable<T> source)
    {
        var (schema, data) = Generate(source);

        return new TabularDataResource(schema, data.ToArray());
    }

    private static (TableSchema schema, IEnumerable<IEnumerable<KeyValuePair<string, object>>> data) Generate<T>(IEnumerable<T> source)
    {
        var fields = new Dictionary<string, TableSchemaFieldType?>();
        var data = new List<IEnumerable<KeyValuePair<string, object>>>();
        IDestructurer destructurer = default;

        foreach (var item in source)
        {
            switch (item)
            {
                case IEnumerable<(string name, object value)> valueTuples:
                    {
                        var tuples = valueTuples.ToArray();

                        EnsureFieldsAreInitializedFromValueTuples(tuples);

                        var obj = new List<KeyValuePair<string, object>>();
                        foreach (var (name, value) in tuples)
                        {
                            obj.Add(new KeyValuePair<string, object>(name, value));
                        }

                        data.Add(obj);
                    }
                    break;

                case IEnumerable<KeyValuePair<string, object>> keyValuePairs:
                    {
                        var pairs = keyValuePairs.ToArray();

                        EnsureFieldsAreInitializedFromKeyValuePairs(pairs);

                        var obj = new List<KeyValuePair<string, object>>();
                        foreach (var pair in pairs)
                        {
                            obj.Add(new KeyValuePair<string, object>(pair.Key, pair.Value));
                        }

                        data.Add(obj);
                    }
                    break;

                default:
                    {
                        destructurer ??= Destructurer.GetOrCreate(item.GetType());

                        var dict = destructurer.Destructure(item);

                        EnsureFieldsAreInitializedFromKeyValuePairs(dict);

                        data.Add(dict);
                    }
                    break;
            }
        }

        var schema = new TableSchema();

        foreach (var field in fields)
        {
            schema.Fields.Add(new TableSchemaFieldDescriptor(field.Key, field.Value));
        }

        return (schema, data);

        void EnsureFieldsAreInitializedFromKeyValuePairs(IEnumerable<KeyValuePair<string, object>> keyValuePairs)
        {
            foreach (var keyValuePair in keyValuePairs)
            {
                if (!fields.ContainsKey(keyValuePair.Key))
                {
                    fields.Add(keyValuePair.Key, InferTableSchemaFieldTypeFromValue(keyValuePair.Value));
                }
                else if (InferTableSchemaFieldTypeFromValue(keyValuePair.Value) is { } type &&
                         type != TableSchemaFieldType.Null)
                {
                    fields[keyValuePair.Key] = type;
                }
            }
        }

        void EnsureFieldsAreInitializedFromValueTuples(IEnumerable<(string name, object value)> valueTuples) => EnsureFieldsAreInitializedFromKeyValuePairs(valueTuples.Select(t => new KeyValuePair<string, object>(t.name, t.value)));

        TableSchemaFieldType? InferTableSchemaFieldTypeFromValue(object value) =>
            value switch
            {
                null => null,
                _ => value.GetType().ToTableSchemaFieldType()
            };
    }

    public static TableSchemaFieldType ToTableSchemaFieldType(this Type type) =>
        type switch
        {
            { } t when t == typeof(bool) => TableSchemaFieldType.Boolean,
            { } t when t == typeof(bool?) => TableSchemaFieldType.Boolean,
            { } t when t == typeof(DateTime) => TableSchemaFieldType.DateTime,
            { } t when t == typeof(DateTime?) => TableSchemaFieldType.DateTime,
            { } t when t == typeof(DateTimeOffset) => TableSchemaFieldType.DateTime,
            { } t when t == typeof(DateTimeOffset?) => TableSchemaFieldType.DateTime,
            { } t when t == typeof(int) => TableSchemaFieldType.Integer,
            { } t when t == typeof(int?) => TableSchemaFieldType.Integer,
            { } t when t == typeof(ushort) => TableSchemaFieldType.Integer,
            { } t when t == typeof(ushort?) => TableSchemaFieldType.Integer,
            { } t when t == typeof(uint) => TableSchemaFieldType.Integer,
            { } t when t == typeof(uint?) => TableSchemaFieldType.Integer,
            { } t when t == typeof(ulong) => TableSchemaFieldType.Integer,
            { } t when t == typeof(long) => TableSchemaFieldType.Integer,
            { } t when t == typeof(long?) => TableSchemaFieldType.Integer,
            { } t when t == typeof(float) => TableSchemaFieldType.Number,
            { } t when t == typeof(float?) => TableSchemaFieldType.Number,
            { } t when t == typeof(double) => TableSchemaFieldType.Number,
            { } t when t == typeof(double?) => TableSchemaFieldType.Number,
            { } t when t == typeof(decimal) => TableSchemaFieldType.Number,
            { } t when t == typeof(decimal?) => TableSchemaFieldType.Number,
            { } t when t == typeof(string) => TableSchemaFieldType.String,
            { } t when t == typeof(ReadOnlyMemory<char>) => TableSchemaFieldType.String,
            { IsArray: true } => TableSchemaFieldType.Array,
            _ => TableSchemaFieldType.Any
        };
}