// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Reactive.Disposables;
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
    
        public Quit(string targetKernelName = null): base(targetKernelName)
        {
            Handler = (_, context) =>
            {
                context.Complete(context.Command);
                DisposeOnQuit?.Dispose();
                Environment.Exit(0);
                return Task.CompletedTask;
            };
        }
    }
}