// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.App.Connection;

/// <remarks>This kernel is used as a placeholder for the MSSQL kernel in order to enable SQL language coloring in the editor. Language grammars can only be defined for fixed kernel names, but MSSQL subkernels are user-defined via the #!connect magic command. So, this kernel is specified in addition to the user-defined kernel as a kind of "styling" kernel as well as to provide guidance and discoverability for SQL features.</remarks>
public class SqlDiscoverabilityKernel :
    Kernel,
    IKernelCommandHandler<SubmitCode>
{
    private readonly HashSet<string> _kernelNameFilter;
    public const string DefaultKernelName = "sql";

    public SqlDiscoverabilityKernel() : base(DefaultKernelName)
    {
        _kernelNameFilter =
        [
            "MsSqlKernel",
            "PostgreSqlKernel",
            "SQLiteKernel"
        ];
        KernelInfo.LanguageName = "SQL";
        KernelInfo.Description = """
                            Query a Microsoft SQL database
                            """;
    }

    Task IKernelCommandHandler<SubmitCode>.HandleAsync(SubmitCode command, KernelInvocationContext context)
    {
        var root = (Kernel)ParentKernel ?? this;

        var connectedSqlKernelNames = new HashSet<string>();

        root.VisitSubkernels(childKernel =>
        {
            if (_kernelNameFilter.Contains(childKernel.GetType().Name))
            {
                connectedSqlKernelNames.Add(childKernel.Name);
            }
        });

        var codeSample = !string.IsNullOrWhiteSpace(command.Code)
                             ? command.Code
                             : "SELECT TOP * FROM ...";

        if (connectedSqlKernelNames.Count is 0)
        {
            var version = PackageAcquisition.InferCompatiblePackageVersion();

            context.Display(
                HTML(
                    $"""
                     <p>A SQL connection has not been established.</p>
                     <p>To connect to a database, first add the SQL extension package by running the following in a C# cell:</p>
                     <code>
                         <pre>
                         #r "nuget:Microsoft.DotNet.Interactive.SqlServer,{version}"
                         </pre>
                     </code>
                     Now, you can connect to a Microsoft SQL Server database by running the following in a C# cell:
                     <code>
                         <pre>
                         #!connect mssql --kernel-name mydatabase "Persist Security Info=False; Integrated Security=true; Initial Catalog=MyDatabase; Server=localhost"
                         </pre>
                     </code>
                     <p>Once a connection is established, you can send SQL statements by prefixing them with the magic command for your connection.</p>
                     <code>
                         <pre>
                     #!sql-mydatabase
                     {codeSample}
                         </pre>
                     </code>

                     """), "text/html");
        }
        else
        {
            PocketView view =
                div(
                    p("You can send SQL statements to one of the following connected SQL kernels:"),
                    connectedSqlKernelNames.Select(
                        name =>
                            code(
                                pre($"#!{name}\n{codeSample}"))));

            context.Display(view);
        }
        
        if (!string.IsNullOrWhiteSpace(command.Code))
        {
            context.Fail(command, message: "SQL statements cannot be executed in this kernel.");
        }

        return Task.CompletedTask;
    }
}