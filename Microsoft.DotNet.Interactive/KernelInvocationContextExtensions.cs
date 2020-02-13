// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive
{
    public static class KernelInvocationContextExtensions
    {
        public static Task<DisplayedValue> DisplayAsync(
            this KernelInvocationContext context,
            object value,
            string mimeType = null)
        {
            var displayId = Kernel.DisplayIdGenerator?.Invoke() ??
                            Guid.NewGuid().ToString();

            mimeType ??= Formatter.PreferredMimeTypeFor(value.GetType());

            var formattedValue = new FormattedValue(
                mimeType,
                value.ToDisplayString(mimeType));

            context.Publish(
                new DisplayedValueProduced(
                    value,
                    context?.Command,
                    new[] { formattedValue },
                    displayId));

            return Task.FromResult(new DisplayedValue(displayId, mimeType, context));
        }
    }
}