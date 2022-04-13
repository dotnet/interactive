// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Tests.Utility;

public static class KernelExtensions
{
    public static async Task<KernelInfo> GetKernelInfoAsync(this Kernel kernel)
    {
        var result = await kernel.SendAsync(new RequestKernelInfo());

        return await result
                     .KernelEvents
                     .OfType<KernelInfoProduced>()
                     .Select(e => e.KernelInfo)
                     .SingleOrDefaultAsync();
    }
}