// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.VSCode
{
    public static class VSCodeInteractiveHost
    {
        private const string VSCodeKernelName = "vscode";

        /// <summary>
        /// Gets input from the user.
        /// </summary>
        /// <param name="prompt">The prompt to show.</param>
        /// <param name="isPassword">Whether the input should be treated as a password.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The user input value.</returns>
        public static Task<string> GetInputAsync(string prompt = "", bool isPassword = false, CancellationToken cancellationToken = default)
        {
            var kernel = Kernel.Root.FindKernel(VSCodeKernelName) ?? throw new ArgumentNullException($"Cannot find kernel {VSCodeKernelName}");

            var command = new GetInput(prompt, isPassword, targetKernelName: VSCodeKernelName);
            var completionSource = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var token = command.GetOrCreateToken();

            var sub = Kernel.Root.KernelEvents
                .Where(e => e.Command.GetOrCreateToken() == token)
                .Subscribe(e =>
                {
                    switch (e)
                    {
                        case CommandFailed ex:
                            completionSource.TrySetException(ex.Exception ?? new Exception(ex.Message));
                            break;
                        case CommandSucceeded _:
                            completionSource.TrySetResult(null);
                            break;
                        case InputProduced iv:
                            completionSource.TrySetResult(iv.Value);
                            break;
                    }
                });
            var _ = kernel.SendAsync(command, cancellationToken);
            return completionSource.Task.ContinueWith(t =>
            {
                sub.Dispose();
                if (t.IsCompletedSuccessfully)
                {
                    return t.Result;
                }
                else
                {
                    throw t.Exception;
                }
            }, cancellationToken);
        }
    }
}
