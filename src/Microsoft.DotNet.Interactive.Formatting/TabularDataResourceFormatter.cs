// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Formatting
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

        public static TabularDataResourceJsonString ToTabularDataResourceJsonString<T>(this IEnumerable<T> source)
        {
            var tabularDataSet = source.ToTabularDataResource();
            return tabularDataSet.ToJson();
        }

        public static TabularDataResource ToTabularDataResource<T>(this IEnumerable<T> source)
        {
            var (schema, data) = Generate(source);
           return new TabularDataResource(schema, data);
        }

        private static (TableSchema schema, IEnumerable data) Generate<T>(IEnumerable<T> source)
        {
            var schema = new TableSchema();
            var fields = new HashSet<string>();
            var members = new HashSet<(string name, Type type)>();
            var data = new List<object>();

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
                            foreach (var memberInfo in item
                                .GetType()
                                .GetMembers(BindingFlags.Public | BindingFlags.Instance))
                            {
                                switch (memberInfo)
                                {
                                    case PropertyInfo pi:
                                        members.Add((memberInfo.Name, pi.PropertyType));
                                        break;
                                    case FieldInfo fi:
                                        members.Add((memberInfo.Name, fi.FieldType));
                                        break;
                                }
                            }

                            EnsureFieldsAreInitializedFromMembers();
                            data.Add(item);
                        }
                        break;
                }
            }

            return (schema, data);

            void EnsureFieldsAreInitializedFromMembers()
            {
                foreach (var memberInfo in members)
                {
                    if (fields.Add(memberInfo.name))
                    {
                        schema.Fields.Add(new TableSchemaFieldDescriptor(memberInfo.name, memberInfo.type.ToTableSchemaFieldType()));
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
                        schema.Fields.Add(new TableSchemaFieldDescriptor(keyValuePair.Key, keyValuePair.Value?.GetType().ToTableSchemaFieldType()));
                    }

                }
            }
        }

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
                { } t when t == typeof(float) => TableSchemaFieldType.Number,
                { } t when t == typeof(double) => TableSchemaFieldType.Number,
                { } t when t == typeof(decimal) => TableSchemaFieldType.Number,
                { } t when t == typeof(string) => TableSchemaFieldType.String,
                { } t when t == typeof(ReadOnlyMemory<char>) => TableSchemaFieldType.String,
                _ => TableSchemaFieldType.Any
            };

    }
}