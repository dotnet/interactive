// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Microsoft.DotNet.Interactive.App.Lsp
{
    public static class LspSerializer
    {
        public static readonly JsonSerializerSettings JsonSerializerSettings;

        public static JsonSerializer JsonSerializer { get; }

        static LspSerializer()
        {
            JsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Newtonsoft.Json.Formatting.None,
                MissingMemberHandling = MissingMemberHandling.Error,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            };
            JsonSerializerSettings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));

            JsonSerializer = JsonSerializer.Create(JsonSerializerSettings);
        }
    }
}
