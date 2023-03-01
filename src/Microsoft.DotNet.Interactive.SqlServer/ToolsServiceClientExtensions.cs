using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.SqlServer;

public static class ToolsServiceClientExtensions
{
    internal static async Task<bool> ConnectAsync(this ToolsServiceClient serviceClient, Uri ownerUri, string connectionStr)
    {
        var connectionOptions = new Dictionary<string, string>();
        connectionOptions.Add("ConnectionString", connectionStr);

        var connectionDetails = new ConnectionDetails {Options = connectionOptions};
        var connectionParams = new ConnectParams {OwnerUri = ownerUri.AbsolutePath, Connection = connectionDetails};

        return await serviceClient.ConnectAsync(connectionParams);
    }
}