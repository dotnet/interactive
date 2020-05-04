// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    public class InputOutputHistoryElementConverter : JsonConverter<InputOutputHistoryElement>
    {
        public override bool CanRead { get; } = true;
        public override bool CanWrite { get; } = true;

        public override void WriteJson(JsonWriter writer, InputOutputHistoryElement value, JsonSerializer serializer)
        {
            var tuple = new JArray(value.Session, value.LineNumber, new JArray(value.Input, value.Output));
            serializer.Serialize(writer, tuple);
        }

        public override InputOutputHistoryElement ReadJson(JsonReader reader, Type objectType, InputOutputHistoryElement existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            var source = serializer.Deserialize<JArray>(reader);
            if (source.Count != 3)
            {
                throw new FormatException("the input must be a tuple with 3 elements");
            }

            var session = source[0].Value<int>();
            var lineNumber = source[1].Value<int>();
            var tuple = source[2].Values<string>().ToArray();

            return new InputOutputHistoryElement(session,lineNumber, tuple[0], tuple[1]);
        }
    }
}