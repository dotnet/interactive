using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing
{
    [ValueAdapterMessageType(ValueAdapterMessageType.Request)]
    [ValueAdapterCommand(ValueAdapterCommandTypes.Variables)]
    public class VariablesRequest : ValueAdapterRequest<IValueAdapterRequestArguments>
    {
        public VariablesRequest(): base(null)
        {
        }
    }
}
