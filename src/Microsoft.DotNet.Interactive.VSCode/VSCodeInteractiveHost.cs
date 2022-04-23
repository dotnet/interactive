// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.VSCode
{
    public static class VSCodeInteractiveHost
    {
        // FIX: (VSCodeInteractiveHost) move this to someplace more central

        private const string VSCodeKernelName = "vscode";

        /// <summary>
        /// Gets input from the user.
        /// </summary>
        /// <param name="prompt">The prompt to show.</param>
        /// <param name="isPassword">Whether the input should be treated as a password.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The user input value.</returns>
        public static async Task<string> GetInputAsync(string prompt = "", bool isPassword = false, CancellationToken cancellationToken = default)
        {
            var kernel = Kernel.Root.FindKernel(VSCodeKernelName) ?? throw new ArgumentNullException($"Cannot find kernel {VSCodeKernelName}");

            var command = new RequestInput(prompt, isPassword, targetKernelName: VSCodeKernelName);

            var results = await kernel.SendAsync(command, cancellationToken);

            var inputProduced = await results.KernelEvents.OfType<InputProduced>().FirstOrDefaultAsync();

            return inputProduced?.Value;
        }
    }
}
