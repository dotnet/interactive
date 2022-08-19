using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    internal class KernelSpec
    {
        [JsonPropertyName("argv")]
        public IReadOnlyList<string> CommandArguments { get; set; }

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("metadata")]
        public IReadOnlyDictionary<string, object> Metadata { get; set; }
    }
}
