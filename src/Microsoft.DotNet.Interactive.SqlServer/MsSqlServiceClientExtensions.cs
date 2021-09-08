using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.SqlServer
{
    public static class MsSqlServiceClientExtensions
    {
        public static async Task<bool> ConnectAsync(this MsSqlServiceClient serviceClient, Uri ownerUri, string connectionStr)
        {
            var connectionOptions = new Dictionary<string, string>();
            connectionOptions.Add("ConnectionString", connectionStr);

            var connectionDetails = new ConnectionDetails() {Options = connectionOptions};
            var connectionParams = new ConnectParams() {OwnerUri = ownerUri.AbsolutePath, Connection = connectionDetails};

            return await serviceClient.ConnectAsync(connectionParams);
        }
        
        public static async Task<bool> ConnectAsync(this MsSqlServiceClient serviceClient, Uri ownerUri, KqlConnectionDetails kqlDetails)
        {
            var connectionOptions = new Dictionary<string, string>
            {
                {"server", kqlDetails.Cluster},
                {"database", kqlDetails.Database},
                {"azureAccountToken", kqlDetails.Token},
                {"authenticationType", kqlDetails.AuthenticationType}
            };
            
            var connectionParams = new ConnectParams
            {
                OwnerUri = ownerUri.AbsolutePath, 
                Connection = new ConnectionDetails
                {
                    Options = connectionOptions
                }
            };
            
            return await serviceClient.ConnectAsync(connectionParams);
        }
    }
}