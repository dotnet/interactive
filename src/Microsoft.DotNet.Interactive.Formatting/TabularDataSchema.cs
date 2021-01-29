// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal class TabularDataSchema
    {
        [JsonPropertyName("primaryKey")]
        public List<string> PrimaryKey { get; } = new List<string>();

        [JsonPropertyName("fields")]
        public TabularDataFieldList Fields { get; } = new TabularDataFieldList();
    }
}