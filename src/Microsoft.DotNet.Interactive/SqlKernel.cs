// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive
{
    /* This kernel is used as a placeholder for the MSSQL kernel in order to enable SQL language coloring
* in the editor. Language grammars can only be defined for fixed kernel names, but MSSQL subkernels
* are user-defined via the #!connect magic command. So, this kernel is specified in addition to the
* user-defined kernel as a kind of "styling" kernel.
*/
    public class SQLKernel :
        Kernel,
        IKernelCommandHandler<SubmitCode>
    {
        private readonly HashSet<string> _kernelNameFilter;
        public const string DefaultKernelName = "sql";

        public SQLKernel() : base(DefaultKernelName)
        {
            _kernelNameFilter = new HashSet<string>
            {
                "MsSqlKernel",
                "SQLiteKernel"
            };
        }

        public Task HandleAsync(SubmitCode command, KernelInvocationContext context)
        {

            var root = (Kernel)ParentKernel ?? this;

            var mssqlKernelNames = new HashSet<string>();
            

            root.VisitSubkernelsAndSelf(childKernel =>
            {
                if (_kernelNameFilter.Contains( childKernel.GetType().Name))
                {
                    mssqlKernelNames.Add(childKernel.Name);
                }
            });

            if (mssqlKernelNames.Count == 0)
            {
                context.Display(HTML(@"
<p>A SQL connection has not been established.</p>
<p>To connect to a database, first add the SQL extension package by running the following in a C# cell:</p>
<code>
    <pre>
    #r ""nuget:Microsoft.DotNet.Interactive.SqlServer,*-*""
    </pre>
</code>
Now, you can connect to a Microsoft SQL Server database by running the following in a C# cell:
<code>
    <pre>
    #!connect mssql --kernel-name mydatabase ""Persist Security Info=False; Integrated Security=true; Initial Catalog=MyDatabase; Server=localhost""
    </pre>
</code>
<p>Once a connection is established, you can send SQL statements by prefixing them with the magic command for your connection.</p>
<code>
    <pre>
    #!sql-mydatabase
    SELECT * FROM MyDatabase.MyTable
    </pre>
</code>
"), "text/html");
            }
            else if(!string.IsNullOrWhiteSpace(command.Code))
            {
                context.Display($@"
Submit SQL statements to one of the following SQL connections.

- {string.Join("\n- ",mssqlKernelNames.Select(n => $"`#!{n}`"))}
", "text/markdown");
            }
            return Task.CompletedTask;
        }
    }
}