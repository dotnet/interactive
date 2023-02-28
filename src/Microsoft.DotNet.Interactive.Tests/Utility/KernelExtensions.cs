// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
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

    public static async Task<ValueProduced> RequestValueAsync(this Kernel kernel, string valueName)
    {
        var commandResult = await kernel.SendAsync(new RequestValue(valueName));

        commandResult.Events.Should().Contain(e => e is ValueProduced);

        return commandResult.Events.OfType<ValueProduced>().First();
    }
}