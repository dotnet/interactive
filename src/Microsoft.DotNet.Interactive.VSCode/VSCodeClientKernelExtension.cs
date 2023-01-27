// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.PowerShell;

namespace Microsoft.DotNet.Interactive.VSCode;

public class VSCodeClientKernelExtension : IKernelExtension
{
    public async Task OnLoadAsync(Kernel kernel)
    {
        if (kernel is CompositeKernel root)
        {
            var hostKernel = await root.Host.ConnectProxyKernelOnDefaultConnectorAsync(
                "vscode",
                new Uri("kernel://vscode"),
                new[] { "frontend" });
            hostKernel.ChooseKernelDirective.IsHidden = true;
            hostKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(RequestInput)));
            root.SetDefaultTargetKernelNameForCommand(typeof(RequestInput), "vscode");
            hostKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(SendEditableCode)));
            root.SetDefaultTargetKernelNameForCommand(typeof(SendEditableCode), "vscode");

            var jsKernel = await root.Host.ConnectProxyKernelOnDefaultConnectorAsync(
                "javascript",
                new Uri("kernel://webview/javascript"),
                new[] { "js" });
            jsKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(SubmitCode)));
            jsKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(RequestValue)));
            jsKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(RequestValueInfos)));
            jsKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(SendValue)));

            jsKernel.UseValueSharing();

            root.VisitSubkernels(subkernel =>
            {
                if (subkernel is PowerShellKernel powerShellKernel)
                {
                    powerShellKernel.ReadInput = prompt =>
                    {
                        var result = Kernel.GetInputAsync(prompt).GetAwaiter().GetResult();
                        return result;
                    };

                    powerShellKernel.ReadPassword = prompt =>
                    {
                        var result = Kernel.GetPasswordAsync(prompt).GetAwaiter().GetResult();
                        return new PasswordString(result);
                    };
                }
            });
        }
    }
}