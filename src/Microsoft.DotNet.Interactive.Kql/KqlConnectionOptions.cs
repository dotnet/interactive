using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.Kql
{
    public class KqlConnectionOptions : KernelConnectionOptions
    {
        public string Cluster { get; set; }
        public string Database { get; set; }
    }
}