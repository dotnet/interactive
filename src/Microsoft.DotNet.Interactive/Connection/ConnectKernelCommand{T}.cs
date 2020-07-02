// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Connection
{
    public abstract class ConnectKernelCommand<TOptions> :
        Command
        where TOptions : KernelConnectionOptions
    {
        protected ConnectKernelCommand(string name, string description) :
            base(name, description)
        {
        }

        public abstract Task<Kernel> CreateKernelAsync(
            TOptions options,
            KernelInvocationContext context);
    }
}