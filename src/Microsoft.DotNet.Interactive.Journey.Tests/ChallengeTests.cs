// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Journey.Tests.Utilities;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Journey.Tests;

[TestClass]
public class ChallengeTests : ProgressiveLearningTestBase
{
    [TestMethod]
    public async Task teacher_can_start_another_challenge_when_evaluating_a_challenge()
    {
        var challenge1 = GetEmptyChallenge();
        var challenge2 = GetEmptyChallenge();
        challenge1.OnCodeSubmittedAsync(async (context) =>
        {
            await context.StartChallengeAsync(challenge2);
        });
        challenge1.SetDefaultProgressionHandler(challenge2);
        await Lesson.StartChallengeAsync(challenge1);

        await challenge1.Evaluate();

        Lesson.CurrentChallenge.Should().Be(challenge2);
    }

    [TestMethod]
    public async Task teacher_can_access_code_from_submission_history_when_evaluating_a_challenge()
    {
        var capturedCode = new List<string>();
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        var challenge = GetEmptyChallenge();
        challenge.OnCodeSubmitted(context =>
        {
            capturedCode = context.SubmissionHistory.Select(h => h.SubmittedCode).ToList();
        });
        await Lesson.StartChallengeAsync(challenge);

        await kernel.SubmitCodeAsync("1 + 1");
        await kernel.SubmitCodeAsync("1 + 2");
        await kernel.SubmitCodeAsync("1 + 3");

        capturedCode.Should().BeEquivalentTo("1 + 2", "1 + 1");
        capturedCode.Should().NotContain("1 + 3");
    }

    [TestMethod]
    public async Task teacher_can_access_code_from_submission_history_when_evaluating_a_model_answer()
    {
        var capturedCode = new List<string>();
        using var kernel = await CreateKernel(LessonMode.TeacherMode);
        var challenge = GetEmptyChallenge();
        challenge.OnCodeSubmitted(context =>
        {
            capturedCode = context.SubmissionHistory.Select(h => h.SubmittedCode).ToList();
        });
        await Lesson.StartChallengeAsync(challenge);

        await kernel.SubmitCodeAsync(ToModelAnswer("1 + 1"));
        await kernel.SubmitCodeAsync(ToModelAnswer("1 + 2"));
        await kernel.SubmitCodeAsync(ToModelAnswer("1 + 3"));

        capturedCode.Should().BeEquivalentTo("1 + 2", "1 + 1");
        capturedCode.Should().NotContain("1 + 3");
    }

    [TestMethod]
    public async Task challenge_tracks_submitted_code_in_submission_history()
    {
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        var challenge = GetEmptyChallenge();
        challenge.OnCodeSubmitted(_ => { });
        await Lesson.StartChallengeAsync(challenge);

        await kernel.SubmitCodeAsync("1 + 1");
        await kernel.SubmitCodeAsync("1 + 2");
        await kernel.SubmitCodeAsync("1 + 3");

        challenge.SubmissionHistory.Select(h => h.SubmittedCode).ToList().Should().BeEquivalentTo("1 + 3", "1 + 2", "1 + 1");
    }

    [TestMethod]
    public async Task challenge_tracks_events_in_submission_history()
    {
        var capturedEvents = new List<List<KernelEvent>>();
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        var challenge = GetEmptyChallenge();
        challenge.OnCodeSubmitted(_ => { });
        await Lesson.StartChallengeAsync(challenge);

        await kernel.SubmitCodeAsync("alsjl");
        await kernel.SubmitCodeAsync("1 + 1");
        await kernel.SubmitCodeAsync("1 + 2");

        capturedEvents = challenge.SubmissionHistory.Select(s => s.EventsProduced.ToList()).ToList();
        capturedEvents.Should().SatisfyRespectively(
            events => events.Should().ContainSingle<ReturnValueProduced>().Which.Value.Should().Be(3),
            events => events.Should().ContainSingle<ReturnValueProduced>().Which.Value.Should().Be(2),
            events => events.Should().ContainSingle<CommandFailed>());
    }

    [TestMethod]
    public async Task challenge_tracks_evaluations_in_submission_history()
    {
        var numberOfSubmission = 1;
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        var challenge = GetEmptyChallenge();
        challenge.OnCodeSubmitted(context =>
        {
            context.SetMessage($"{numberOfSubmission}");
            numberOfSubmission++;
        });
        await Lesson.StartChallengeAsync(challenge);

        await kernel.SubmitCodeAsync("1 + 1");
        await kernel.SubmitCodeAsync("1 + 1");
        await kernel.SubmitCodeAsync("1 + 1");

        var capturedEvaluation = challenge.SubmissionHistory.Select(h => h.Evaluation).ToList();

        capturedEvaluation.Should().SatisfyRespectively(
            e => e.Message.Should().Be("3"),
            e => e.Message.Should().Be("2"),
            e => e.Message.Should().Be("1"));
    }

    [TestMethod]
    public async Task teacher_can_access_code_when_evaluating_a_rule()
    {
        var capturedCode = new List<string>();
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        var challenge = GetEmptyChallenge();
        challenge.AddRule(context =>
        {
            capturedCode.Add(context.SubmittedCode);
        });
        challenge.OnCodeSubmitted(_ => { });
        await Lesson.StartChallengeAsync(challenge);

        await kernel.SubmitCodeAsync("1 + 1");
        await kernel.SubmitCodeAsync("1 + 2");
        await kernel.SubmitCodeAsync("1 + 3");

        capturedCode.Should().BeEquivalentTo("1 + 1", "1 + 2", "1 + 3");
    }

    [TestMethod]
    public async Task teacher_can_access_events_when_evaluating_a_rule()
    {
        var capturedEvents = new List<List<KernelEvent>>();
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        var challenge = GetEmptyChallenge();
        challenge.AddRule(context =>
        {
            capturedEvents.Add(context.EventsProduced.ToList());
        });
        challenge.OnCodeSubmitted(_ => { });
        await Lesson.StartChallengeAsync(challenge);

        await kernel.SubmitCodeAsync("alsjkdf");
        await kernel.SubmitCodeAsync("1 + 1");
        await kernel.SubmitCodeAsync("1 + 2");

        capturedEvents.Should().SatisfyRespectively(
            events => events.Should().ContainSingle<CommandFailed>(),
            events => events.Should().ContainSingle<ReturnValueProduced>().Which.Value.Should().Be(2),
            events => events.Should().ContainSingle<ReturnValueProduced>().Which.Value.Should().Be(3));
    }

    [TestMethod]
    public async Task teacher_can_use_assertion_libraries_in_rule_definitions()
    {
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        var challenge = GetEmptyChallenge();
        challenge.AddRule(c =>
        {
            3.Should().Be(10);
        });
        await Lesson.StartChallengeAsync(challenge);

        await kernel.SubmitCodeAsync("1 + 1");

        challenge.CurrentEvaluation.RuleEvaluations.First().Reason.Should().Be("Expected value to be 10, but found 3 (difference of -7).");
    }

    [TestMethod]
    public async Task teacher_can_use_exceptions_to_fail_evaluation()
    {
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        var challenge = GetEmptyChallenge();
        challenge.AddRule(
            c => throw new ArgumentException($"Students should write better than {c.SubmittedCode}"));
        await Lesson.StartChallengeAsync(challenge);

        await kernel.SubmitCodeAsync("1 + 1");

        challenge.CurrentEvaluation.RuleEvaluations.First().Reason.Should().Be("Students should write better than 1 + 1");
    }

    [TestMethod]
    public async Task unhandled_exception_will_cause_rule_to_fail()
    {
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        var challenge = GetEmptyChallenge();
        challenge.AddRule(c =>
        {
            var userValue = 0;
            var ratio = 10 / userValue;
            if (ratio > 1)
            {
                c.Pass("Good job");
            }
        });
        await Lesson.StartChallengeAsync(challenge);

        await kernel.SubmitCodeAsync("1 + 1");

        challenge.CurrentEvaluation.RuleEvaluations.First().Reason.Should().Be("Attempted to divide by zero.");
    }
}