// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public class TabularDataResourceJsonString : JsonString
    {
        public TabularDataResourceJsonString(string json)
            : base(json)
        {
        }

        public static TabularDataResourceJsonString Create(IReadOnlyDictionary<string , Type> fields, IEnumerable data)
        {
            var schema = new TableSchema();

            foreach (var entry in fields)
            {
               schema.Fields.Add(new TableSchemaFieldDescriptor(entry.Key, entry.Value.ToTableSchemaFieldType())); 
            }
            var tabularDataSet = new TabularDataResource(schema, data);

            return tabularDataSet.ToJson();
        }
    }
}