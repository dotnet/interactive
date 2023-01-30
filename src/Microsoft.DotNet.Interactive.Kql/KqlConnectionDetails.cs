namespace Microsoft.DotNet.Interactive.Kql;

public class KqlConnectionDetails
{
    public string Cluster { get; set; }
    public string Database { get; set; }
    public string Token { get; set; }
    public string AuthenticationType { get; set; } = "AzureMFA";
}