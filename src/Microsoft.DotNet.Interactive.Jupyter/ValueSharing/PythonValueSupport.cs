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
    internal class PythonValueSupport : IValueSupport, IKernelValueDeclarer, ISupportGetValue
    {
        private static readonly JsonSerializerOptions _serializerOptions;
        private readonly IMessageSender _sender;
        private readonly IMessageReceiver _receiver;

        static PythonValueSupport()
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

        public PythonValueSupport(IMessageSender sender, IMessageReceiver receiver)
        {
            _receiver = receiver;
            _sender = sender;
        }

        public IReadOnlyCollection<KernelValueInfo> GetValueInfos()
        {
            throw new System.NotImplementedException();
        }

        public bool TryGetValue<T>(string name, out T value)
        {
            throw new System.NotImplementedException();
        }

        public bool TryGetValueDeclaration(ValueProduced valueProduced, out KernelCommand command)
        {
            if (valueProduced.Value is IList<TabularDataResource> value && value.Count == 1)
            {
                var pythonCode = $@"
try:
    import pandas as pd
    {valueProduced.Name} = pd.read_json('{JsonSerializer.Serialize(value[0].Data, _serializerOptions)}', orient='records')
except ModuleNotFoundError:
    import json
    {valueProduced.Name} = json.loads('{JsonSerializer.Serialize(value[0].Data, _serializerOptions)}')
";
                command = new SubmitCode(pythonCode);
                return true;
            }

            command = null;
            return false;
        }
    }
}
