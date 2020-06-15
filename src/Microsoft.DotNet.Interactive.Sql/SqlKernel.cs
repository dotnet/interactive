// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.LanguageService;

namespace Microsoft.DotNet.Interactive.Sql
{
    public class SqlKernel :
        KernelBase,
        IKernelCommandHandler<SubmitCode>
    {
        internal const string DefaultKernelName = "sql";
        public SqlKernel(): base(DefaultKernelName)
        {
           
        }

        public async Task HandleAsync(SubmitCode command, KernelInvocationContext context)
        {
            // SEND Sql query
            // await completion
            // Get results
            // Display
            await context.DisplayAsync("HELLO WORLD");
        }

    }
}