// Copyright(c).NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Formatting;

namespace System.Text.Json
{
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

            var collection = new List<object>();

            foreach (var element in jsonElement.EnumerateArray())
            {
                var dataObject = JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText(),
                    TabularDataResourceFormatter.JsonSerializerOptions);
                collection.Add(dataObject);
            }

            return collection.ToTabularDataResource();
        }
    }
}