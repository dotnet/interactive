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
public class LessonTests : ProgressiveLearningTestBase
{
    private Challenge GetChallenge(string? name = null)
    {
        return new(name: name);
    }

    private List<SendEditableCode> GetSendEditableCode(string code)
    {
        return new()
        {
            new SendEditableCode("csharp", code)
        };
    }

    [TestMethod]
    public async Task starting_to_an_unrevealed_challenge_directly_reveals_it()
    {
        var challenge = GetChallenge();

        await Lesson.StartChallengeAsync(challenge);

        challenge.Revealed.Should().BeTrue();
    }

    [TestMethod]
    public async Task starting_a_challenge_sets_the_current_challenge_to_it()
    {
        var challenge = GetChallenge();

        await Lesson.StartChallengeAsync(challenge);

        Lesson.CurrentChallenge.Should().Be(challenge);
    }

    [TestMethod]
    public async Task teacher_can_start_a_challenge_using_challenge_name()
    {
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        var challenges = new Challenge[]
        {
            new(contents: GetSendEditableCode("1"), name: "1"),
            new(contents: GetSendEditableCode("2"), name: "2"),
            new(contents: GetSendEditableCode("3"), name: "3")
        }.ToList();
        challenges[0].OnCodeSubmittedAsync(async context =>
        {
            await context.StartChallengeAsync("3");
        });
        challenges.SetDefaultProgressionHandlers();
        await Lesson.StartChallengeAsync(challenges[0]);
        Lesson.SetChallengeLookup(name => challenges.FirstOrDefault(c => c.Name == name));

        await kernel.SubmitCodeAsync("1+1");

        Lesson.CurrentChallenge.Should().Be(challenges[2]);
    }

    [TestMethod]
    public async Task teacher_can_explicitly_start_the_next_challenge()
    {
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        var challenges = new[]
        {
            GetChallenge("1"),
            GetChallenge("2"),
            GetChallenge("3")
        }.ToList();
        challenges[0].OnCodeSubmittedAsync(async context =>
        {
            await context.StartNextChallengeAsync();
        });
        challenges.SetDefaultProgressionHandlers();
        await Lesson.StartChallengeAsync(challenges[0]);

        await kernel.SubmitCodeAsync("1+1");

        Lesson.CurrentChallenge.Should().Be(challenges[1]);
    }

    [TestMethod]
    public async Task teacher_can_stay_at_the_current_challenge()
    {
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        var challenges = new[]
        {
            GetChallenge("1"),
            GetChallenge("2"),
            GetChallenge("3")
        }.ToList();
        challenges[0].OnCodeSubmitted(context =>
        {
            context.SetMessage("hi");
        });
        challenges.SetDefaultProgressionHandlers();
        await Lesson.StartChallengeAsync(challenges[0]);

        await kernel.SubmitCodeAsync("1+1");

        Lesson.CurrentChallenge.Should().Be(challenges[0]);
    }

    [TestMethod]
    public async Task when_teacher_chooses_to_stay_at_the_current_challenge_the_next_challenge_is_not_revealed()
    {
        var capturedCommands = new List<SendEditableCode>();
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        var vscodeKernel = kernel.FindKernelByName("vscode");
        vscodeKernel.RegisterCommandHandler<SendEditableCode>((command, _) =>
        {
            capturedCommands.Add(command);
            return Task.CompletedTask;
        });
        using var events = kernel.KernelEvents.ToSubscribedList();
        var contents = new SendEditableCode[] {
            new("csharp", "var a = 2;"),
            new("csharp", "var b = 3;"),
            new("csharp", "a = 4;")
        };
        var challenges = new Challenge[]
        {
            new(),
            new(contents: contents)
        }.ToList();
        challenges[0].OnCodeSubmitted(context => { });
        challenges.SetDefaultProgressionHandlers();
        await Lesson.StartChallengeAsync(challenges[0]);

        await kernel.SubmitCodeAsync("Console.WriteLine(1 + 1);");

        capturedCommands.Should().BeEmpty();
    }

    [TestMethod]
    public async Task explicitly_starting_the_next_challenge_at_last_challenge_does_nothing()
    {
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        var challenge = GetChallenge("1");
        challenge.OnCodeSubmittedAsync(async context =>
        {
            await context.StartNextChallengeAsync();
        });
        await Lesson.StartChallengeAsync(challenge);

        await kernel.SubmitCodeAsync("1+1");

        Lesson.CurrentChallenge.Should().Be(null);
    }

    [TestMethod]
    public async Task when_student_reaches_the_end_then_submissions_work_as_normal()
    {
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        var challenge = GetChallenge("1");
        challenge.OnCodeSubmittedAsync(async context =>
        {
            await context.StartNextChallengeAsync();
        });
        await Lesson.StartChallengeAsync(challenge);

        await kernel.SubmitCodeAsync("1+1");

        Lesson.CurrentChallenge.Should().Be(null);

        var result = await kernel.SubmitCodeAsync("1+41");
        result.Events.Should().NotContainErrors();

        result.Events.Should().ContainSingle<ReturnValueProduced>()
              .Which
              .Value
              .Should()
              .Be(42);
    }

    [TestMethod]
    public async Task when_a_student_submits_code_to_a_challenge_they_move_to_the_next_challenge()
    {
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        var challenges = new[]
        {
            GetChallenge("1"),
            GetChallenge("2"),
            GetChallenge("3")
        }.ToList();
        challenges.SetDefaultProgressionHandlers();
        await Lesson.StartChallengeAsync(challenges[0]);

        await kernel.SubmitCodeAsync("1+1");

        Lesson.CurrentChallenge.Should().Be(challenges[1]);
    }

    [TestMethod]
    public async Task when_a_student_completes_the_last_challenge_then_the_Lesson_is_completed()
    {
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        var challenges = new[]
        {
            GetChallenge("1"),
            GetChallenge("2")
        }.ToList();
        challenges.SetDefaultProgressionHandlers();
        await Lesson.StartChallengeAsync(challenges[0]);

        await kernel.SubmitCodeAsync("1+1");
        await kernel.SubmitCodeAsync("2+1");

        Lesson.CurrentChallenge.Should().Be(null);
    }

    [TestMethod]
    public async Task teacher_can_run_challenge_environment_setup_code_when_starting_a_Lesson()
    {
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        using var events = kernel.KernelEvents.ToSubscribedList();
        var setup = new SubmitCode[] {
            new("var a = 2;"),
            new("var b = 3;"),
            new("a = 4;")
        };
        var challenge = new Challenge(environmentSetup: setup);
        await Lesson.StartChallengeAsync(challenge);
        await kernel.InitializeChallenge(challenge);

        await kernel.SubmitCodeAsync("a+b");

        events.Should().ContainSingle<ReturnValueProduced>().Which.Value.Should().Be(7);
    }

    [TestMethod]
    public async Task teacher_can_show_challenge_contents_when_starting_a_Lesson()
    {
        var capturedSendEditableCode = new List<(string kernelName, string code)>();
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        var vscodeKernel = kernel.FindKernelByName("vscode");
        vscodeKernel.RegisterCommandHandler<SendEditableCode>((command, _) =>
        {
            capturedSendEditableCode.Add((command.KernelName, command.Code));
            return Task.CompletedTask;
        });
        using var events = kernel.KernelEvents.ToSubscribedList();
        var contents = new (string language, string code)[] {
            ("csharp", "var a = 2;"),
            ("csharp", "var b = 3;"),
            ("csharp", "a = 4;")
        };
        var challenge = new Challenge(contents: contents.Select(c => new SendEditableCode(c.language, c.code)).ToArray());
        await Lesson.StartChallengeAsync(challenge);
        await kernel.InitializeChallenge(challenge);

        await kernel.SubmitCodeAsync("a+b");

        capturedSendEditableCode.Should().BeEquivalentTo(contents);
    }

    [TestMethod]
    public async Task teacher_can_run_challenge_environment_setup_code_when_progressing_the_student_to_a_new_challenge()
    {
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        using var events = kernel.KernelEvents.ToSubscribedList();
        var setup = new SubmitCode[] {
            new("var a = 2;"),
            new("var b = 3;"),
            new("a = 4;")
        };
        var challenges = new Challenge[]
        {
            new(),
            new(environmentSetup: setup)
        }.ToList();
        challenges.SetDefaultProgressionHandlers();
        await Lesson.StartChallengeAsync(challenges[0]);
        await kernel.SubmitCodeAsync("Console.WriteLine(1 + 1);");

        await kernel.SubmitCodeAsync("a + b");

        events.Should().ContainSingle<ReturnValueProduced>().Which.Value.Should().Be(7);
    }

    [TestMethod]
    public async Task teacher_can_show_challenge_contents_when_progressing_the_student_to_a_new_challenge()
    {
        var capturedSendEditableCode = new List<(string kernelName, string code)>();
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        var vscodeKernel = kernel.FindKernelByName("vscode");
        vscodeKernel.RegisterCommandHandler<SendEditableCode>((command, _) =>
        {
            capturedSendEditableCode.Add((command.KernelName, command.Code));
            return Task.CompletedTask;
        });
        using var events = kernel.KernelEvents.ToSubscribedList();
        var contents = new (string language, string code)[] {
            ("csharp", "var a = 2;"),
            ("csharp", "var b = 3;"),
            ("csharp", "a = 4;")
        };
        var challenges = new Challenge[]
        {
            new(),
            new(contents: contents.Select(c => new SendEditableCode(c.language, c.code)).ToArray())
        }.ToList();
        challenges.SetDefaultProgressionHandlers();
        await Lesson.StartChallengeAsync(challenges[0]);
        await kernel.SubmitCodeAsync("Console.WriteLine(1 + 1);");

        await kernel.SubmitCodeAsync("a + b");

        capturedSendEditableCode.Should().BeEquivalentTo(contents);
    }

    [TestMethod]
    public async Task after_starting_a_lesson_the_student_can_submit_multiple_times_to_the_same_challenge_and_see_evaluation_feedback_for_the_latest_submission()
    {
        var correctAnswer = "1 + 1";
        var incorrectAnswer = "1 + 2";
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        using var events = kernel.KernelEvents.ToSubscribedList();
        var challenge = new Challenge();
        challenge.AddRule(context =>
        {
            if (context.SubmittedCode == correctAnswer)
            {
                context.Pass("You passed");
            }
            else
            {
                context.Fail("You failed");
            }
        });
        challenge.OnCodeSubmittedAsync(async context =>
        {
            var numRules = context.RuleEvaluations.Count();
            var numPassedRules = context.RuleEvaluations.Count(e => e.Passed);
            if (numRules == numPassedRules)
            {
                await context.StartNextChallengeAsync();
            }
        });
        await Lesson.StartChallengeAsync(challenge);
        await kernel.SubmitCodeAsync(incorrectAnswer);

        await kernel.SubmitCodeAsync(correctAnswer);

        events.Should().ContainSingle<DisplayedValueProduced>(
            e => e.FormattedValues.Single(v => v.MimeType == "text/html")
                .Value.Contains("You passed"));
    }

    [TestMethod]
    public async Task after_progressing_to_a_new_challenge_the_student_can_submit_multiple_times_to_the_same_challenge_and_see_evaluation_feedback_for_the_latest_submission()
    {
        var correctAnswer = "1 + 1";
        var incorrectAnswer = "1 + 2";
        using var kernel = await CreateKernel(LessonMode.StudentMode);
        using var events = kernel.KernelEvents.ToSubscribedList();
        var challenges = new List<Challenge> { new(), new() };
        challenges[1].AddRule(context =>
        {
            if (context.SubmittedCode == correctAnswer)
            {
                context.Pass("You passed");
            }
            else
            {
                context.Fail("You failed");
            }
        });
        challenges[1].OnCodeSubmittedAsync(async context =>
        {
            var numRules = context.RuleEvaluations.Count();
            var numPassedRules = context.RuleEvaluations.Count(e => e.Passed);
            if (numRules == numPassedRules)
            {
                await context.StartNextChallengeAsync();
            }
        });
        challenges.SetDefaultProgressionHandlers();
        await Lesson.StartChallengeAsync(challenges[0]);
        await kernel.SubmitCodeAsync("Console.WriteLine(1 + 1);");
        await kernel.SubmitCodeAsync(incorrectAnswer);

        await kernel.SubmitCodeAsync(correctAnswer);

        events.Should().ContainSingle<DisplayedValueProduced>(
            e => e.FormattedValues.Single(v => v.MimeType == "text/html")
                .Value.Contains("You passed"));
    }
}