// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class RequestValue : KernelCommand
    {
        public string Name { get; }

        public string MimeType { get;  }

        public RequestValue(string name, string targetKernelName, string mimeType = null ) : base(targetKernelName)
        {
            Name = name;
            MimeType = mimeType;
        }

        public override Task InvokeAsync(KernelInvocationContext context)
        {
            if (context.HandlingKernel is ISupportGetValues supportGetValuesKernel)
            {
                if (supportGetValuesKernel.TryGetValue(Name, out object value))
                {
                    var mimeType = MimeType ?? Formatter.GetPreferredMimeTypeFor(value.GetType());
                    var formatted = new FormattedValue(mimeType, value?.ToDisplayString(mimeType));

                    context.Publish(new ValueProduced(value, Name, this, formatted));
                    return Task.CompletedTask;
                }

                throw new InvalidOperationException($"Cannot find value named: {Name}");
            }

            throw new InvalidOperationException($"Kernel {context.HandlingKernel.Name} doesn't support command {nameof(RequestValue)}");
        }
    }
}