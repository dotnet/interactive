// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Journey;

public class ChallengeContext
{
    public Challenge Challenge { get; }
    public ChallengeEvaluation Evaluation { get; }
    public Lesson Lesson => Challenge.Lesson;
    public IEnumerable<ChallengeSubmission> SubmissionHistory => Challenge.SubmissionHistory;
    public IEnumerable<RuleEvaluation> RuleEvaluations => Evaluation.RuleEvaluations;

    public ChallengeContext(Challenge challenge)
    {
        Challenge = challenge ?? throw new ArgumentNullException(nameof(challenge));
        Evaluation = new ChallengeEvaluation();
    }

    public void SetMessage(string message, object hint = null)
    {
        Evaluation.SetMessage(message, hint);
    }

    public async Task StartChallengeAsync(Challenge challenge)
    {
        await Lesson.StartChallengeAsync(challenge);
    }

    public async Task StartChallengeAsync(string name)
    {
        await Lesson.StartChallengeAsync(name);
    }

    public async Task StartNextChallengeAsync()
    {
        if (Challenge.DefaultProgressionHandler is not null)
        {
            await Challenge.DefaultProgressionHandler(this);
        }
        else
        {
            await StartChallengeAsync(null as Challenge);
        }
    }
}