// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.VSCode
{
    public class VSCodeInteractiveHost : IInteractiveHost
    {
        private const string VSCodeKernelName = "vscode";

        private readonly Kernel _kernel;

        public VSCodeInteractiveHost(Kernel kernel)
        {
            _kernel = kernel;
        }

        /// <summary>
        /// Gets input from the user.
        /// </summary>
        /// <param name="prompt">The prompt to show.</param>
        /// <param name="isPassword">Whether the input should be treated as a password.</param>
        /// <returns>The value.</returns>
        public Task<string> GetInputAsync(string prompt = "", bool isPassword = false, CancellationToken cancellationToken = default)
        {
            var command = new GetInput(prompt, isPassword, targetKernelName: VSCodeKernelName);
            var completionSource = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var token = command.GetOrCreateToken();
            _kernel.KernelEvents.Where(e => e.Command.GetToken() == token).Subscribe(e =>
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
            }, cancellationToken);
            var _ = _kernel.SendAsync(command, cancellationToken);
            return completionSource.Task;
        }
    }
}
