// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.Interactive.SqlServer
{
    public class MsSqlKernelExtension : IKernelExtension
    {
        public Task OnLoadAsync(Kernel kernel)
        {
            if (kernel is CompositeKernel compositeKernel)
            {
                var root = compositeKernel.RootKernel.FindResolvedNativeSqlToolsServicePackageReference();
                if (root != null)
                {
                    var pathToService = root.PathToToolsService("MicrosoftSqlToolsServiceLayer");

                    if (!string.IsNullOrEmpty(pathToService))
                    {
                        if (File.Exists(pathToService))
                        {
                            compositeKernel
                            .UseKernelClientConnection(new ConnectMsSqlCommand(pathToService));

                            KernelInvocationContext.Current?.Display(
                                new HtmlString(@"<details><summary>Query Microsoft SQL Server databases.</summary>
    <p>This extension adds support for connecting to Microsoft SQL Server databases using the <code>#!connect mssql</code> magic command. For more information, run a cell using the <code>#!sql</code> magic command.</p>
    </details>"),
                                "text/html");
                        }
                        else
                        {
                            throw new InvalidOperationException($"The SQL Server extension was loaded successfully and resolved the path to the SQL Tools Service but the file {pathToService} does not exist. The #!connect mssql command will not be available.");
                        }

                    }
                    else
                    {
                        throw new InvalidOperationException($"The SQL Server extension was loaded successfully but was unable to determine the path to the SQL Tools Service. The #!connect mssql command will not be available.");
                    }
                }
                else
                {
                    throw new InvalidOperationException($"The SQL Server extension was loaded successfully but was unable to find the SQL Tools Service package. The #!connect mssql command will not be available. (RID: {RuntimeInformation.RuntimeIdentifier})");
                }
            }

            return Task.CompletedTask;
        }
    }
}