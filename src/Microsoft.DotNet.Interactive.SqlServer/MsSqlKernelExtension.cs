// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.SqlServer;

public class MsSqlKernelExtension : IKernelExtension
{
    public async Task OnLoadAsync(Kernel kernel)
    {
        if (kernel is CompositeKernel compositeKernel)
        {
            // Check if the required Sql Tools Service tool is installed, and then install it if necessary
            var dotnet = new Dotnet();
            var installedGlobalTools = await dotnet.ToolList();
            const string sqlToolName = "MicrosoftSqlToolsServiceLayer";
            bool sqlToolInstalled = installedGlobalTools.Any(tool => string.Equals(tool, sqlToolName, StringComparison.InvariantCultureIgnoreCase));
            if (!sqlToolInstalled)
            {
                var commandLineResult = await dotnet.ToolInstall("Microsoft.SqlServer.SqlToolsServiceLayer.Tool", null, null, "1.0.0");
                commandLineResult.ThrowOnFailure();
            }

            compositeKernel
                .AddKernelConnector(new ConnectMsSqlCommand(sqlToolName));

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