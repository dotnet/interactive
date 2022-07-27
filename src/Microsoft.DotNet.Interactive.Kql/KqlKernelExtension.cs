// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.SqlServer;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.Kql
{
    public class KqlKernelExtension : IKernelExtension
    {
        public async Task OnLoadAsync(Kernel kernel)
        {
            if (kernel is CompositeKernel compositeKernel)
            {
                // Check if the required Sql Tools Service tool is installed, and then install it if necessary
                var dotnet = new Dotnet();
                var installedGlobalTools = await dotnet.ToolList();
                const string kqlToolName = "MicrosoftKustoServiceLayer";
                bool kqlToolInstalled = installedGlobalTools.Any(tool => string.Equals(tool, kqlToolName, StringComparison.InvariantCultureIgnoreCase));
                if (!kqlToolInstalled)
                {
                    var commandLineResult = await dotnet.ToolInstall("Microsoft.SqlServer.KustoServiceLayer.Tool");
                    commandLineResult.ThrowOnFailure();
                }

                compositeKernel
                    .AddKernelConnector(new ConnectKqlCommand(kqlToolName));

                KernelInvocationContext.Current?.Display(
                    new HtmlString(@"<details><summary>Query Microsoft Kusto Server databases.</summary>
        <p>This extension adds support for connecting to Microsoft Kusto Server databases using the <code>#!connect kql</code> magic command. For more information, run a cell using the <code>#!kql</code> magic command.</p>
        </details>"),
                    "text/html");
            }
        }
    }
}