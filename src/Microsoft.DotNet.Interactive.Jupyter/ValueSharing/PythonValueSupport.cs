// using Microsoft.Build.Utilities;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.ValueSharing;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing
{
    internal class PythonValueSupport : IKernelValueDeclarer, ISupportGetValue
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
            var results = new List<KernelValueInfo>();

            var code = @"
_rwho_ls = %who_ls
_locals = locals()
print([{""name"": x, ""type"":  type(_locals[x])} for x in _rwho_ls ])";
            var kernelRequest = Messaging.Message.Create(new ExecuteRequest(code));

            Protocol.Message kernelResults = null;
            Task.Run(async () =>
            {
                kernelResults = await RunOnKernel(code);
            }).ContinueWith((t) =>
            {
                if (t.IsFaulted) throw t.Exception;
            }).Wait();

            if (kernelResults is Error error)
            {
                throw new System.Exception(error.EValue);
            }

            if (kernelResults is Stream streamResult)
            {
            }
            return results;
        }

        public bool TryGetValue<T>(string name, out T value)
        {
            var code = $"import pandas; import json; print({name}.to_json(orient='records') if isinstance({name}, pandas.DataFrame) else json.dumps(x))";
            
            Protocol.Message kernelResults = null;
            Task.Run(async () =>
            {
                kernelResults = await RunOnKernel(code);
            }).ContinueWith((t) =>
            {
                if (t.IsFaulted) throw t.Exception;
            }).Wait();

            if (kernelResults is Error error)
            {
                throw new System.Exception(error.EValue);
            }

            if (kernelResults is Stream streamResult)
            {
                value = JsonSerializer.Deserialize<T>(streamResult.Text);
                return true;
            }

            value = default;
            return false;
        }

        public bool TryGetValueDeclaration(ValueProduced valueProduced, out KernelCommand command)
        {
            if (valueProduced.Value is IList<TabularDataResource> value && value.Count == 1)
            {
                var pythonCode = $@"
try:
    import pandas as pd
    {valueProduced.Name} = pd.read_json('{JsonSerializer.Serialize(value[0].Data, _serializerOptions).Replace("'", "''")}', orient='records')
except ModuleNotFoundError:
    import json
    {valueProduced.Name} = json.loads('{JsonSerializer.Serialize(value[0].Data, _serializerOptions).Replace("'", "''")}')
";
                command = new SubmitCode(pythonCode);
                return true;
            }

            command = null;
            return false;
        }

        private async Task<Protocol.Message> RunOnKernel(string codeToExecute)
        {
            var kernelRequest = Messaging.Message.Create(new ExecuteRequest(codeToExecute));
            var kernelReply = _receiver.Messages.ChildOf(kernelRequest)
                                .SelectContent()
                                .TakeUntilMessageType(JupyterMessageContentTypes.ExecuteResult, JupyterMessageContentTypes.Stream, JupyterMessageContentTypes.Error)
                                .ToTask();

            await _sender.SendAsync(kernelRequest);
            var results = await kernelReply;

            return results;
        }
    }
}
