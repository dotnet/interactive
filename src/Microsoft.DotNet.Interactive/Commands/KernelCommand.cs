// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Commands
{
    public abstract class KernelCommand
    {
        protected KernelCommand(string targetKernelName = null, KernelCommand parent = null)
        {
            Properties = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
            TargetKernelName = targetKernelName;
            Parent = parent;
        }

        [JsonIgnore]
        public KernelCommandInvocation Handler { get; set; }

        [JsonIgnore]
        public KernelCommand Parent { get; internal set; }

        [JsonIgnore]
        public bool? ShouldPublishCompletionEvent { get; protected internal set; }

        [JsonIgnore]
        public IDictionary<string, object> Properties { get; }

        public string TargetKernelName { get; internal set; }

        internal static KernelCommand None { get; } = new NoCommand();

        [JsonIgnore]
        internal KernelUri KernelUri { get; set; }

        public virtual Task InvokeAsync(KernelInvocationContext context)
        {
            if (Handler is null)
            {
                throw new NoSuitableKernelException(this);
            }

            return Handler(this, context);
        }
    }
}