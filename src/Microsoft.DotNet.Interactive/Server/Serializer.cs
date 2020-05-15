// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Server
{
    public static class Serializer
    {
        public static readonly JsonSerializerSettings JsonSerializerSettings;

        static Serializer()
        {
            JsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Newtonsoft.Json.Formatting.None,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            JsonSerializerSettings.Converters.Add(new FileSystemInfoJsonConverter());
            
            JsonSerializer = JsonSerializer.Create(JsonSerializerSettings);
        }

        public static JsonSerializer JsonSerializer { get; }
    }
}