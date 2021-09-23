﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.ExtensionLab;

namespace Microsoft.DotNet.Interactive.Kql
{
    public class KqlKernelExtension : IKernelExtension
    {
        public Task OnLoadAsync(Kernel kernel)
        {
            if (kernel is CompositeKernel compositeKernel)
            {
                kernel.UseNteractDataExplorer();
                kernel.UseSandDanceExplorer();

                compositeKernel
                    .UseKernelClientConnection(new ConnectionKql());

                KernelInvocationContext.Current?.Display(
                    new HtmlString(@"<details><summary>Query Microsoft Kusto Server databases.</summary>
    <p>This extension adds support for connecting to Microsoft Kusto Server databases using the <code>#!connect kql</code> magic command. For more information, run a cell using the <code>#!kql</code> magic command.</p>
    </details>"),
                    "text/html");
            }

            return Task.CompletedTask;
        }
    }
}