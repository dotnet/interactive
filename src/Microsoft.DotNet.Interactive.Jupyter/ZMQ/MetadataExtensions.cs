// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Notebook;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.Jupyter.ZMQ
{
    public static class MetadataExtensions
    {
        public static Dictionary<string, object> DeserializeMetadataFromJsonString(string metadataJson)
        {
            var metadata = NetMQExtensions.DeserializeFromJsonString<Dictionary<string, object>>(metadataJson) ?? new Dictionary<string, object>();
            TryParseWellKnownMetadata(metadata);
            return metadata;
        }

        private static void TryParseWellKnownMetadata(Dictionary<string, object> metadata)
        {
            foreach (var kvp in metadata)
            {
                switch (kvp.Key)
                {
                    case "dotnet_interactive" when kvp.Value is JObject jo:
                        metadata[kvp.Key] = jo.ToObject<InputCellMetadata>();
                        break;
                }
            }
        }
    }
}
