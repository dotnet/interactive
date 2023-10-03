// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.Http;

internal static class KernelExtensions
{
    public static T UseHttpApi<T>(this T kernel, HttpPort httpPort, HttpProbingSettings httpProbingSettings)
        where T : Kernel
    {

        var initApiCommand = new Command("#!enable-http")
        {
            IsHidden = true,
            Handler = CommandHandler.Create((InvocationContext cmdLineContext) =>
            {
                var context = cmdLineContext.GetService<KernelInvocationContext>();

                if (context.Command is SubmitCode)
                {
                    var probingUrls = httpProbingSettings is not null
                        ? httpProbingSettings.AddressList
                        : new[]
                        {
                            new Uri($"http://localhost:{httpPort}")
                        };
                    var html =
                        HttpApiBootstrapper.GetHtmlInjection(probingUrls, httpPort?.ToString() ?? Guid.NewGuid().ToString("N"));
                    context.Display(html, "text/html");
                }

                return Task.CompletedTask;
            })
        };

        kernel.AddDirective(initApiCommand);

        return kernel;
    }
}