// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    public class InputHistoryElementConverter : JsonConverter<InputHistoryElement>
    {
        public override bool CanRead { get; } = true;
        public override bool CanWrite { get; } = true;

        public override void WriteJson(JsonWriter writer, InputHistoryElement value, JsonSerializer serializer)
        {
            var tuple = new JArray(value.Session, value.LineNumber, value.Input );
            serializer.Serialize(writer, tuple);
        }

        public override InputHistoryElement ReadJson(JsonReader reader, Type objectType, InputHistoryElement existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            var source = serializer.Deserialize<JArray>(reader);
            if (source.Count != 3)
            {
                throw new FormatException("the input must be a tuple with 3 elements");
            }

            var session = source[0].Value<int>();
            var lineNumber = source[1].Value<int>();
            var input = source[2].Value<string>();

            return new InputHistoryElement(session, lineNumber, input);
        }
    }
}