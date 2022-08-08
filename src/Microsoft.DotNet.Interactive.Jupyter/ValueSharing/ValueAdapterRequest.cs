using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing
{
    public interface IValueAdapterRequestArguments
    {
    }

    [ValueAdapterMessageType(ValueAdapterMessageType.Request)]
    public abstract class ValueAdapterRequest<T>: ValueAdapterCommandMessage where T : IValueAdapterRequestArguments
    {

        [JsonPropertyName("arguments")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public T Arguments { get; }


        public ValueAdapterRequest(T arguments) : base(ValueAdapterMessageType.Request)
        {
            Arguments = arguments;
        }
    }
}
