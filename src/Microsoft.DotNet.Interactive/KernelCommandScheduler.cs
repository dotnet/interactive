// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive;

public class KernelCommandScheduler : KernelScheduler<KernelCommand, KernelCommandResult>
{
    protected override bool IsPreemptive(
        KernelCommand current,
        KernelCommand incoming)
    {
        if (current is null)
        {
            return false;
        }

        if (incoming.Parent == current)
        {
            return true;
        }

        var currentToken = current.GetOrCreateToken();
        var incomingToken = incoming.GetOrCreateToken();

        if (incomingToken == currentToken)
        {
            return true;
        }

        if (incomingToken.StartsWith(currentToken))
        {
            return true;
        }

        if (AreSiblings(currentToken, incomingToken))
        {
            return true;
        }

        return incoming.RoutingSlip.StartsWith(current.RoutingSlip);
    }

    private bool AreSiblings(string currentToken, string incomingToken)
    {
        return GetParentTokenOf(currentToken) == GetParentTokenOf(incomingToken);
    }

    private string GetParentTokenOf(string token)
    {
        var parts = token.Split(new []{'.'});

        return string.Join(".", parts[..^1]);
    }
}