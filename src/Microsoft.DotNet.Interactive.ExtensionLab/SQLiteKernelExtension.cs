// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class SQLiteKernelExtension : IKernelExtension
    {
        public Task OnLoadAsync(Kernel kernel)
        {
            if (kernel is CompositeKernel compositeKernel)
            {
                kernel.UseNteractDataExplorer();
                kernel.UseSandDanceExplorer();

                compositeKernel
                    .UseKernelClientConnection(new ConnectSQLiteCommand());

                KernelInvocationContext.Current?.Display(
                    new HtmlString(@"<details><summary>Query SQLite databases.</summary>
    <p>This extension adds support for connecting to SQLite databases using the <code>#!connect sqlite</code> magic command. For more information, run a cell using the <code>#!sql</code> magic command.</p>
    </details>"),
                    "text/html");

            }

            return Task.CompletedTask;
        }
    }
}