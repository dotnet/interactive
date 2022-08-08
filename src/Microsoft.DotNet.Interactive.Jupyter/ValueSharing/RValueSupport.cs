using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.ValueSharing;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing
{
    internal class RValueSupport : IKernelValueDeclarer
    {
        private static readonly JsonSerializerOptions _serializerOptions;
        private readonly IMessageSender _sender;
        private readonly IMessageReceiver _receiver;

        static RValueSupport()
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

        public RValueSupport(IMessageSender sender, IMessageReceiver receiver)
        {
            _receiver = receiver;
            _sender = sender;
        }

        public bool TryGetValueDeclaration(ValueProduced valueProduced, out KernelCommand command)
        {
            if (valueProduced.Value is IList<TabularDataResource> value && value.Count == 1)
            {
                var code = $@"library(jsonlite); {valueProduced.Name} <- data.frame(fromJSON('{JsonSerializer.Serialize(value[0].Data, _serializerOptions).Replace("'", "\\'")}'))";
                command = new SubmitCode(code);
                return true;
            }

            command = null;
            return false;
        }
    }
}
