// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Directives;

namespace Microsoft.DotNet.Interactive.Http;

internal static class KernelExtensions
{
    public static T UseHttpApi<T>(this T kernel, HttpPort httpPort, HttpProbingSettings httpProbingSettings)
        where T : Kernel
    {
        var initApiCommand = new KernelActionDirective("#!enable-http")
        {
            Hidden = true,
        };

        kernel.AddDirective(initApiCommand, EnableHttp);

        return kernel;

        Task EnableHttp(KernelCommand _, KernelInvocationContext context)
        {
            if (context.Command is SubmitCode)
            {
                var probingUrls = httpProbingSettings is not null
                                      ? httpProbingSettings.AddressList
                                      :
                                      [
                                          $"http://localhost:{httpPort}"
                                      ];
                var html =
                    HttpApiBootstrapper.GetHtmlInjection(probingUrls, httpPort?.ToString() ?? Guid.NewGuid().ToString("N"));
                context.Display(html, "text/html");
            }

            return Task.CompletedTask;
        }
    }
}