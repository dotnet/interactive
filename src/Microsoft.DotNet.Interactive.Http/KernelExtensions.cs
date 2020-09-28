// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;

using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Http
{
    public static class KernelExtensions
    {
        public static T UseHttpApi<T>(this T kernel, HttpPort httpPort, HttpProbingSettings httpProbingSettings)
            where T : Kernel
        {

            var initApiCommand = new Command("#!enable-http")
            {
                IsHidden = true,
                Handler = CommandHandler.Create((KernelInvocationContext context) =>
                {
                    if (context.Command is SubmitCode submitCode)
                    {
                        var probingUrls = httpProbingSettings != null
                            ? httpProbingSettings.AddressList
                            : new[]
                            {
                                new Uri($"http://localhost:{httpPort}")
                            };
                        var html =
                            HttpApiBootstrapper.GetHtmlInjection(probingUrls, httpPort?.ToString() ?? Guid.NewGuid().ToString("N"));
                        context.Display(html, "text/html");
                        context.Complete(submitCode);
                    }
                })
            };

            kernel.AddDirective(initApiCommand);

            return kernel;
        }
    }
}
