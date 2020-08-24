// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public static class TabularDataFormatter
    {
        // https://specs.frictionlessdata.io/table-schema/#language
        public const string MimeType = "application/table-schema+json";

        internal static ITypeFormatter[] DefaultFormatters { get; } = DefaultTabularDataFormatterSet.DefaultFormatters;


        public static TabularDataSet ToTabularDataSet(this IEnumerable source)
        {
            var (schema, data) = Generate(source);

            var tabularDataSet = new TabularDataSet(schema, data);

            return tabularDataSet;
        }

        public static TabularJsonString TabularJsonString(this IEnumerable source)
        {
           return source.ToTabularDataSet().ToJson();
        }

        private static (TabularDataSchema schema, JArray data) Generate(IEnumerable source)
        {
            var schema = new TabularDataSchema();
            var fields = new HashSet<string>();
            var members = new HashSet<(string name, Type type)>();
            var data = new JArray();

            foreach (var item in source)
            {
                switch (item)
                {
                    case IEnumerable<(string name, object value)> valueTuples:

                        var tuples = valueTuples.ToArray();

                        EnsureFieldsAreInitializedFromValueTuples(tuples);

                        var o = new JObject();
                        foreach (var tuple in tuples)
                        {
                            o.Add(tuple.name, JToken.FromObject(tuple.value ?? "NULL"));
                        }

                        data.Add(o);
                        break;

                    case IEnumerable<KeyValuePair<string, object>> keyValuePairs:

                        var pairs = keyValuePairs.ToArray();

                        EnsureFieldsAreInitializedFromKeyValuePairs(pairs);

                        var obj = new JObject();
                        foreach (var pair in pairs)
                        {
                            obj.Add(pair.Key, JToken.FromObject(pair.Value));
                        }

                        data.Add(obj);
                        break;

                    default:
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
                        data.Add(JObject.FromObject(item));
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
                            schema.Fields.Add(new TabularDataSchemaField(memberInfo.name, memberInfo.type.ToTableFieldType()));
                        }
                    }
            }

            void EnsureFieldsAreInitializedFromValueTuples(IEnumerable<(string name, object value)> valueTuples)
            {
                foreach (var (name, value) in valueTuples)
                {
                    if (fields.Add(name))
                    {
                        schema.Fields.Add(new TabularDataSchemaField(name, value?.GetType().ToTableFieldType()));
                    }
                }
            }

            void EnsureFieldsAreInitializedFromKeyValuePairs(IEnumerable<KeyValuePair<string, object>> keyValuePairs)
            {
                foreach (var keyValuePair in keyValuePairs)
                {
                    if (fields.Add(keyValuePair.Key))
                    {
                        schema.Fields.Add(new TabularDataSchemaField(keyValuePair.Key, keyValuePair.Value?.GetType().ToTableFieldType()));
                    }

                }
            }
        }

        private static string ToTableFieldType(this Type type) =>
            type switch
            {
                { } t when t == typeof(bool) => "boolean",
                { } t when t == typeof(DateTime) => "datetime",
                { } t when t == typeof(int) => "integer",
                { } t when t == typeof(UInt16) => "integer",
                { } t when t == typeof(UInt32) => "integer",
                { } t when t == typeof(UInt64) => "integer",
                { } t when t == typeof(long) => "integer",
                { } t when t == typeof(Single) => "number",
                { } t when t == typeof(float) => "number",
                { } t when t == typeof(double) => "number",
                { } t when t == typeof(decimal) => "number",
                { } t when t == typeof(string) => "string",
                { } t when t == typeof(ReadOnlyMemory<char>) => "string",
                _ => "any",
            };

    }
}