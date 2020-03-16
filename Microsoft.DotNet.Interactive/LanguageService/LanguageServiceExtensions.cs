// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.DotNet.Interactive.LanguageService
{
    public static class LanguageServiceExtensions
    {
        private static readonly JsonSerializerSettings LspSerializerSettings = new JsonSerializerSettings()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
        };

        private static readonly JsonSerializer _jsonSerializer = JsonSerializer.CreateDefault(LspSerializerSettings);

        public static string SerializeLspObject(this object obj)
        {
            return (obj is { })
                ? JsonConvert.SerializeObject(obj, LspSerializerSettings)
                : string.Empty;
        }

        public static T ToLspObject<T>(this JObject obj)
        {
            return obj.ToObject<T>(_jsonSerializer);
        }
    }
}
