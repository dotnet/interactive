// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class Quit : Cancel
    {
        private static Action _onQuit;
        private static readonly Action DefaultOnQuit = () => throw new InvalidOperationException("Quit command is not configured");

        static Quit()
        {
            _onQuit = DefaultOnQuit;
        }

        public static void OnQuit(Action onQuit)
        {
            _onQuit = onQuit ?? DefaultOnQuit;
        }
    
        [JsonConstructor]
        public Quit(string targetKernelName = null): base(targetKernelName)
        {
        }

        public override Task InvokeAsync(KernelInvocationContext context)
        {
            _onQuit();
            context?.Complete(context.Command);
            return Task.CompletedTask;
        }
    }
}