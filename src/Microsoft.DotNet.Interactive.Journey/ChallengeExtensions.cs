// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Journey
{
    public static class ChallengeExtensions
    {
        public static Challenge SetDefaultProgressionHandler(this Challenge challenge, Challenge nextChallenge)
        {
            challenge.DefaultProgressionHandler = async context => await context.StartChallengeAsync(nextChallenge);
            return nextChallenge;
        }

        public static void SetDefaultProgressionHandlers(this List<Challenge> challenges)
        {
            for (int i = 0; i < challenges.Count - 1; i++)
            {
                challenges[i].SetDefaultProgressionHandler(challenges[i + 1]);
            }
            challenges.Last().DefaultProgressionHandler = async context =>
            {
                await context.StartChallengeAsync(null as Challenge);
            };
        }
    }
}
