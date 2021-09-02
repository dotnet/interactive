using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.SqlServer
{
    public class KustoConnectionOptions : KernelConnectionOptions
    {
        public string Cluster { get; set; }
        public string Database { get; set; }
    }
}