// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Notebook;


namespace Microsoft.DotNet.Interactive.Jupyter.ZMQ
{
    public static class MetadataExtensions
    {
        public static Dictionary<string, object> DeserializeMetadataFromJsonString(string metadataJson)
        {
            var metadata = new Dictionary<string, object>();
            var doc = JsonDocument.Parse(metadataJson);
            foreach (var property in doc.RootElement.EnumerateObject())
            {
                switch (property.Name)
                {
                    case "dotnet_interactive":
                        metadata[property.Name] = JsonSerializer.Deserialize<InputCellMetadata>( property.Value.GetRawText() );
                        break;

                    default:
                        metadata[property.Name] = property.Value.ToObject();
                        break;
                }
            }

            return metadata;
        }
    }
}
