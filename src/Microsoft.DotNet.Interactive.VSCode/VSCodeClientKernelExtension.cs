// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.VSCode;

public class VSCodeClientKernelExtension
{
    public static async Task LoadAsync(Kernel kernel)
    {
        if (kernel is CompositeKernel root)
        {
            var hostKernel = await root.Host.ConnectProxyKernelOnDefaultConnectorAsync(
                "vscode",
                new Uri("kernel://vscode"),
                ["frontend"]);
            hostKernel.KernelSpecifierDirective.Hidden = true;

            hostKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(RequestInput)));
            root.SetDefaultTargetKernelNameForCommand(typeof(RequestInput), "vscode");
            hostKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(RequestInputs)));
            root.SetDefaultTargetKernelNameForCommand(typeof(RequestInputs), "vscode");
            hostKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(SendEditableCode)));
            root.SetDefaultTargetKernelNameForCommand(typeof(SendEditableCode), "vscode");

            var jsKernel = await root.Host.ConnectProxyKernelOnDefaultConnectorAsync(
                "javascript",
                new Uri("kernel://webview/javascript"),
                ["js"]);
            jsKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(SubmitCode)));
            jsKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(RequestValue)));
            jsKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(RequestValueInfos)));
            jsKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(SendValue)));

            jsKernel.UseValueSharing();
        }
    }
}