// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Formatting.TabularData;

[JsonConverter(typeof(TableSchemaFieldTypeJsonConverter))]
public enum TableSchemaFieldType
{
    Any,
    Object,
    Null,
    Number,
    Integer,
    Boolean,
    String,
    Array,
    DateTime,
    GeoPoint,
    GeoJson
}