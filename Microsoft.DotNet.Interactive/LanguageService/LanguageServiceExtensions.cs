// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Web;
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

        private const string DataUriPrefix = "data:";

        public static string DecodeDocumentFromDataUri(this string dataUri)
        {
            // from https://developer.mozilla.org/en-US/docs/Web/HTTP/Basics_of_HTTP/Data_URIs, basic format is
            // data:[<mime_type>][;base64],<data>
            if (!dataUri.StartsWith(DataUriPrefix))
            {
                throw new FormatException($"Expected string to start with '{DataUriPrefix}'");
            }

            var headerEnd = dataUri.IndexOf(',');
            if (headerEnd < 0)
            {
                throw new FormatException("Expected ',' before data.");
            }

            var header = dataUri.Substring(DataUriPrefix.Length, headerEnd - DataUriPrefix.Length);
            var isBase64Encoded = header.EndsWith(";base64");

            var data = dataUri.Substring(headerEnd + 1);
            if (isBase64Encoded)
            {
                var bytes = Convert.FromBase64String(data);
                data = System.Text.Encoding.UTF8.GetString(bytes);
            }
            else
            {
                data = HttpUtility.UrlDecode(data);
            }

            return data;
        }
    }
}
