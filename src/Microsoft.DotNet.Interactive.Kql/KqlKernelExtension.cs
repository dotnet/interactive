// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.SqlServer;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.Kql;

public class KqlKernelExtension
{
    public static async Task LoadAsync(Kernel kernel)
    {
        if (kernel is CompositeKernel compositeKernel)
        {
            var kqlToolName = "MicrosoftKustoServiceLayer";
            await Utils.CheckAndInstallGlobalToolAsync(kqlToolName, "1.3.0", "Microsoft.SqlServer.KustoServiceLayer.Tool");

            var kqlToolPath = Path.Combine(Paths.DotnetToolsPath, kqlToolName);
            compositeKernel
                .AddKernelConnector(new ConnectKqlCommand(kqlToolPath));

            KernelInvocationContext.Current?.Display(
                new HtmlString(@"<details><summary>Query Microsoft Kusto Server databases.</summary>
        <p>This extension adds support for connecting to Microsoft Kusto Server databases using the <code>#!connect kql</code> magic command. For more information, run a cell using the <code>#!kql</code> magic command.</p>
        </details>"),
                "text/html");
        }
    }
}