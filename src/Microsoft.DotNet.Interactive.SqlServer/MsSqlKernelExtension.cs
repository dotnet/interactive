// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;

namespace Microsoft.DotNet.Interactive.SqlServer;

public class MsSqlKernelExtension : IKernelExtension
{
    public async Task OnLoadAsync(Kernel kernel)
    {
        if (kernel is CompositeKernel compositeKernel)
        {
            var sqlToolName = "MicrosoftSqlToolsServiceLayer";
            await Utils.CheckAndInstallGlobalToolAsync(sqlToolName, "1.2.0", "Microsoft.SqlServer.SqlToolsServiceLayer.Tool");

            var userFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
            var sqlToolPath = Path.Join(userFolder, ".dotnet", "tools", sqlToolName);
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