// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting.TabularData;

namespace Microsoft.DotNet.Interactive.ValueSharing;

internal class JavaScriptValueDeclarer 
{
    // FIX: (JavaScriptValueDeclarer) refactor away / make internal

    private static readonly JsonSerializerOptions _serializerOptions;

    static JavaScriptValueDeclarer()
    {
        _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            NumberHandling = JsonNumberHandling.AllowReadingFromString |
                             JsonNumberHandling.AllowNamedFloatingPointLiterals,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters =
            {
                new TableSchemaFieldTypeConverter(),
                new TabularDataResourceConverter(),
                new DataDictionaryConverter()
            }
        };
    }

    public bool TryGetValueDeclaration(
        ValueProduced valueProduced,
        string declareAsName,
        out SubmitCode command)
    {
        if (valueProduced.Value is { } value)
        {
            var code = $"{valueProduced.Name} = {JsonSerializer.Serialize(value, _serializerOptions)};";
            command = new SubmitCode(code);
            return true;
        }

        // FIX: (TryGetValueDeclaration) handle application/json

        command = null;
        return false;
    }
}