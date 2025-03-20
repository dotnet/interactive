// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Journey.Tests.Utilities;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Journey.Tests;

[TestClass]
public class OutputEvaluationTests : ProgressiveLearningTestBase
{
    [TestMethod]
    public async Task teacher_can_provide_challenge_evaluation_feedback()
    {
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        var challenge = GetEmptyChallenge();
        challenge.OnCodeSubmitted(context =>
        {
            context.SetMessage("123", 3);
        });
        await Lesson.StartChallengeAsync(challenge);

        await kernel.SubmitCodeAsync("1 + 1");

        challenge.CurrentEvaluation.Message.Should().Be("123");
        challenge.CurrentEvaluation.Hint.Should().Be(3);
    }

    [TestMethod]
    public async Task teacher_can_fail_rule_evaluation_and_provide_feedback_and_hint()
    {
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        var challenge = GetEmptyChallenge();
        challenge.AddRule(context => context.Fail("abc", 3));
        await Lesson.StartChallengeAsync(challenge);

        await kernel.SubmitCodeAsync("1 + 1");

        challenge.CurrentEvaluation.RuleEvaluations.Single().Outcome.Should().Be(Outcome.Failure);
        challenge.CurrentEvaluation.RuleEvaluations.Single().Reason.Should().Be("abc");
        challenge.CurrentEvaluation.RuleEvaluations.Single().Hint.Should().Be(3);
    }

    [TestMethod]
    public async Task teacher_can_pass_rule_evaluation_and_provide_feedback_and_hint()
    {
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        var challenge = GetEmptyChallenge();
        challenge.AddRule(context => context.Pass("abc", 3));
        await Lesson.StartChallengeAsync(challenge);

        await kernel.SubmitCodeAsync("1 + 1");

        challenge.CurrentEvaluation.RuleEvaluations.Single().Outcome.Should().Be(Outcome.Success);
        challenge.CurrentEvaluation.RuleEvaluations.Single().Reason.Should().Be("abc");
        challenge.CurrentEvaluation.RuleEvaluations.Single().Hint.Should().Be(3);
    }

    [TestMethod]
    public async Task teacher_can_partially_pass_rule_evaluation_and_provide_feedback_and_hint()
    {
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        var challenge = GetEmptyChallenge();
        challenge.AddRule(context => context.PartialPass("abc", 3));
        await Lesson.StartChallengeAsync(challenge);

        await kernel.SubmitCodeAsync("1 + 1");

        challenge.CurrentEvaluation.RuleEvaluations.Single().Outcome.Should().Be(Outcome.PartialSuccess);
        challenge.CurrentEvaluation.RuleEvaluations.Single().Reason.Should().Be("abc");
        challenge.CurrentEvaluation.RuleEvaluations.Single().Hint.Should().Be(3);
    }

    [TestMethod]
    [Ignore("requires a dotnet interactive fix")]
    public async Task teacher_can_check_for_command_failed_as_a_rule()
    {
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        using var events = kernel.KernelEvents.ToSubscribedList();
        var challenge = GetEmptyChallenge();
        challenge.AddRule(c =>
        {
            foreach (var e in c.EventsProduced)
            {
                if (e is CommandFailed)
                {
                    c.Fail("Compilation failed. Try again!");
                    return;
                }
            }
            c.Pass("Success");
        });
        await Lesson.StartChallengeAsync(challenge);

        await kernel.SubmitCodeAsync("asjfl");

        events
            .Should()
            .ContainSingle<DisplayedValueProduced>(
                e => e.FormattedValues.Single(v => v.MimeType == "text/html")
                    .Value.Contains("Compilation failed. Try again!"));
    }
}