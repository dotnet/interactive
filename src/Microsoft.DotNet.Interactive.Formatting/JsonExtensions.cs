// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Formatting.TabularData;

namespace Microsoft.DotNet.Interactive.Formatting;

public static class JsonExtensions
{
    public static TabularDataResource ToTabularDataResource(this JsonDocument document)
    {
        return document.RootElement.ToTabularDataResource();
    }

    public static TabularDataResource ToTabularDataResource(this JsonElement jsonElement)
    {
        if (jsonElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("input must be a valid array of object");
        }

        var rows = new List<IEnumerable<KeyValuePair<string, object>>>();

        foreach (var element in jsonElement.EnumerateArray())
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                var row = new List<KeyValuePair<string, object>>();

                foreach (var property in element.EnumerateObject())
                {
                    row.Add(new KeyValuePair<string, object>(                  
                        property.Name,
                        property.Value.ValueKind switch
                        {
                            JsonValueKind.String => property.Value.GetString(),
                            JsonValueKind.Number => property.Value.GetSingle(),
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.Null => null,
                            JsonValueKind.Undefined => property.Value,
                            JsonValueKind.Object => property.Value,
                            JsonValueKind.Array => property.Value,
                            _ => throw new ArgumentOutOfRangeException()
                        }));
                }

                rows.Add(row);
            }
        }

        return rows.ToTabularDataResource();
    }
}