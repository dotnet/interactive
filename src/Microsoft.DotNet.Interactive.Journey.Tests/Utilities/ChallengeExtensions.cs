// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Journey.Tests.Utilities;

public static class ChallengeExtensions
{
    public static IEnumerable<bool> GetRevealedStatuses(this IEnumerable<Challenge> challenges)
    {
        return challenges.Select(c => c.Revealed);
    }
}