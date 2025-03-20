// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Journey.Tests.Utilities;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Journey.Tests;

[TestClass]
public class WorkflowTests : ProgressiveLearningTestBase
{
    private readonly IReadOnlyList<SendEditableCode> _sampleContent = new SendEditableCode[]
    {
        new("markdown",
            @"# Challenge 1

## Add 1 with 2 and return it"),

        new("csharp",
            @"// write your answer here")
    };

    private string sampleAnswer =
        @"#!csharp
1 + 2";

 
    [TestMethod]
    public async Task teacher_can_evaluate_a_challenge()
    {
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        using var events = kernel.KernelEvents.ToSubscribedList();

        // teacher defines challenge
        var challenge = new Challenge(contents: _sampleContent);
        challenge.AddRule(ruleContext =>
        {
            ruleContext.Fail("this rule failed because reasons");
        });
        challenge.OnCodeSubmitted(challengeContext =>
        {
            double numPassed = challengeContext.RuleEvaluations.Count(e => e.Passed);
            var total = challengeContext.RuleEvaluations.Count();
            if (numPassed / total >= 0.5)
            { }
            else
            {
                challengeContext.SetMessage("Keep working!");
            }
        });

        // teacher sends challenge
        await Lesson.StartChallengeAsync(challenge);

        // student submit code
        await kernel.SubmitCodeAsync("1+1");

        events.Should().ContainSingle<DisplayedValueProduced>()
            .Which.FormattedValues
            .Should()
            .ContainSingle(v =>
                v.MimeType == "text/html"
                && v.Value.Contains("Keep working!")
                && v.Value.Contains("this rule failed because reasons"));
    }

    [TestMethod]
    public async Task teacher_can_access_challenge_submission_history_for_challenge_evaluation()
    {
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        using var events = kernel.KernelEvents.ToSubscribedList();

        // teacher defines challenge
        var challenge = new Challenge(contents: _sampleContent);
        challenge.AddRule(ruleContext =>
        {
            ruleContext.Fail("this rule failed because reasons");
        });
        challenge.OnCodeSubmitted(challengeContext =>
        {
            double numPassed = challengeContext.RuleEvaluations.Count(e => e.Passed);
            var total = challengeContext.RuleEvaluations.Count();
            if (numPassed / total >= 0.5)
            {
                challengeContext.SetMessage("Good work! Challenge 1 passed.");
            }
            else
            {
                var history = challengeContext.SubmissionHistory;
                var pastFailures = 0;
                foreach (var submission in history)
                {
                    numPassed = submission.RuleEvaluations.Count(e => e.Passed);
                    total = submission.RuleEvaluations.Count();
                    if (numPassed / total < 0.5) pastFailures++;
                }

                challengeContext.SetMessage(pastFailures > 2 ? "Enough! Try something else." : "Keep working!");
            }
        });

        // teacher sends challenge
        await Lesson.StartChallengeAsync(challenge);

        // student submit code
        await kernel.SubmitCodeAsync(sampleAnswer);
        await kernel.SubmitCodeAsync(sampleAnswer);
        await kernel.SubmitCodeAsync(sampleAnswer);
        await kernel.SubmitCodeAsync(sampleAnswer);

        events
            .Should()
            .ContainSingle<DisplayedValueProduced>(
                e => e.FormattedValues.Single(v => v.MimeType == "text/html")
                    .Value.Contains("Enough! Try something else."));
    }
}