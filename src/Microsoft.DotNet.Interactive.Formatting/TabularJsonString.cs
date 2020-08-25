// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public class TabularJsonString : JsonString
    {
        public TabularJsonString(string json)
            : base(json)
        {
        }

        public static TabularJsonString Create(IReadOnlyDictionary<string , Type> fields, IEnumerable data)
        {
            var schema = new TabularDataSchema();

            foreach (var entry in fields)
            {
               schema.Fields.Add(new TabularDataSchemaField(entry.Key, entry.Value.ToTableFieldType())); 
            }
            var tabularDataSet = new TabularDataSet(schema, data);

            return tabularDataSet.ToJson();
        }
    }
}