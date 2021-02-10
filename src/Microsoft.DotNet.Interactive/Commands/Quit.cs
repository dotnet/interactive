// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Reactive.Disposables;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class Quit : KernelCommand
    {
        private static readonly CompositeDisposable DisposeOnQuit = new CompositeDisposable();

        public static void RegisterForDisposalOnQuit(IDisposable disposable)
        {
            if (disposable is not null)
            {
                DisposeOnQuit.Add(disposable);
            }
        }
    
        [JsonConstructor]
        public Quit(string targetKernelName = null): this(() =>
        {
            Environment.Exit(0);
        }, targetKernelName)
        {
        }

        public Quit(Action onQuit, string targetKernelName = null) : base(targetKernelName)
        {
            if (onQuit == null)
            {
                throw new ArgumentNullException(nameof(onQuit));
            }
            Handler = (_, context) =>
            {
                context?.Complete(context.Command);
                DisposeOnQuit?.Dispose();
                onQuit();
                return Task.CompletedTask;
            };
        }
    }
}