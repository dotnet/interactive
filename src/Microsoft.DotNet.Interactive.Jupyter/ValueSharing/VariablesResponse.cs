using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing
{
    public class Variable
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }
    public class VariablesResponseBody : IValueAdapterResponseBody
    {
        [JsonPropertyName("variables")]
        public IReadOnlyList<Variable> Variables { get; }

        public VariablesResponseBody(IReadOnlyList<Variable> variables)
        {
            Variables = variables;
        }
    }

    [ValueAdapterMessageType(ValueAdapterMessageType.Response)]
    [ValueAdapterCommand(ValueAdapterCommandTypes.Variables)]
    public class VariablesResponse : ValueAdapterResponse<VariablesResponseBody>
    {
        public VariablesResponse(bool success, VariablesResponseBody body) : base(success, body)
        {
        }
    }
}
