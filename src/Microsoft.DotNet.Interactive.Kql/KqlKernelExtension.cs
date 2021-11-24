// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.SqlServer;

namespace Microsoft.DotNet.Interactive.Kql
{
    public class KqlKernelExtension : IKernelExtension
    {
        public Task OnLoadAsync(Kernel kernel)
        {
            if (kernel is CompositeKernel compositeKernel)
            {
                var root = Kernel.Root.FindResolvedNativeSqlToolsServicePackageReference();
                if (root != null)
                {
                    var pathToService = root.PathToToolsService("MicrosoftKustoServiceLayer");

                    if (!string.IsNullOrEmpty(pathToService))
                    {
                        if (File.Exists(pathToService))
                        {
                            ConnectKqlCommand.ResolvedToolsServicePath = pathToService;
                            compositeKernel
                                .UseKernelClientConnection(new ConnectKqlCommand());

                            KernelInvocationContext.Current?.Display(
                                new HtmlString(@"<details><summary>Query Microsoft Kusto Server databases.</summary>
        <p>This extension adds support for connecting to Microsoft Kusto Server databases using the <code>#!connect kql</code> magic command. For more information, run a cell using the <code>#!kql</code> magic command.</p>
        </details>"),
                                "text/html");
                        }
                        else
                        {
                            KernelInvocationContext.Current?.DisplayStandardError($"The KQL extension was loaded successfully and resolved the path to the Kusto Tools Service but the file {pathToService} does not exist. The connect command will not be available.");
                        }

                    }
                    else
                    {
                        KernelInvocationContext.Current?.DisplayStandardError("The KQL extension was loaded successfully but was unable to determine the path to the KQL Tools Service. The connect command will not be available.");
                    }
                }
                else
                {
                    KernelInvocationContext.Current?.DisplayStandardError($"The KQL extension was loaded successfully but was unable to find the KQL Tools Service package. The connect command will not be available. (RID: {RuntimeInformation.RuntimeIdentifier})");
                }

            }

            return Task.CompletedTask;
        }
    }
}