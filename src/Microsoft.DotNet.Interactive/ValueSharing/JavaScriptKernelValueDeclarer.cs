// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting.TabularData;

namespace Microsoft.DotNet.Interactive.ValueSharing
{
    public class JavaScriptKernelValueDeclarer : IKernelValueDeclarer
    {
        private static readonly JsonSerializerOptions _serializerOptions;

        static JavaScriptKernelValueDeclarer()
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

        public bool TryGetValueDeclaration(string valueName, object value, out KernelCommand command)
        {
            var code = $"{valueName} = {JsonSerializer.Serialize(value, _serializerOptions)};";
            command = new SubmitCode(code);
            return true;
        }
    }
}