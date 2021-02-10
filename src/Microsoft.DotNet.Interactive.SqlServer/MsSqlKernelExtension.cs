// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.SqlServer
{
    public class MsSqlKernelExtension : IKernelExtension
    {
        public Task OnLoadAsync(Kernel kernel)
        {
            if (kernel is CompositeKernel compositeKernel)
            {
                // this is a formatter for SQL data
                Formatter.Register
                <IEnumerable /* tables*/
                    <IEnumerable /* rows */
                        <IEnumerable /* fields */<(string, object)>>>>((source, writer) =>
                {
                    // TODO: (RegisterFormatters) do all the tables...

                    writer.Write(source.First()
                                       .ToTabularJsonString()
                                       .ToDisplayString(HtmlFormatter.MimeType));
                }, HtmlFormatter.MimeType);

                compositeKernel
                    .UseKernelClientConnection(new MsSqlKernelConnection());

                KernelInvocationContext.Current?.Display(
                    $@"
* Adds `mssql` to the connection types available using the [`#!connect`](https://github.com/dotnet/interactive/blob/main/docs/connect.md) magic command.",
                    "text/markdown");

            }

            return Task.CompletedTask;
        }
    }
}