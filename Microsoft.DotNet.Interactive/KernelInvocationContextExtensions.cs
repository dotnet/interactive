// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive
{
    public static class KernelInvocationContextExtensions
    {
        public static async Task<DisplayedValue> DisplayAsync(
            this KernelInvocationContext context,
            object value, 
            string mimeType = null)
        {
            var displayId = Kernel.DisplayIdGenerator?.Invoke() ??
                            Guid.NewGuid().ToString();

             mimeType ??= Formatter.PreferredMimeTypeFor(value.GetType());

            var formatted = new FormattedValue(
                mimeType,
                value.ToDisplayString(mimeType));

            var kernel = context.HandlingKernel ?? context.CurrentKernel;

            await kernel.SendAsync(new DisplayValue(value, formatted, displayId));

            return new DisplayedValue(displayId, mimeType);
        }
    }
}