﻿// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Microsoft.DotNet.Interactive.Formatting.TabularData
{
    [TypeFormatterSource(typeof(TabularDataFormatterSource))]
    public class TabularDataResource
    {
        public TabularDataResource(
            TableSchema schema, IEnumerable<IEnumerable<KeyValuePair<string, object>>> data)
        {
            Profile = "tabular-data-resource";
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public string Profile { get; }

        public TableSchema Schema { get; }

        public IEnumerable<IEnumerable<KeyValuePair<string, object>>> Data { get; }

        public TabularDataResourceJsonString ToJsonString()
        {
            return new(JsonSerializer.Serialize(this, TabularDataResourceFormatter.JsonSerializerOptions));
        }
    }
}