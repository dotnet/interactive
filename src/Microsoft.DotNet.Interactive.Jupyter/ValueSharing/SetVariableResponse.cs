using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing
{
    public class SetVariableResponseBody : IValueAdapterResponseBody
    {
        /* 
         * If the name is changed by kernel due to identifier constraints, the new name is to be returned in the response
         * body. For streaming, the new name should be used for new chunks in the request.
         */
        [JsonPropertyName("name")]
        public string Name { get; }

        [JsonPropertyName("type")]
        public string Type { get; }

        public SetVariableResponseBody(string name, string type)
        {
            Name = name;
            Type = type;
        }
    }

    [ValueAdapterMessageType(ValueAdapterMessageType.Response)]
    [ValueAdapterCommand(ValueAdapterCommandTypes.SetVariable)]
    public class SetVariableResponse : ValueAdapterResponse<SetVariableResponseBody>
    {
        public SetVariableResponse(bool success, SetVariableResponseBody body) : base(success, body)
        {
        }
    }
}
