// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Tests.Utility;

public static class KernelExtensions
{
    public static async Task<(bool success, ValueInfosProduced valueInfosProduced)> TryRequestValueInfosAsync(this Kernel kernel)
    {
        if (kernel.SupportsCommandType(typeof(RequestValueInfos)))
        {
            var result = await kernel.SendAsync(new RequestValueInfos());

            var candidateResult = await result.KernelEvents.OfType<ValueInfosProduced>().FirstOrDefaultAsync();
            if (candidateResult is { })
            {
                return (true, candidateResult);
            }
        }

        return (false, default);
    }

    public static async Task<(bool success, ValueProduced valueProduced)> TryRequestValueAsync(this Kernel kernel, string valueName)
    {
        if (kernel.SupportsCommandType(typeof(RequestValue)))
        {
            var commandResult = await kernel.SendAsync(new RequestValue(valueName));

            if (await commandResult.KernelEvents.OfType<ValueProduced>().FirstOrDefaultAsync() is { } valueProduced)
            {
                return (true, valueProduced);
            }
        }

        return (false, default);
    }
}