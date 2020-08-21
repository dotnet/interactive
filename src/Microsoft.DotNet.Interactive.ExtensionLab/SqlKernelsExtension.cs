// // Copyright (c) .NET Foundation and contributors. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class SqlKernelsExtension : IKernelExtension
    {
        public Task OnLoadAsync(Kernel kernel)
        {
            if (kernel is CompositeKernel compositeKernel)
            {
                compositeKernel
                    .UseKernelClientConnection(new SQLiteKernelConnection())
                    .UseKernelClientConnection(new MsSqlKernelConnection());
            }

            return Task.CompletedTask;
        }
    }
}