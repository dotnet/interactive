// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
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

            var candidateResult = result.Events.OfType<ValueInfosProduced>().FirstOrDefault();
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

            if (commandResult.Events.OfType<ValueProduced>().FirstOrDefault() is { } valueProduced)
            {
                return (true, valueProduced);
            }
        }

        // FIX: (TryRequestValueAsync) this doesn't need to be a Try method since we never return false in actual usage. we should throw and clean up the associated tests.


        return (false, default);
    }
}