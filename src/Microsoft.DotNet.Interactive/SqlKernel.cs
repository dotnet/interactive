// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive
{
    /* This kernel is used as a placeholder for the MSSQL kernel in order to enable SQL language coloring
    * in the editor. Language grammars can only defined for fixed kernel names, but MSSQL subkernels
    * are user-defined via the #!connect magic command. So, this kernel is specified in addition to the
    * user-defined kernel as a kind of "styling" kernel.
    */
    public class SqlKernel :
        Kernel,
        IKernelCommandHandler<SubmitCode>
    {
        public const string DefaultKernelName = "sql";

        public SqlKernel() : base(DefaultKernelName)
        {
        }

        public Task HandleAsync(SubmitCode command, KernelInvocationContext context)
        {
            var sqlKernel = this.FindKernel(kernel => kernel.Language?.ToUpper() == "SQL");
            if (sqlKernel == null)
            {
                context.Display(@"
A SQL kernel is not currently defined.

SQL kernels are provided as part of the Microsoft.DotNet.Interactive.SqlServer package, which can be installed by using the following nuget command:
#r Microsoft.DotNet.Interactive.SqlServer

Once installed, you can find more info about creating a SQL kernel and running queries by running the following help command:
#!connect mssql -h
                ");
            }
            return Task.CompletedTask;
        }
    }
}