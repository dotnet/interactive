// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Commands
{
    public abstract class KernelCommand
    {
        protected KernelCommand(string targetKernelName = null, KernelCommand parent = null)
        {
            if (parent is null)
            {
                parent = KernelInvocationContext.Current?.Command;

                if (parent != this)
                {
                    Parent = parent;
                }
            }

            Properties = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

            TargetKernelName = targetKernelName;
        }

        [JsonIgnore, System.Text.Json.Serialization.JsonIgnore]
        public KernelCommandInvocation Handler { get; set; }

        [JsonIgnore, System.Text.Json.Serialization.JsonIgnore]   
        public KernelCommand Parent { get; internal set; }

        [JsonIgnore, System.Text.Json.Serialization.JsonIgnore]
        public IDictionary<string, object> Properties { get; }

        public string TargetKernelName { get; internal set; }

        public virtual Task InvokeAsync(KernelInvocationContext context)
        {
            if (Handler == null)
            {
                throw new NoSuitableKernelException(this);
            }

            return Handler(this, context);
        }
    }
}