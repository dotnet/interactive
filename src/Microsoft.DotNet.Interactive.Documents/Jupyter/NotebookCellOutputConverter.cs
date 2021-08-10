// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.Documents.Jupyter
{
    public class NotebookCellOutputConverter : JsonConverter<InteractiveDocumentOutputElement>
    {
        public static JsonSerializerOptions InternalOptions { get; set; }

        public override InteractiveDocumentOutputElement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            EnsureStartObject(reader, typeToConvert);
            
            IDictionary<string, object> data = null;
            string text = null;
            string errorName = null;
            string errorValue = null;
            IEnumerable<string> stackTrace = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    switch (reader.GetString())
                    {
                        case "data":
                            data = JsonSerializer.Deserialize<IDictionary<string, object>>(ref reader, options);
                            break;
                        case "text":
                            text = JsonSerializer.Deserialize<string>(ref reader, options);
                            break;
                        case "errorName":
                            if (reader.Read() && reader.TokenType == JsonTokenType.String)
                            {
                                errorName = reader.GetString();
                            }
                            break;
                        case "errorValue":
                            if (reader.Read() && reader.TokenType == JsonTokenType.String)
                            {
                                errorValue = reader.GetString();
                            }
                            break;
                        case "stackTrace":
                            if (reader.Read() && reader.TokenType == JsonTokenType.StartArray)
                            {
                                stackTrace = JsonSerializer.Deserialize<string[]>(ref reader, options);
                            }
                            break;
                    }
                }
                else if (reader.TokenType == JsonTokenType.EndObject)
                {
                    if (text is not null)
                    {
                        return new InteractiveDocumentTextOutputElement(text);
                    }

                    if (data is not null)
                    {
                        return new InteractiveDocumentDisplayOutputElement(data);
                    }

                    if (errorName is not null && errorValue is not null && stackTrace is not null)
                    {
                        return new InteractiveDocumentErrorOutputElement(errorName, errorValue, stackTrace.ToArray());
                    }

                }
            }

            throw new JsonException($"Cannot deserialize {typeToConvert.Name}");
        }
    }
}