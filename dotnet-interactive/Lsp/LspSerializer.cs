// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.DotNet.Interactive.App.Lsp
{
    public static class LspSerializer
    {
        public static readonly JsonSerializerSettings JsonSerializerSettings;

        public static JsonSerializer JsonSerializer { get; }

        static LspSerializer()
        {
            JsonSerializerSettings = CreateDefaultSettings();
            JsonSerializer = JsonSerializer.Create(JsonSerializerSettings);
        }

        private static JsonSerializerSettings CreateDefaultSettings()
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Newtonsoft.Json.Formatting.None,
                MissingMemberHandling = MissingMemberHandling.Error,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            };

            settings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));
            return settings;
        }

        public static bool TryToObject<T>(this JToken token, out T result)
        {
            // TODO: is there a faster way to do this?
            var foundError = false;
            var settings = CreateDefaultSettings();
            settings.Error = (sender, e) =>
            {
                foundError = true;
                e.ErrorContext.Handled = true;
            };
            var serializer = JsonSerializer.Create(settings);

            result = token.ToObject<T>(serializer);
            return !foundError;
        }
    }
}
