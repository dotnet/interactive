// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Commands
{
    public abstract class KernelCommandBase : IKernelCommand
    {
        protected KernelCommandBase(string targetKernelName = null)
        {
            var parent = KernelInvocationContext.Current?.Command;
            
            if (parent != this)
            {
                Parent = parent;
            }
            
            Properties = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

            TargetKernelName = targetKernelName;
        }

        [JsonIgnore]
        public KernelCommandInvocation Handler { get; set; }

        [JsonIgnore]   
        public IKernelCommand Parent { get; }

        [JsonIgnore]
        public IDictionary<string, object> Properties { get; }

        public string TargetKernelName { get; }

        public virtual async Task InvokeAsync(KernelInvocationContext context)
        {
            if (Handler == null)
            {
                throw new InvalidOperationException($"{GetType().Name}.{nameof(Handler)} was not set.");
            }

            await Handler(this, context);
        }
    }
}