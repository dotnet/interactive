// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Html;

namespace Microsoft.DotNet.Interactive.PostgreSql.Tests;

public class PSqlKernelExtension
{
    public static void Load(Kernel kernel)
    {
        if (kernel is CompositeKernel compositeKernel)
        {
            compositeKernel
                .AddKernelConnector(new ConnectPostgreSqlDirective());

            KernelInvocationContext.Current?.Display(
                new HtmlString(@"<details><summary>Query Posgres databases.</summary>
<p>This extension adds support for connecting to PostgreSql Server databases using the <code>#!connect psql</code> magic command. For more information, run a cell using the <code>#!sql</code> magic command.</p>
</details>"),
                "text/html");
        }
    }
}