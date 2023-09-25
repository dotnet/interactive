// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.SqlServer;

public class MsSqlKernelExtension
{
    public static async Task LoadAsync(Kernel kernel)
    {
        if (kernel is CompositeKernel compositeKernel)
        {
            var sqlToolName = "MicrosoftSqlToolsServiceLayer";
            await Utils.CheckAndInstallGlobalToolAsync(sqlToolName, "1.2.0", "Microsoft.SqlServer.SqlToolsServiceLayer.Tool");

            var sqlToolPath = Path.Combine(Paths.DotnetToolsPath, sqlToolName);
            compositeKernel
                .AddKernelConnector(new ConnectMsSqlCommand(sqlToolPath));

            compositeKernel
                .SubmissionParser
                .SetInputTypeHint(typeof(MsSqlConnectionString), "connectionstring-mssql");

            KernelInvocationContext.Current?.Display(
                new HtmlString(@"<details><summary>Query Microsoft SQL Server databases.</summary>
<p>This extension adds support for connecting to Microsoft SQL Server databases using the <code>#!connect mssql</code> magic command. For more information, run a cell using the <code>#!sql</code> magic command.</p>
</details>"),
                "text/html");
        }
    }
}