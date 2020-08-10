// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Connection
{
    /// <summary>
    /// Defines a magic command that can be used to connect a subkernel dynamically.
    /// </summary>
    /// <typeparam name="TOptions">The type bound to which custom command line options passed with the connect command will be bound.</typeparam>
    public abstract class ConnectKernelCommand<TOptions> :
        Command
        where TOptions : KernelConnectionOptions
    {
        protected ConnectKernelCommand(
            string name, 
            string description) :
            base(name, description)
        {
        }

        /// <summary>
        /// Description used for the kernel connected using this command.
        /// </summary>
        public string ConnectedKernelDescription { get; set; }

        /// <summary>
        /// Creates a kernel instance when this connection command is invoked.
        /// </summary>
        /// <param name="options">The options passed with the connect command.</param>
        /// <param name="context">The <see cref="KernelInvocationContext"/> for the current command.</param>
        /// <returns>A new <see cref="Kernel"/> instance to be added to the <see cref="CompositeKernel"/>.</returns>
        public abstract Task<Kernel> CreateKernelAsync(
            TOptions options,
            KernelInvocationContext context);
    }
}