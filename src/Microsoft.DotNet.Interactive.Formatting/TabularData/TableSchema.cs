// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Formatting.TabularData;

public class TableSchema
{
    [JsonPropertyName("primaryKey")]
    public List<string> PrimaryKey { get; init; } = new();

    [JsonPropertyName("fields")]
    public TableDataFieldDescriptors Fields { get; init; } = new();
}