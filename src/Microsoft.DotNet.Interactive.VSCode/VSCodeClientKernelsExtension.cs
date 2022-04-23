// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.ValueSharing;

namespace Microsoft.DotNet.Interactive.VSCode
{
    public class VSCodeClientKernelsExtension : IKernelExtension
    {
        public async Task OnLoadAsync(Kernel kernel)
        {
            if (kernel is CompositeKernel root)
            {
                var hostKernel = await root.Host.ConnectProxyKernelOnDefaultConnectorAsync(
                                     "vscode",
                                     new Uri("kernel://vscode/vscode"),
                                     new[] { "frontend" });
                hostKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(RequestInput)));
                hostKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(SendEditableCode)));

                var jsKernel = await root.Host.ConnectProxyKernelOnDefaultConnectorAsync(
                                   "javascript",
                                   new Uri("kernel://webview/javascript"),
                                   new[] { "js" });
                jsKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(SubmitCode)));
                jsKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(RequestValue)));
                jsKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(RequestValueInfos)));
                jsKernel.UseValueSharing(new JavaScriptValueDeclarer());
            }
        }
    }
}
