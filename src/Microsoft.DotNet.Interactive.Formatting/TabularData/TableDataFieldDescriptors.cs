// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Formatting.TabularData;
[JsonConverter(typeof(TableDataFieldDescriptorsJsonConverter))]
public class TableDataFieldDescriptors : IReadOnlyCollection<TableSchemaFieldDescriptor>
{
    private readonly Dictionary<string, TableSchemaFieldDescriptor> _descriptors = new();

    public TableSchemaFieldDescriptor this[string name] => _descriptors[name];

    public bool Contains(string name) => _descriptors.ContainsKey(name);

    public void Add(TableSchemaFieldDescriptor descriptor)
    {
        _descriptors.Add(descriptor.Name, descriptor);
    }

    public IEnumerator<TableSchemaFieldDescriptor> GetEnumerator() => _descriptors.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => _descriptors.Count;
}