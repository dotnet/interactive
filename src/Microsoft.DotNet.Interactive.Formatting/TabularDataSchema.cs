// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal class TabularDataSchema
    {
        [JsonProperty(PropertyName = "primaryKey")]
        public List<string> PrimaryKey { get; } = new List<string>();

        [JsonProperty(PropertyName = "fields")]
        public TabularDataFieldList Fields { get; } = new TabularDataFieldList();
    }
}